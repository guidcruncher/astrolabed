# Usage

## Quick start (local)
1. Build and run:
   - dotnet run --project src/Astrolabed/Astrolabed.csproj -- --config src/Astrolabed/appsettings.Development.json

2. Run in Docker (single container):
   - docker build -t dnsforwarder .
   - docker run -p 53:53/udp -p 1080:1080 dnsforwarder -- --config /app/appsettings.Docker.json

3. Run with docker-compose (recommended for multi-protocol exposure):
   - docker-compose -f docs/docker-compose.yml up --build

4. Use dig to test:
   - dig @127.0.0.1 -p 1053 example.com

## Configuration (appsettings.json)

Configuearation is storesd im a JSON text file called appsettings.json which should be located im the same folder as the application.
	
See [configuration.md](configuration.md) for example configuratiob.

## Configuration Notes

### Logging levels

Log detail levels are controlled by the Logging.Level configuration property.

```json
"Logging": {
  "Level": "Debug"
}
```

Log levels include (In order of granularity) :

- Trace
- Debug
- Information
- Warning
- Error
- Critical
- None

### DNS Block responses

There are different ways of returning a block response. This is controlled by the "BlockResponse" setting.

#### NXDOMAIN

Returns a standard NXDOMAIN response. The DNS server tells the client that the requested domain name does not exist in the Domain Name System.

Standard applications expect non-existent domains as a normal response and handle them gracefully.

Browsers and network stacks immediately stop trying to connect to the domain without attempting TCP/TLS handshakes, saving network bandwidth.

Operating systems cache NXDOMAIN according to the Negative Cache TTL, preventing repeated unnecessary requests for a short window.

```json
  "BlockResponse": {
    "Mode": "NXDOMAIN",
    "Ttl": 60
  }
```

#### SERVFAIL

Returns a standard SERVFAIL response.

The DNS server tells the client that an internal error occurred while trying to process the request (e.g., DNSSEC validation failure or upstream resolver error).

Hardens privacy against telemetry scripts that attempt complex fallback mechanisms, as the client treats the DNS server as degraded.

```json
  "BlockResponse": {
    "Mode": "SERVFAIL",
    "Ttl": 60
  }
```

#### REFUSED

Returns a standard REFUSED response.

The DNS server explicitly refuses to process the query due to policy or access controls (e.g., IP address not allowed or query type forbidden).

Accurately reflects that the server actively chose not to fulfill the request due to policy enforcement.

Like NXDOMAIN, the client receives a fast failure response without waiting for a connection timeout.

```json
  "BlockResponse": {
    "Mode": "REFUSED",
    "Ttl": 60
  }
```

#### STATIC_IP

Returns a specific IP Address, this can point to 127.0.0.1, 0.0.0.0 or an address of your choice.

While not a standard DNS error status, returning ⁠0.0.0.0⁠ (or ⁠127.0.0.1⁠) with a ⁠NOERROR⁠ status code is the most widely adopted block method in modern DNS ad blockers.

Since ⁠NOERROR⁠ is returned, the client operating system considers the lookup 100% successful and will not retry against secondary DNS servers.

⁠0.0.0.0⁠ fails instantly at the socket layer without waiting for a TCP timeout, keeping page load times fast.


```json
  "BlockResponse": {
    "Mode": "STATIC_IP",
    "StaticIp": "0.0.0.0",
    "Ttl": 60
  }
```

## Exposed ports in the Docker Compose example- DNS (UDP) — 53

- DHCP (UDP) — 67 (server), 68 (client) — only relevant if DHCP mode is enabled
- NTP (UDP) — 123
- Metrics (HTTP) — 1080

## Metrics & Logging

- Prometheus-style metrics are exposed at: `http://127.0.0.1:1080/metrics` (example).
- Structured logging includes a `RequestId` for tracing individual DNS requests.


## Testing

In addition to the Unit tests, several Python test scripts exist to make real-world requests against the server amd show the results.

These can be found in ./tests/scripts

### DNS

```bash
$ python3 ./test_dns.py 
usage: test_dns.py [-h] [-s SERVER] [-b CLIENT_IP] [--tcp] [-p PORT]
                   [-t TIMEOUT]
                   domain [type]
test_dns.py: error: the following arguments are required: domain
```

```bash
$ python3 ./test_dns.py -s 1.1.1.1 -p 53 bbc.com A
;; ->>HEADER<<- opcode: QUERY, status: NOERROR, id: 31424
;; flags: qr rd ra; QUERY: 1, ANSWER: 4, AUTHORITY: 0, ADDITIONAL: 0

;; QUESTION SECTION:
;bbc.com.               IN      A

;; ANSWER SECTION:
bbc.com.                121     IN      A       151.101.64.81
bbc.com.                121     IN      A       151.101.128.81
bbc.com.                121     IN      A       151.101.192.81
bbc.com.                121     IN      A       151.101.0.81

;; Query time: 5.36 msec
;; SERVER: 1.1.1.1#53(1.1.1.1)
;; WHEN: Thu Aug 13 03:05:18 BST 2026
;; MSG SIZE  rcvd: 89
```

### DHCP

Note this script must be run with elevated privileges such as sudo.

```bash
$ sudo python3 ./test_dhcp.py --server-port 1067 --client-port 68
```

### NTP

This script connects to 127.0.0.1:1123 only.

```bash
$ python3 ./test_ntp.py 
```


