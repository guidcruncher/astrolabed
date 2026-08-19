#!/usr/bin/env python3

import argparse
import random
import socket
import struct
import sys
import time

RECORD_TYPES = {
    "A": 1,
    "NS": 2,
    "CNAME": 5,
    "SOA": 6,
    "PTR": 12,
    "MX": 15,
    "TXT": 16,
    "AAAA": 28,
    "SRV": 33,
}

TYPE_NAMES = {v: k for k, v in RECORD_TYPES.items()}

RCODES = {
    0: "NOERROR",
    1: "FORMERR",
    2: "SERVFAIL",
    3: "NXDOMAIN",
    4: "NOTIMP",
    5: "REFUSED",
}


def build_query(domain: str, qtype: int, tx_id: int) -> bytes:
    """Build a raw DNS wire-format query payload."""
    flags = 0x0100  # Standard query, Recursion Desired (RD = 1)
    qdcount = 1
    ancount = 0
    nscount = 0
    arcount = 0

    header = struct.pack("!HHHHHH", tx_id, flags, qdcount, ancount, nscount, arcount)

    qname = b""
    for part in domain.strip(".").split("."):
        encoded_part = part.encode("ascii")
        qname += struct.pack("!B", len(encoded_part)) + encoded_part
    qname += b"\x00"  # Root null byte

    qclass = 1  # IN (Internet)
    question = qname + struct.pack("!HH", qtype, qclass)

    return header + question


def parse_domain_name(data: bytes, offset: int) -> tuple[str, int]:
    """Parse a domain name from DNS wire format, following compression pointers."""
    labels = []
    original_offset = offset
    jumped = False
    max_jumps = 5
    jumps = 0

    while offset < len(data):
        length = data[offset]

        if length == 0:
            if not jumped:
                original_offset = offset + 1
            break

        # Check for compression pointer (0xC0 flag)
        if (length & 0xC0) == 0xC0:
            if offset + 1 >= len(data):
                raise ValueError("Truncated compression pointer")

            if not jumped:
                original_offset = offset + 2

            pointer = ((length & 0x3F) << 8) | data[offset + 1]
            offset = pointer
            jumped = True
            jumps += 1
            if jumps > max_jumps:
                raise ValueError("Infinite compression loop detected")
            continue

        offset += 1
        labels.append(data[offset : offset + length].decode("latin1"))
        offset += length

    domain = ".".join(labels)
    return domain, original_offset


def parse_rdata(rdata: bytes, rtype: int, raw_msg: bytes, rdata_offset: int) -> str:
    """Format RDATA byte contents into readable text representation."""
    if rtype == 1 and len(rdata) == 4:  # A
        return socket.inet_ntoa(rdata)

    if rtype == 28 and len(rdata) == 16:  # AAAA
        return socket.inet_ntop(socket.AF_INET6, rdata)

    if rtype in (2, 5, 12):  # NS, CNAME, PTR
        name, _ = parse_domain_name(raw_msg, rdata_offset)
        return name

    if rtype == 15 and len(rdata) >= 3:  # MX
        pref = struct.unpack("!H", rdata[:2])[0]
        exchange, _ = parse_domain_name(raw_msg, rdata_offset + 2)
        return f"{pref} {exchange}"

    if rtype == 16:  # TXT
        txt_offset = 0
        txts = []
        while txt_offset < len(rdata):
            length = rdata[txt_offset]
            txt_offset += 1
            txts.append(rdata[txt_offset : txt_offset + length].decode("utf-8", errors="replace"))
            txt_offset += length
        return " ".join(f'"{t}"' for t in txts)

    return rdata.hex()


def parse_records(
    data: bytes, offset: int, count: int
) -> tuple[list[tuple[str, str, int, str]], int]:
    """Parse a section of Resource Records."""
    records = []
    for _ in range(count):
        name, offset = parse_domain_name(data, offset)
        rtype_num, rclass_num, ttl, rdlength = struct.unpack("!HHIH", data[offset : offset + 10])
        offset += 10

        rdata_offset = offset
        rdata = data[offset : offset + rdlength]
        offset += rdlength

        type_str = TYPE_NAMES.get(rtype_num, str(rtype_num))
        rdata_str = parse_rdata(rdata, rtype_num, data, rdata_offset)
        records.append((name, type_str, ttl, rdata_str))

    return records, offset


def parse_response(data: bytes) -> dict:
    """Parse a full raw DNS response packet."""
    tx_id, flags, qdcount, ancount, nscount, arcount = struct.unpack("!HHHHHH", data[:12])

    rcode_num = flags & 0x0F
    rcode_str = RCODES.get(rcode_num, f"RCODE_{rcode_num}")

    # Extract Flags
    aa = bool(flags & 0x0400)
    tc = bool(flags & 0x0200)
    rd = bool(flags & 0x0100)
    ra = bool(flags & 0x0080)

    flags_list = []
    if qr := bool(flags & 0x8000): flags_list.append("qr")
    if aa: flags_list.append("aa")
    if tc: flags_list.append("tc")
    if rd: flags_list.append("rd")
    if ra: flags_list.append("ra")

    offset = 12

    # Questions Section
    questions = []
    for _ in range(qdcount):
        qname, offset = parse_domain_name(data, offset)
        qtype_num, qclass_num = struct.unpack("!HH", data[offset : offset + 4])
        offset += 4
        qtype_str = TYPE_NAMES.get(qtype_num, str(qtype_num))
        questions.append((qname, qtype_str))

    # Resource Sections
    answers, offset = parse_records(data, offset, ancount)
    authorities, offset = parse_records(data, offset, nscount)
    additionals, offset = parse_records(data, offset, arcount)

    return {
        "id": tx_id,
        "rcode": rcode_str,
        "flags": flags_list,
        "questions": questions,
        "answers": answers,
        "authorities": authorities,
        "additionals": additionals,
    }


def print_dig_output(
    res: dict,
    elapsed_ms: float,
    server: str,
    port: int,
    raw_bytes_len: int,
):
    """Format output matching standard dig structure."""
    print(f";; ->>HEADER<<- opcode: QUERY, status: {res['rcode']}, id: {res['id']}")
    flags_str = " ".join(res["flags"])
    print(
        f";; flags: {flags_str}; QUERY: {len(res['questions'])}, "
        f"ANSWER: {len(res['answers'])}, AUTHORITY: {len(res['authorities'])}, "
        f"ADDITIONAL: {len(res['additionals'])}\n"
    )

    print(";; QUESTION SECTION:")
    for qname, qtype in res["questions"]:
        print(f";{qname}.\t\tIN\t{qtype}")
    print()

    if res["answers"]:
        print(";; ANSWER SECTION:")
        for name, rtype, ttl, rdata in res["answers"]:
            print(f"{name}.\t\t{ttl}\tIN\t{rtype}\t{rdata}")
        print()

    if res["authorities"]:
        print(";; AUTHORITY SECTION:")
        for name, rtype, ttl, rdata in res["authorities"]:
            print(f"{name}.\t\t{ttl}\tIN\t{rtype}\t{rdata}")
        print()

    if res["additionals"]:
        print(";; ADDITIONAL SECTION:")
        for name, rtype, ttl, rdata in res["additionals"]:
            print(f"{name}.\t\t{ttl}\tIN\t{rtype}\t{rdata}")
        print()

    now_str = time.strftime("%a %b %d %H:%M:%S %Z %Y", time.localtime())
    print(f";; Query time: {elapsed_ms:.2f} msec")
    print(f";; SERVER: {server}#{port}({server})")
    print(f";; WHEN: {now_str}")
    print(f";; MSG SIZE  rcvd: {raw_bytes_len}")


def dig(
    domain: str,
    qtype_str: str,
    server: str,
    port: int = 53,
    client_ip: str = "0.0.0.0",
    tcp: bool = False,
    timeout: float = 5.0,
):
    qtype = RECORD_TYPES.get(qtype_str.upper())
    if qtype is None:
        print(f"Error: Unsupported record type '{qtype_str}'", file=sys.stderr)
        sys.exit(1)

    tx_id = random.randint(1, 65535)
    query_bytes = build_query(domain, qtype, tx_id)

    start = time.perf_counter()

    try:
        if tcp:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(timeout)
            sock.bind((client_ip, 0))
            sock.connect((server, port))

            tcp_payload = struct.pack("!H", len(query_bytes)) + query_bytes
            sock.sendall(tcp_payload)

            len_buf = sock.recv(2)
            if not len_buf:
                raise ConnectionResetError("Empty response")
            resp_len = struct.unpack("!H", len_buf)[0]

            response_bytes = b""
            while len(response_bytes) < resp_len:
                chunk = sock.recv(resp_len - len(response_bytes))
                if not chunk:
                    break
                response_bytes += chunk
            sock.close()
        else:
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.settimeout(timeout)
            sock.bind((client_ip, 0))
            sock.sendto(query_bytes, (server, port))
            response_bytes, _ = sock.recvfrom(4096)
            sock.close()

    except socket.timeout:
        print(";; connection timed out; no servers could be reached", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

    elapsed_ms = (time.perf_counter() - start) * 1000
    res = parse_response(response_bytes)

    print_dig_output(res, elapsed_ms, server, port, len(response_bytes))


def main():
    # Pre-parse command-line arguments to intercept dig-style @server inputs
    server_override = "1.1.1.1"
    cleaned_argv = []

    for arg in sys.argv[1:]:
        if arg.startswith("@"):
            server_override = arg.lstrip("@")
        else:
            cleaned_argv.append(arg)

    parser = argparse.ArgumentParser(description="Standard library python dig clone")
    parser.add_argument("domain", help="Domain name to query")
    parser.add_argument(
        "type",
        nargs="?",
        default="A",
        help="Record type (A, AAAA, MX, TXT, NS, SOA, PTR, CNAME) [Default: A]",
    )
    parser.add_argument(
        "-s",
        "--server",
        default=server_override,
        help="DNS Server IP (e.g. 1.1.1.1 or @1.1.1.1) [Default: 1.1.1.1]",
    )
    parser.add_argument(
        "-b",
        "--client-ip",
        default="0.0.0.0",
        help="Source IP address to bind outbound query socket to [Default: 0.0.0.0 (Current IP)]",
    )
    parser.add_argument("--tcp", action="store_true", help="Use TCP mode")
    parser.add_argument("-p", "--port", type=int, default=53, help="Port [Default: 53]")
    parser.add_argument("-t", "--timeout", type=float, default=5.0, help="Timeout in seconds")

    args = parser.parse_args(cleaned_argv)

    nameserver = args.server.lstrip("@")

    dig(
        domain=args.domain,
        qtype_str=args.type,
        server=nameserver,
        port=args.port,
        client_ip=args.client_ip,
        tcp=args.tcp,
        timeout=args.timeout,
    )


if __name__ == "__main__":
    main()
