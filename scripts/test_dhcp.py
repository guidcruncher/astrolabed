import argparse
import os
import socket
import struct
import time

DHCP_MSG_TYPES = {
    1: "DHCPDISCOVER",
    2: "DHCPOFFER",
    3: "DHCPREQUEST",
    4: "DHCPDECLINE",
    5: "DHCPACK",
    6: "DHCPNAK",
    7: "DHCPRELEASE",
    8: "DHCPINFORM",
}

DHCP_OPTION_DEFINITIONS = {
    1: ("Subnet Mask", "ip"),
    3: ("Router", "ip_list"),
    6: ("Domain Name Server", "ip_list"),
    12: ("Host Name", "string"),
    15: ("Domain Name", "string"),
    26: ("Interface MTU", "uint16"),
    28: ("Broadcast Address", "ip"),
    42: ("NTP Server", "ip_list"),
    43: ("Vendor Specific Information", "hex"),  # Added Option 43 support
    50: ("Requested IP Address", "ip"),
    51: ("IP Address Lease Time", "uint32_seconds"),
    53: ("DHCP Message Type", "msg_type"),
    54: ("DHCP Server Identifier", "ip"),
    58: ("Renewal Time (T1)", "uint32_seconds"),
    59: ("Rebinding Time (T2)", "uint32_seconds"),
    60: ("Vendor Class Identifier", "string"),
    66: ("TFTP Server Name", "string"),
    67: ("Bootfile Name", "string"),
    81: ("Client FQDN", "string"),
    252: ("WPAD URL", "string"),
}


def mac_to_bytes(mac_str: str) -> bytes:
    return bytes.fromhex(mac_str.replace(":", "").replace("-", ""))


def parse_ip_list(data: bytes) -> list[str]:
    ips = []
    for i in range(0, len(data), 4):
        if i + 4 <= len(data):
            ips.append(socket.inet_ntoa(data[i : i + 4]))
    return ips


def decode_option(opt_code: int, data: bytes) -> tuple[str, str]:
    name, opt_type = DHCP_OPTION_DEFINITIONS.get(
        opt_code, ("Unknown Option", "unknown")
    )

    if opt_type == "ip" and len(data) == 4:
        return name, socket.inet_ntoa(data)
    elif opt_type == "ip_list":
        ips = parse_ip_list(data)
        if ips:
            return name, ", ".join(ips)
    elif opt_type == "string":
        return name, data.decode("utf-8", errors="ignore").rstrip("\x00")
    elif opt_type == "uint16" and len(data) == 2:
        val = struct.unpack("!H", data)[0]
        return name, str(val)
    elif opt_type == "uint32_seconds" and len(data) == 4:
        sec = struct.unpack("!I", data)[0]
        return name, f"{sec} seconds"
    elif opt_type == "msg_type" and len(data) == 1:
        val = data[0]
        return name, DHCP_MSG_TYPES.get(val, f"Unknown ({val})")
    elif opt_type == "hex":
        return name, f"0x{data.hex()}"

    try:
        text = data.decode("ascii")
        if text.isprintable() and len(text) > 0:
            return name, text
    except Exception:
        pass

    return name, f"0x{data.hex()}"


def build_dhcp_packet(
    mac_bytes: bytes,
    xid: int,
    msg_type: int,
    requested_ip: str | None = None,
    server_ip: str | None = None,
    hostname: str | None = None,
    vendor_class_id: str | None = None,
    broadcast: bool = False,
) -> bytes:
    flags = 0x8000 if broadcast else 0x0000
    header = struct.pack("!BBBBIHH", 1, 1, 6, 0, xid, 0, flags)
    ips = b"\x00" * 16
    chaddr = mac_bytes.ljust(16, b"\x00")
    zero_fields = b"\x00" * 192
    magic_cookie = b"\x63\x82\x53\x63"

    options = bytearray()
    options.extend([53, 1, msg_type])

    if hostname:
        hostname_bytes = hostname.encode("utf-8")
        options.extend([12, len(hostname_bytes)])
        options.extend(hostname_bytes)

    if vendor_class_id:
        # Strip trailing C-style null bytes if accidentally provided
        vci_bytes = vendor_class_id.rstrip("\x00").encode("utf-8")
        options.extend([60, len(vci_bytes)])
        options.extend(vci_bytes)

    if requested_ip:
        options.extend([50, 4])
        options.extend(socket.inet_aton(requested_ip))

    if server_ip:
        options.extend([54, 4])
        options.extend(socket.inet_aton(server_ip))

    # Parameter Request List (Option 55):
    # Added Option 43 (Vendor Specific Info) so the server returns custom vendor data
    requested_options = [1, 3, 6, 15, 26, 42, 43, 60, 66, 67, 252]
    options.extend([55, len(requested_options)])
    options.extend(requested_options)
    options.append(255)

    return header + ips + chaddr + zero_fields + magic_cookie + bytes(options)


def parse_dhcp_packet(data: bytes) -> dict | None:
    if len(data) < 240:
        return None

    xid = struct.unpack("!I", data[4:8])[0]
    yiaddr = socket.inet_ntoa(data[16:20])

    options_data = data[240:]
    parsed_options = []
    i = 0

    while i < len(options_data):
        opt = options_data[i]
        if opt == 255:  # END option
            break
        if opt == 0:    # PAD option
            i += 1
            continue

        # Check if length byte exists
        if i + 1 >= len(options_data):
            break

        length = options_data[i + 1]
        
        # Check if entire payload falls within remaining options_data length
        if i + 2 + length > len(options_data):
            break

        val = bytes(options_data[i + 2 : i + 2 + length])
        name, formatted_val = decode_option(opt, val)

        parsed_options.append(
            {
                "code": opt,
                "name": name,
                "value": formatted_val,
                "raw": val,
            }
        )
        i += 2 + length

    msg_type = next((o["raw"][0] for o in parsed_options if o["code"] == 53 and o["raw"]), 0)

    return {
        "xid": xid,
        "yiaddr": yiaddr,
        "msg_type": msg_type,
        "options": parsed_options,
    }


def test_dhcp_server(
    target_host: str = "127.0.0.1",
    target_port: int = 1167,
    client_port: int = 68,
    mac_address: str = "00:11:22:33:44:55",
    hostname: str = "test-client",
    vendor_class_id: str | None = None,
    timeout: float = 5.0,
) -> dict:
    mac_bytes = mac_to_bytes(mac_address)
    xid = int.from_bytes(os.urandom(4), "big")

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    sock.bind(("0.0.0.0", client_port))
    sock.settimeout(timeout)

    use_broadcast = target_host == "255.255.255.255"

    try:
        discover_packet = build_dhcp_packet(
            mac_bytes,
            xid,
            msg_type=1,
            hostname=hostname,
            vendor_class_id=vendor_class_id,
            broadcast=use_broadcast,
        )
        sock.sendto(discover_packet, (target_host, target_port))

        offered_ip = None
        server_id = None
        offer_options = []
        start_time = time.time()

        while time.time() - start_time < timeout:
            data, addr = sock.recvfrom(1024)
            parsed = parse_dhcp_packet(data)

            if parsed and parsed["xid"] == xid and parsed["msg_type"] == 2:
                offered_ip = parsed["yiaddr"]
                offer_options = parsed["options"]
                server_id_opt = next((o["value"] for o in offer_options if o["code"] == 54), None)
                server_id = server_id_opt or target_host
                break

        if not offered_ip:
            raise TimeoutError(f"No DHCPOFFER received from {target_host}:{target_port}")

        request_packet = build_dhcp_packet(
            mac_bytes,
            xid,
            msg_type=3,
            requested_ip=offered_ip,
            server_ip=server_id,
            hostname=hostname,
            vendor_class_id=vendor_class_id,
            broadcast=use_broadcast,
        )
        sock.sendto(request_packet, (target_host, target_port))

        start_time = time.time()
        while time.time() - start_time < timeout:
            data, addr = sock.recvfrom(1024)
            parsed = parse_dhcp_packet(data)

            if parsed and parsed["xid"] == xid:
                if parsed["msg_type"] == 5:
                    return {
                        "status": "SUCCESS",
                        "leased_ip": parsed["yiaddr"],
                        "offer_options": offer_options,
                        "ack_options": parsed["options"],
                        "response_from_host": addr[0],
                        "response_from_port": addr[1],
                    }
                elif parsed["msg_type"] == 6:
                    raise RuntimeError(f"Received DHCPNAK from {addr[0]}:{addr[1]}")

        raise TimeoutError(f"No DHCPACK received from {target_host}:{target_port}")

    finally:
        sock.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Test DHCP server lease workflow.")
    parser.add_argument(
        "--server-port", "-p", type=int, default=1167, help="Target DHCP server port (default: 1167)"
    )
    parser.add_argument(
        "--client-port", "-c", type=int, default=68, help="Client UDP listening port (default: 68)"
    )
    parser.add_argument(
        "--server-ip", "-s", type=str, default="127.0.0.1", help="Target DHCP server IP (default: 127.0.0.1)"
    )
    parser.add_argument(
        "--mac", "-m", type=str, default="00:11:22:33:44:55", help="Client MAC address (default: 00:11:22:33:44:55)"
    )
    parser.add_argument(
        "--hostname", "-H", type=str, default="test-client", help="Client hostname (default: test-client)"
    )
    parser.add_argument(
        "--vendor-class", "-v", type=str, default=None, help="Option 60 Vendor Class Identifier string (optional)"
    )
    parser.add_argument(
        "--timeout", "-t", type=float, default=5.0, help="Response timeout in seconds (default: 5.0)"
    )

    args = parser.parse_args()

    print(
        f"Testing DHCP server at {args.server_ip}:{args.server_port} (Listening on client port {args.client_port})..."
    )
    try:
        result = test_dhcp_server(
            target_host=args.server_ip,
            target_port=args.server_port,
            client_port=args.client_port,
            mac_address=args.mac,
            hostname=args.hostname,
            vendor_class_id=args.vendor_class,
            timeout=args.timeout,
        )
        print("\nDHCP Lease Test Successful:")
        print(f"  Leased IP:      {result['leased_ip']}")
        print(f"  Responded From: {result['response_from_host']}:{result['response_from_port']}")

        print("\nDHCPOFFER Options:")
        for opt in result["offer_options"]:
            print(f"  Option {opt['code']:<3} ({opt['name']}): {opt['value']}")

        print("\nDHCPACK Options:")
        for opt in result["ack_options"]:
            print(f"  Option {opt['code']:<3} ({opt['name']}): {opt['value']}")

    except Exception as err:
        print(f"\nDHCP Lease Test Failed: {err}")
