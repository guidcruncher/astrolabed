# Configuration examples

Here are full example configuration files you can use as a starting point when running Astrolabed.

They are available in the project root.

## Example

Comprehensive example for local/dev running.

For documenstion on configuration see [configurationdocs.md](configurationdocs.md)


```json
{
  "Dns": {
    "Listen": {
      "Address": "127.0.0.1",
      "Port": 1053
    },
    "UpstreamTimeoutMs": 1500,
    "DefaultResolvers": [
      {
        "Name": "Cloudflare",
        "Address": "1.1.1.1",
        "Port": 53
      },
      {
        "Name": "Cloudflare Secondary",
        "Address": "1.0.0.1",
        "Port": 53
      }
    ],
    "Resolvers": [
      {
        "Name": "LocalDNS",
        "Address": "127.0.0.1",
        "Port": 5353,
        "Rule": "^localdev\\.",
        "Block": false
      },
      {
        "Name": "BlockDevAds",
        "Rule": "^(ads|tracking)\\.",
        "Block": true
      }
    ],
    "Blocklists": [],
    "Allowlists": [],
    "HostsFiles": [
      "file://../../../netdns-runtime/dns-hosts/custom.list"
    ],
    "Caching": {
      "Enabled": true,
      "TtlSeconds": 300,
      "MaxEntries": 2000,
      "CleanupIntervalMinutes": 15
    },
    "BlockResponse": {
      "Mode": "NXDOMAIN",
      "StaticIp": "0.0.0.0",
      "Ttl": 60
    },
    "ConditionalForwarding": {
      "Enabled": true,
      "DhcpServerIp": "192.168.1.1",
      "DhcpServerPort": 53,
      "LocalDomain": "lan",
      "LocalSubnetCidr": "192.168.1.0/24",
      "ForwardNonFqdn": true
    }
  },
  "Dhcp": {
    "Enabled": false,
    "ListenAddress": "0.0.0.0",
    "ListenPort": 1067,
    "LeaseStorePath": "../../../netdns-runtime/leases.json",
    "BadIpStorePath": "../../../netdns-runtime/badips.json",
    "PoolCidr": "192.168.10.0/24",
    "ServerIdentifier": "192.168.10.1",
    "Router": "192.168.10.1",
    "DnsServer": "1.1.1.1",
    "NtpServer": "192.168.10.1",
    "DomainName": "corp.internal",
    "InterfaceMtu": 1500,
    "TftpServerName": "192.168.10.5",
    "BootfileName": "pxelinux.0",
    "WebProxyServerUrl": "http://wpad.corp.internal/wpad.dat",
    "LeaseHours": 24,
    "ArpTimeoutMs": 500
  },
  "Ntp": {
    "Enabled": true,
    "ListenAddress": "127.0.0.1",
    "Port": 1123,
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
  "WebUI": {
    "Enabled": true,
    "ListenAddress": "127.0.0.1",
    "ListenPort": 1081
  },
  "Logging": {
    "Level": "Trace"
  }
}
```

### Prometheus

Metrics are exposed at `http://localhost:1080/metrics` and that Prometheus (if enabled in compose) discovers the `dnsforwarder` target.
