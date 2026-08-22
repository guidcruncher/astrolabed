#!/usr/bin/env python3
import argparse
import datetime
import socket
import struct
import sys
import time

NTP_EPOCH_OFFSET = 2208988800  # Seconds between 1900-01-01 and 1970-01-01

LEAP_INDICATORS = {
    0: "0 - No Warning",
    1: "1 - Last minute has 61 seconds",
    2: "2 - Last minute has 59 seconds",
    3: "3 - Unknown / Unsynchronized"
}

NTP_MODES = {
    0: "0 - Reserved",
    1: "1 - Symmetric Active",
    2: "2 - Symmetric Passive",
    3: "3 - Client",
    4: "4 - Server",
    5: "5 - Broadcast",
    6: "6 - Control Message",
    7: "7 - Reserved for Private Use"
}

def parse_ntp_timestamp(sec: int, frac: int) -> str:
    if sec == 0 and frac == 0:
        return "0 (Unset)"
    unix_time = sec - NTP_EPOCH_OFFSET
    fractional_seconds = frac / 4294967296.0
    dt = datetime.datetime.fromtimestamp(unix_time, tz=datetime.timezone.utc)
    dt_with_frac = dt + datetime.timedelta(seconds=fractional_seconds)
    return f"{dt_with_frac.strftime('%Y-%m-%d %H:%M:%S.%f')} UTC (Sec: 0x{sec:08X}, Frac: 0x{frac:08X})"

def parse_ref_id(ref_id_int: int, stratum: int) -> str:
    ref_bytes = struct.pack("!I", ref_id_int)
    if stratum in (0, 1):
        try:
            ascii_str = ref_bytes.decode('ascii').rstrip('\x00')
            return f"'{ascii_str}' (Kiss-o'-Death / Reference Identifier)"
        except UnicodeDecodeError:
            pass
    try:
        ip_str = socket.inet_ntoa(ref_bytes)
        return f"{ip_str} (IPv4 Address)"
    except Exception:
        return f"0x{ref_id_int:08X}"

def parse_fixed_point_16_16(value: int) -> float:
    integer_part = (value >> 16) & 0xFFFF
    fraction_part = value & 0xFFFF
    return integer_part + (fraction_part / 65536.0)

def main():
    parser = argparse.ArgumentParser(description="RFC 5905 Compliant NTP Server CLI Test Tool")
    parser.add_argument("--ip", default="127.0.0.1", help="Target NTP Server IP address (default: 127.0.0.1)")
    parser.add_argument("--port", type=int, default=123, help="Target NTP Server UDP port (default: 123)")
    parser.add_argument("--timeout", type=float, default=3.0, help="Socket timeout in seconds (default: 3.0)")

    args = parser.parse_args()

    # Construct 48-byte NTP client request packet
    # First byte 0x23: LI = 0 (00), VN = 4 (100), Mode = 3 Client (011) -> 00100011 = 0x23
    client_packet = bytearray(48)
    client_packet[0] = 0x23

    # Set transmit timestamp to current time
    t_send = time.time()
    t_send_sec = int(t_send) + NTP_EPOCH_OFFSET
    t_send_frac = int((t_send - int(t_send)) * 4294967296)
    struct.pack_into("!II", client_packet, 40, t_send_sec, t_send_frac)

    print("=" * 70)
    print(f" Sending NTP Client Request to {args.ip}:{args.port}")
    print("=" * 70)

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(args.timeout)

    try:
        start_time = time.time()
        sock.sendto(client_packet, (args.ip, args.port))
        response, addr = sock.recvfrom(1024)
        rtt_ms = (time.time() - start_time) * 1000.0

        if len(response) < 48:
            print(f"[ERROR] Received incomplete packet ({len(response)} bytes). Minimum required is 48 bytes.")
            sys.exit(1)

        # Unpack header bytes
        first_byte = response[0]
        li = (first_byte >> 6) & 0x03
        vn = (first_byte >> 3) & 0x07
        mode = first_byte & 0x07

        stratum = response[1]
        poll = struct.unpack("!b", response[2:3])[0]
        precision = struct.unpack("!b", response[3:4])[0]

        root_delay_raw = struct.unpack("!I", response[4:8])[0]
        root_dispersion_raw = struct.unpack("!I", response[8:12])[0]
        ref_id_raw = struct.unpack("!I", response[12:16])[0]

        ref_sec, ref_frac = struct.unpack("!II", response[16:24])
        org_sec, org_frac = struct.unpack("!II", response[24:32])
        rec_sec, rec_frac = struct.unpack("!II", response[32:40])
        xmt_sec, xmt_frac = struct.unpack("!II", response[40:48])

        ext_fields = response[48:]

        # Display results
        print("\n[ NETWORK METRICS ]")
        print(f"  Remote Server      : {addr[0]}:{addr[1]}")
        print(f"  Packet Size        : {len(response)} bytes")
        print(f"  Round Trip Time    : {rtt_ms:.3f} ms")

        print("\n[ HEADER FIELDS ]")
        print(f"  Leap Indicator     : {LEAP_INDICATORS.get(li, str(li))}")
        print(f"  Version Number     : {vn}")
        print(f"  Mode               : {NTP_MODES.get(mode, str(mode))}")
        print(f"  Stratum            : {stratum} ({'Primary Reference' if stratum == 1 else 'Secondary Reference' if stratum > 1 else 'Unsynchronized'})")
        print(f"  Poll Interval      : {poll} (2^{poll} = {2**poll} seconds)")
        print(f"  Precision          : {precision} (2^{precision} = {2**precision:.10f} seconds)")

        print("\n[ CLOCK PROPERTIES ]")
        print(f"  Root Delay         : {parse_fixed_point_16_16(root_delay_raw):.6f} seconds")
        print(f"  Root Dispersion    : {parse_fixed_point_16_16(root_dispersion_raw):.6f} seconds")
        print(f"  Reference ID       : {parse_ref_id(ref_id_raw, stratum)}")

        print("\n[ TIMESTAMPS ]")
        print(f"  Reference Timestamp: {parse_ntp_timestamp(ref_sec, ref_frac)}")
        print(f"  Origin Timestamp   : {parse_ntp_timestamp(org_sec, org_frac)}")
        print(f"  Receive Timestamp  : {parse_ntp_timestamp(rec_sec, rec_frac)}")
        print(f"  Transmit Timestamp : {parse_ntp_timestamp(xmt_sec, xmt_frac)}")

        if ext_fields:
            print("\n[ EXTENSION FIELDS ]")
            print(f"  Raw Hex ({len(ext_fields)} bytes): {ext_fields.hex()}")

        print("=" * 70)

    except socket.timeout:
        print(f"\n[ERROR] Request timed out after {args.timeout} seconds while reaching {args.ip}:{args.port}")
    except Exception as e:
        print(f"\n[ERROR] An error occurred: {e}")
    finally:
        sock.close()

if __name__ == "__main__":
    main()
