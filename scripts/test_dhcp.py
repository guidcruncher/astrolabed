#!/usr/bin/env python3
"""
RFC 2131 / RFC 2132 Compliant DHCP Test Script (Zero Dependencies)
Performs a full DHCPOFFER -> DHCPREQUEST -> DHCPACK sequence via CLI configured targets.
"""

import argparse
import os
import random
import select
import socket
import struct
import sys
import time

# Message Types (Option 53)
DHCPDISCOVER = 1
DHCPOFFER = 2
DHCPREQUEST = 3
DHCPDECLINE = 4
DHCPACK = 5
DHCPNAK = 6
DHCPRELEASE = 7

# Magic Cookie (RFC 1497 / RFC 2132)
MAGIC_COOKIE = b"\x63\x82\x53\x63"


def generate_mac_address() -> bytes:
    """Generates a random locally administered MAC address."""
    return bytes([0x02, random.randint(0x00, 0xFF), random.randint(0x00, 0xFF),
                 random.randint(0x00, 0xFF), random.randint(0x00, 0xFF), random.randint(0x00, 0xFF)])


def parse_mac_address(mac_str: str) -> bytes:
    """Parses a colon/dash-separated MAC string into bytes."""
    clean_mac = mac_str.replace(":", "").replace("-", "")
    if len(clean_mac) != 12:
        raise ValueError(f"Invalid MAC address format: {mac_str}")
    return bytes.fromhex(clean_mac)


def format_mac(mac: bytes) -> str:
    """Formats bytes as a colon-separated MAC address."""
    return ":".join(f"{b:02x}" for b in mac)


def format_ip(ip_bytes: bytes) -> str:
    """Formats 4-byte string as an IPv4 address."""
    return socket.inet_ntoa(ip_bytes)


class DHCPPacket:
    """
    RFC 2131 Section 2: Format of a DHCP Message
    """

    def __init__(self, xid: int, chaddr: bytes, op: int = 1):
        self.op = op          # 1 = BOOTREQUEST, 2 = BOOTREPLY
        self.htype = 1       # 1 = Ethernet (10Mb)
        self.hlen = 6        # Hardware address length
        self.hops = 0
        self.xid = xid        # Transaction ID
        self.secs = 0
        self.flags = 0x8000   # Broadcast flag set per RFC 2131
        self.ciaddr = b"\x00\x00\x00\x00"
        self.yiaddr = b"\x00\x00\x00\x00"
        self.siaddr = b"\x00\x00\x00\x00"
        self.giaddr = b"\x00\x00\x00\x00"
        self.chaddr = chaddr.ljust(16, b"\x00")
        self.sname = b"\x00" * 64
        self.file = b"\x00" * 128
        self.options = {}

    def pack_options(self) -> bytes:
        """Encodes options dictionary into binary TLV format (RFC 2132)."""
        opts_bytes = bytearray(MAGIC_COOKIE)
        for code, value in self.options.items():
            opts_bytes.append(code)
            opts_bytes.append(len(value))
            opts_bytes.extend(value)
        opts_bytes.append(255)  # End Option (255)
        return bytes(opts_bytes)

    def build(self) -> bytes:
        """Serializes full packet into bytes."""
        header = struct.pack(
            "!BBBBIHH4s4s4s4s16s64s128s",
            self.op,
            self.htype,
            self.hlen,
            self.hops,
            self.xid,
            self.secs,
            self.flags,
            self.ciaddr,
            self.yiaddr,
            self.siaddr,
            self.giaddr,
            self.chaddr,
            self.sname,
            self.file,
        )
        return header + self.pack_options()

    @classmethod
    def parse(cls, data: bytes):
        """Parses binary data into a DHCPPacket object and decoded options."""
        if len(data) < 240:
            raise ValueError("Packet too short to be valid DHCP")

        header = data[:236]
        cookie = data[236:240]

        if cookie != MAGIC_COOKIE:
            raise ValueError("Invalid Magic Cookie")

        fields = struct.unpack("!BBBBIHH4s4s4s4s16s64s128s", header)
        packet = cls(xid=fields[4], chaddr=fields[11][: fields[2]], op=fields[0])
        packet.htype = fields[1]
        packet.hlen = fields[2]
        packet.hops = fields[3]
        packet.secs = fields[5]
        packet.flags = fields[6]
        packet.ciaddr = fields[7]
        packet.yiaddr = fields[8]
        packet.siaddr = fields[9]
        packet.giaddr = fields[10]
        packet.sname = fields[12]
        packet.file = fields[13]

        opts_raw = data[240:]
        idx = 0
        while idx < len(opts_raw):
            code = opts_raw[idx]
            if code == 255:
                break
            if code == 0:
                idx += 1
                continue
            
            if idx + 1 >= len(opts_raw):
                break
            length = opts_raw[idx + 1]
            idx += 2
            val = opts_raw[idx : idx + length]
            packet.options[code] = val
            idx += length

        return packet


class DHCPTestClient:
    """DHCP Client performing RFC 2131 state machine validation with CLI configs."""

    def __init__(self, target_ip: str, target_port: int, client_port: int, 
                 interface: str = None, mac: bytes = None, timeout: int = 5):
        self.target_ip = target_ip
        self.target_port = target_port
        self.client_port = client_port
        self.interface = interface
        self.timeout = timeout
        self.mac = mac if mac else generate_mac_address()
        self.xid = random.randint(1, 0xFFFFFFFF)
        self.sock = None

    def setup_socket(self):
        """Initializes socket bound to specified interface and ports."""
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        if hasattr(socket, "SO_REUSEPORT"):
            try:
                self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)
            except OSError:
                pass

        # Bind to specific network interface if requested (Linux only)
        if self.interface:
            if hasattr(socket, "SO_BINDTODEVICE"):
                try:
                    self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BINDTODEVICE, self.interface.encode())
                except OSError as e:
                    print(f"[!] Warning: Failed to bind to interface {self.interface}: {e}")
            else:
                print("[!] Warning: SO_BINDTODEVICE is not supported on this platform.")

        self.sock.bind(("", self.client_port))

    def send_discover(self):
        """Builds and transmits DHCPDISCOVER."""
        pkt = DHCPPacket(xid=self.xid, chaddr=self.mac)
        pkt.options[53] = bytes([DHCPDISCOVER])
        pkt.options[55] = bytes([1, 3, 6, 15])
        
        print(f"[*] Sending DHCPDISCOVER to {self.target_ip}:{self.target_port} "
              f"(XID: {hex(self.xid)}, MAC: {format_mac(self.mac)})...")
        self.sock.sendto(pkt.build(), (self.target_ip, self.target_port))

    def receive_offer(self) -> DHCPPacket:
        """Listens for and validates DHCPOFFER matching XID."""
        start = time.time()
        while time.time() - start < self.timeout:
            remain = self.timeout - (time.time() - start)
            r, _, _ = select.select([self.sock], [], [], max(0.1, remain))
            if r:
                data, addr = self.sock.recvfrom(2048)
                try:
                    pkt = DHCPPacket.parse(data)
                    msg_type = pkt.options.get(53, [0])[0]
                    if pkt.xid == self.xid and msg_type == DHCPOFFER:
                        server_ip = format_ip(pkt.options.get(54, b"\x00\x00\x00\x00"))
                        offered_ip = format_ip(pkt.yiaddr)
                        print(f"[+] Received DHCPOFFER from {addr[0]}:{addr[1]}:")
                        print(f"    ├─ Offered IP : {offered_ip}")
                        print(f"    └─ Server ID  : {server_ip}")
                        return pkt
                except ValueError:
                    continue
        raise TimeoutError("Timed out waiting for DHCPOFFER")

    def send_request(self, offer: DHCPPacket):
        """Builds and transmits DHCPREQUEST in response to an offer."""
        pkt = DHCPPacket(xid=self.xid, chaddr=self.mac)
        server_id_bytes = offer.options.get(54, b"\x00\x00\x00\x00")
        
        pkt.options[53] = bytes([DHCPREQUEST])
        pkt.options[50] = offer.yiaddr
        pkt.options[54] = server_id_bytes
        pkt.options[55] = bytes([1, 3, 6, 15])

        print(f"[*] Sending DHCPREQUEST for {format_ip(offer.yiaddr)} to {self.target_ip}:{self.target_port}...")
        self.sock.sendto(pkt.build(), (self.target_ip, self.target_port))

    def receive_ack(self) -> DHCPPacket:
        """Listens for and validates DHCPACK."""
        start = time.time()
        while time.time() - start < self.timeout:
            remain = self.timeout - (time.time() - start)
            r, _, _ = select.select([self.sock], [], [], max(0.1, remain))
            if r:
                data, addr = self.sock.recvfrom(2048)
                try:
                    pkt = DHCPPacket.parse(data)
                    msg_type = pkt.options.get(53, [0])[0]
                    if pkt.xid == self.xid:
                        if msg_type == DHCPACK:
                            lease = struct.unpack("!I", pkt.options.get(51, b"\x00\x00\x00\x00"))[0]
                            subnet = format_ip(pkt.options.get(1, b"\x00\x00\x00\x00"))
                            router = format_ip(pkt.options.get(3, b"\x00\x00\x00\x00"))
                            print(f"[+] Received DHCPACK from {addr[0]}:{addr[1]}:")
                            print(f"    ├─ Assigned IP : {format_ip(pkt.yiaddr)}")
                            print(f"    ├─ Subnet Mask : {subnet}")
                            print(f"    ├─ Router      : {router}")
                            print(f"    └─ Lease Time  : {lease} seconds")
                            return pkt
                        elif msg_type == DHCPNAK:
                            raise RuntimeError("Received DHCPNAK from server")
                except ValueError:
                    continue
        raise TimeoutError("Timed out waiting for DHCPACK")

    def run_test(self):
        """Executes full DORA sequence validation."""
        try:
            self.setup_socket()
            self.send_discover()
            offer = self.receive_offer()
            self.send_request(offer)
            self.receive_ack()
            print("\n[SUCCESS] Full RFC 2131 DHCP handshake completed successfully.")
        except Exception as err:
            print(f"\n[FAILURE] Test failed: {err}")
            sys.exit(1)
        finally:
            if self.sock:
                self.sock.close()


def parse_cli_args():
    parser = argparse.ArgumentParser(description="RFC 2131 DHCP Test CLI Tool (Zero Dependencies)")
    parser.add_argument(
        "-s", "--server-ip",
        default="255.255.255.255",
        help="Target DHCP server or broadcast IP address (Default: 255.255.255.255)"
    )
    parser.add_argument(
        "-p", "--port",
        type=int,
        default=67,
        help="Target DHCP server UDP port (Default: 67)"
    )
    parser.add_argument(
        "-c", "--client-port",
        type=int,
        default=68,
        help="Local UDP port to bind for responses (Default: 68)"
    )
    parser.add_argument(
        "-i", "--interface",
        default=None,
        help="Network interface to bind to (Linux only, e.g., eth0)"
    )
    parser.add_argument(
        "-m", "--mac",
        default=None,
        help="Custom client MAC address (e.g., 00:11:22:33:44:55). Random if omitted."
    )
    parser.add_argument(
        "-t", "--timeout",
        type=int,
        default=5,
        help="Timeout in seconds for response packets (Default: 5)"
    )
    return parser.parse_args()


if __name__ == "__main__":
    args = parse_cli_args()

    if os.geteuid() != 0 if hasattr(os, "geteuid") else False:
        print("[!] Warning: Binding to port 68 or specifying network interfaces requires root/admin privileges.")

    mac_bytes = parse_mac_address(args.mac) if args.mac else None

    client = DHCPTestClient(
        target_ip=args.server_ip,
        target_port=args.port,
        client_port=args.client_port,
        interface=args.interface,
        mac=mac_bytes,
        timeout=args.timeout,
    )
    client.run_test()
