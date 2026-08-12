import os
import socket
import struct
import time


def mac_to_bytes(mac_str: str) -> bytes:
    return bytes.fromhex(mac_str.replace(":", "").replace("-", ""))


def build_dhcp_packet(
    mac_bytes: bytes,
    xid: int,
    msg_type: int,
    requested_ip: str | None = None,
    server_ip: str | None = None,
    broadcast: bool = False,
) -> bytes:
    # Header: op=1 (BOOTREQUEST), htype=1 (Ethernet), hlen=6, hops=0
    # Set flags to 0x8000 for broadcast, or 0x0000 for unicast response
    flags = 0x8000 if broadcast else 0x0000
    header = struct.pack("!BBBBIHH", 1, 1, 6, 0, xid, 0, flags)

    # ciaddr, yiaddr, siaddr, giaddr (4 bytes each)
    ips = b"\x00" * 16

    # Client hardware address (16 bytes total including padding)
    chaddr = mac_bytes.ljust(16, b"\x00")

    # sname (64 bytes) and file (128 bytes)
    zero_fields = b"\x00" * 192

    # Magic Cookie (4 bytes)
    magic_cookie = b"\x63\x82\x53\x63"

    # DHCP Options
    options = bytearray()

    # Option 53: DHCP Message Type (1 = Discover, 3 = Request)
    options.extend([53, 1, msg_type])

    if requested_ip:
        # Option 50: Requested IP Address
        options.extend([50, 4])
        options.extend(socket.inet_aton(requested_ip))

    if server_ip:
        # Option 54: Server Identifier
        options.extend([54, 4])
        options.extend(socket.inet_aton(server_ip))

    # Option 55: Parameter Request List (Subnet Mask, Router, DNS)
    options.extend([55, 3, 1, 3, 6])

    # Option 255: End
    options.append(255)

    return header + ips + chaddr + zero_fields + magic_cookie + bytes(options)


def parse_dhcp_packet(data: bytes) -> dict | None:
    if len(data) < 240:
        return None

    xid = struct.unpack("!I", data[4:8])[0]
    yiaddr = socket.inet_ntoa(data[16:20])

    # Parse options starting after 240-byte standard BOOTP header
    options = data[240:]
    parsed_options = {}
    i = 0

    while i < len(options):
        opt = options[i]
        if opt == 255:  # End option
            break
        if opt == 0:  # Pad option
            i += 1
            continue

        if i + 1 >= len(options):
            break

        length = options[i + 1]
        val = options[i + 2 : i + 2 + length]
        parsed_options[opt] = val
        i += 2 + length

    msg_type = parsed_options.get(53, b"\x00")[0]

    server_id = None
    if 54 in parsed_options and len(parsed_options[54]) == 4:
        server_id = socket.inet_ntoa(parsed_options[54])

    return {
        "xid": xid,
        "yiaddr": yiaddr,
        "msg_type": msg_type,
        "server_id": server_id,
    }


def test_dhcp_server(
    target_host: str = "127.0.0.1",
    target_port: int = 1067,
    client_port: int = 68,
    mac_address: str = "00:11:22:33:44:55",
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
        # Step 1: Send targeted DHCPDISCOVER
        discover_packet = build_dhcp_packet(
            mac_bytes, xid, msg_type=1, broadcast=use_broadcast
        )
        sock.sendto(discover_packet, (target_host, target_port))

        # Step 2: Receive DHCPOFFER
        offered_ip = None
        server_id = None
        start_time = time.time()

        while time.time() - start_time < timeout:
            data, addr = sock.recvfrom(1024)
            parsed = parse_dhcp_packet(data)

            # msg_type 2 = DHCPOFFER
            if parsed and parsed["xid"] == xid and parsed["msg_type"] == 2:
                offered_ip = parsed["yiaddr"]
                server_id = parsed["server_id"] or target_host
                break

        if not offered_ip:
            raise TimeoutError(f"No DHCPOFFER received from {target_host}:{target_port}")

        # Step 3: Send targeted DHCPREQUEST
        request_packet = build_dhcp_packet(
            mac_bytes,
            xid,
            msg_type=3,
            requested_ip=offered_ip,
            server_ip=server_id,
            broadcast=use_broadcast,
        )
        sock.sendto(request_packet, (target_host, target_port))

        # Step 4: Receive DHCPACK
        start_time = time.time()
        while time.time() - start_time < timeout:
            data, addr = sock.recvfrom(1024)
            parsed = parse_dhcp_packet(data)

            if parsed and parsed["xid"] == xid:
                if parsed["msg_type"] == 5:  # DHCPACK
                    return {
                        "status": "SUCCESS",
                        "leased_ip": parsed["yiaddr"],
                        "server_id": server_id,
                        "response_from_host": addr[0],
                        "response_from_port": addr[1],
                    }
                elif parsed["msg_type"] == 6:  # DHCPNAK
                    raise RuntimeError(f"Received DHCPNAK from {addr[0]}:{addr[1]}")

        raise TimeoutError(f"No DHCPACK received from {target_host}:{target_port}")

    finally:
        sock.close()


if __name__ == "__main__":
    TARGET_SERVER_IP = "127.0.0.1"
    TARGET_SERVER_PORT = 1067
    CLIENT_LISTEN_PORT = 68

    print(f"Testing DHCP server at {TARGET_SERVER_IP}:{TARGET_SERVER_PORT}...")
    try:
        result = test_dhcp_server(
            target_host=TARGET_SERVER_IP,
            target_port=TARGET_SERVER_PORT,
            client_port=CLIENT_LISTEN_PORT,
            mac_address="00:11:22:33:44:55",
            timeout=5.0,
        )
        print("DHCP Lease Test Successful:")
        print(f"  Leased IP:          {result['leased_ip']}")
        print(f"  Server ID Option:   {result['server_id']}")
        print(f"  Responded From:     {result['response_from_host']}:{result['response_from_port']}")
    except Exception as err:
        print(f"DHCP Lease Test Failed: {err}")
