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

```json
{
  "Dns": {
    "Listen": {
      "Address": "0.0.0.0",
      "Port": 5353
    },
    "DefaultResolvers": [{
      "Name": "Cloudflare",
      "Address": "1.1.1.1",
      "Port": 53
    }],
    "Resolvers": [
      {
        "Name": "InternalDNS",
        "Address": "10.0.0.10",
        "Port": 53,
        "Rule": "^(.+\\.corp\\.local)$",
        "Block": false
      },
      {
        "Name": "BlockTracking",
        "Rule": "^(tracking\\.|ads\\.).*",
        "Block": true
      },
      {
        "Name": "GoogleDNS",
        "Address": "8.8.8.8",
        "Port": 53,
        "Rule": "^google\\.com$",
        "Block": false
      }
    ],
    "Blocklists": [
    ],
    "Allowlists": [
    ],
    "HostsFiles": [
    ],
    "BlockResponse": {
      "Mode": "NXDOMAIN",
      "StaticIp": "0.0.0.0",
      "Ttl": 60
    },
    "Caching": {
      "Enabled": true,
      "TtlSeconds": 300,
      "MaxEntries": 10000
    }
  },
  "Dhcp": {
    "Enabled": true,
    "ListenAddress": "0.0.0.0",
    "ListenPort": 67,
    "LeaseStorePath": "/var/lib/dnsforwarder/leases.json",
    "PoolCidr": "192.168.10.0/24",

    "ServerIdentifier": "192.168.10.1",
    "Router": "192.168.10.1",
    "DnsServer": "1.1.1.1",
    "NtpServer": "",

    "LeaseHours": 1,

    "ArpTimeoutMs": 500,

    "BadIpStorePath": "/var/lib/dnsforwarder/badips.json"
  },
  "Ntp": {
    "Enabled": true,
    "ListenAddress": "0.0.0.0",
    "Port": 123,
    "BufferSize": 65536,
    "Stratum": 1,
    "ReferenceId": "LOCL",
    "Upstream": {
      "Enabled": true,
      "Servers": [
        "0.pool.ntp.org",
        "1.pool.ntp.org"
      ],
      "PollIntervalSeconds": 16
    }
  },
  "Metrics": {
    "Enabled": true,
    "StorageEngine": "prometheus",
    "Location": "/metrics",
    "ListenAddress": "127.0.0.1",
    "ListenPort": 1080
  },
  "Logging": {
    "Level": "Debug"
  }
}
```

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
