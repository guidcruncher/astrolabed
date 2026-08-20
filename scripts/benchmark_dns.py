import argparse
import socket
import statistics
import sys
import time
from concurrent.futures import ThreadPoolExecutor, as_completed


def build_dns_query(hostname: str, transaction_id: int = 0x1234) -> bytes:
    """Builds a standard DNS A record query packet."""
    header = transaction_id.to_bytes(2, byteorder="big") + b"\x01\x00\x00\x01\x00\x00\x00\x00\x00\x00"

    qname = b""
    for part in hostname.strip(".").split("."):
        encoded_part = part.encode("ascii")
        qname += len(encoded_part).to_bytes(1, byteorder="big") + encoded_part
    qname += b"\x00"

    qtype_qclass = b"\x00\x01\x00\x01"

    return header + qname + qtype_qclass


def send_dns_query(ip: str, port: int, hostname: str, timeout: float) -> tuple[bool, float]:
    """Sends a single DNS query over UDP and measures round-trip latency in milliseconds."""
    packet = build_dns_query(hostname)
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(timeout)

    start_time = time.perf_counter()
    try:
        sock.sendto(packet, (ip, port))
        data, _ = sock.recvfrom(512)
        latency = (time.perf_counter() - start_time) * 1000.0
        if len(data) >= 2 and data[:2] == packet[:2]:
            return True, latency
        return False, latency
    except (socket.timeout, TimeoutError, OSError):
        latency = (time.perf_counter() - start_time) * 1000.0
        return False, latency
    finally:
        sock.close()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Benchmarking tool for DNS servers using built-in Python libraries.")
    parser.add_argument("--ip", required=True, type=str, help="DNS server IP address")
    parser.add_argument("--port", type=int, default=53, help="DNS server port (default: 53)")
    parser.add_argument("--hostname", type=str, default="example.com", help="Domain name to query (default: example.com)")
    parser.add_argument("-c", "--concurrency", type=int, default=10, help="Number of concurrent worker threads (default: 10)")
    parser.add_argument("-n", "--requests", type=int, default=100, help="Total number of queries to send (default: 100)")
    parser.add_argument("-t", "--timeout", type=float, default=2.0, help="Socket timeout in seconds (default: 2.0)")

    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        sys.exit(1)

    return parser.parse_args()


def main() -> None:
    args = parse_args()

    print(f"Starting DNS Benchmark against {args.ip}:{args.port}")
    print(f"Target Hostname: {args.hostname} | Total Requests: {args.requests} | Concurrency: {args.concurrency}\n")

    latencies: list[float] = []
    successes = 0
    failures = 0

    benchmark_start = time.perf_counter()

    with ThreadPoolExecutor(max_workers=args.concurrency) as executor:
        futures = [
            executor.submit(send_dns_query, args.ip, args.port, args.hostname, args.timeout)
            for _ in range(args.requests)
        ]

        for future in as_completed(futures):
            success, latency = future.result()
            if success:
                successes += 1
                latencies.append(latency)
            else:
                failures += 1

    total_time = time.perf_counter() - benchmark_start

    print("--- Benchmark Results ---")
    print(f"Total Time Elapsed : {total_time:.2f} seconds")
    print(f"Successful Queries : {successes}")
    print(f"Failed Queries     : {failures}")
    print(f"Throughput         : {args.requests / total_time:.2f} QPS (queries/sec)")

    if latencies:
        print("\n--- Latency Stats (ms) ---")
        print(f"Min Latency        : {min(latencies):.2f} ms")
        print(f"Max Latency        : {max(latencies):.2f} ms")
        print(f"Avg Latency        : {statistics.mean(latencies):.2f} ms")
        print(f"Median Latency     : {statistics.median(latencies):.2f} ms")
        if len(latencies) > 1:
            print(f"Std Dev            : {statistics.stdev(latencies):.2f} ms")


if __name__ == "__main__":
    main()
