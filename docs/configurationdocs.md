# Configuration Reference

Comprehensive Documentation & Schema Specifications for NetDNS Runtime JSON Configurations

## 1. Dns Configuration ("Dns")

Controls global Domain Name System (DNS) server behavior, listening sockets, forward resolvers, caching policies, filtering/blocking behavior, and conditional forwarding rules.

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| Listen.Address | String (IPv4/v6) | "127.0.0.1" | IP address the DNS server binds to for listening to incoming client queries. |
| Listen.Port | Integer (1-65535) | 1053 | Network port on which the DNS server listens. Standard DNS uses 53. |
| UpstreamTimeoutMs | Integer (ms) | 1500 | Maximum timeout in milliseconds to wait for a response from upstream DNS resolvers. |
| DefaultResolvers | Array<Object> | [ { "Name": "Cloudflare",... } ] | List of default upstream resolvers used for general internet resolution. Each resolver has Name (String), Address (String), and Port (Integer). |
| Resolvers | Array<Object> | [ { "Rule": "^localdev\.",... } ] | Targeted resolvers bound to specific domain regex rules. Properties include Name, Address, Port, Rule (Regex), and Block (Boolean). |
| Blocklists | Array<String> | [ "https://.../list.txt" ] | List of URIs or local file paths containing ad/malware domain blocklists. |
| Allowlists | Array<String> | [ "https://.../allow.txt" ] | List of URIs or local file paths containing domain allowlists to override blocklists. |
| HostsFiles | Array<String> | [ "file://.../custom.list" ] | Paths to custom hosts files containing static hostname-to-IP mappings. |
| Caching.Enabled | Boolean | true | Enables or disables in-memory DNS query response caching. |
| Caching.TtlSeconds | Integer (seconds) | 300 | Default Time-To-Live duration in seconds for cached DNS query records. |
| Caching.MaxEntries | Integer | 2000 | Maximum number of entries allowed in the DNS memory cache before LRU eviction. |
| Caching.CleanupIntervalMinutes | Integer (minutes) | 15 | Interval in minutes for running background task to purge expired cache records. |
| BlockResponse.Mode | String (Enum) | "NXDOMAIN" | Response mode for blocked domains. Options include NXDOMAIN, REFUSED, or StaticIp. |
| BlockResponse.StaticIp | String (IPv4) | "0.0.0.0" | Static IP address returned when Mode is set to static IP override. |
| BlockResponse.Ttl | Integer (seconds) | 60 | TTL value returned to clients for blocked DNS responses. |
| ConditionalForwarding.Enabled | Boolean | true | Enables forwarding local network domain queries to a primary DHCP/router DNS server. |
| ConditionalForwarding.DhcpServerIp | String (IPv4) | "192.168.1.1" | IP address of the DHCP/upstream router server handling local hostname resolution. |
| ConditionalForwarding.DhcpServerPort | Integer (1-65535) | 53 | Port on the upstream target server for local domain queries. |
| ConditionalForwarding.LocalDomain | String | "lan" | Local top-level domain suffix (e.g..lan or.local). |
| ConditionalForwarding.LocalSubnetCidr | String (CIDR) | "192.168.1.0/24" | Subnet mask in CIDR notation used to identify reverse lookup PTR requests. |
| ConditionalForwarding.ForwardNonFqdn | Boolean | true | Determines whether single-label queries (non-FQDNs) should be forwarded to the local domain resolver. |

## 2. DHCP Configuration ("Dhcp")

Manages Dynamic Host Configuration Protocol (DHCP) server settings, IP allocation pools, lease storage, PXE boot parameters, and web proxy (WPAD) auto-discovery.

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| Enabled | Boolean | true | Enables or disables the built-in DHCP server engine. |
| ListenAddress | String (IPv4) | "0.0.0.0" | Network interface address for listening to DHCP DISCOVER packets. 0.0.0.0 binds all interfaces. |
| ListenPort | Integer (1-65535) | 1067 | UDP listening port for DHCP server. Standard DHCP server port is 67. |
| LeaseStorePath | String (Path) | ".../leases.json" | File path where persistent DHCP IP lease allocations are saved in JSON format. |
| BadIpStorePath | String (Path) | ".../badips.json" | File path storing detected IP conflicts and blacklisted IP addresses. |
| PoolCidr | String (CIDR) | "192.168.10.0/24" | IP address pool range formatted in CIDR notation from which dynamic IPs are leased. |
| ServerIdentifier | String (IPv4) | "192.168.10.1" | IP address identifying this DHCP server to client devices (DHCP Option 54). |
| Router | String (IPv4) | "192.168.10.1" | Default Gateway IP address assigned to DHCP clients (DHCP Option 3). |
| DnsServer | String (IPv4) | "1.1.1.1" | Primary DNS server IP address handed out to connecting clients (DHCP Option 6). |
| NtpServer | String (IPv4) | "192.168.10.1" | Network Time Protocol (NTP) server IP address provided to clients (DHCP Option 42). |
| DomainName | String | "corp.internal" | Domain name suffix assigned to clients for local hostname resolution (DHCP Option 15). |
| InterfaceMtu | Integer (bytes) | 1500 | Maximum Transmission Unit (MTU) size assigned to client network interfaces (DHCP Option 26). |
| TftpServerName | String (IP/Host) | "192.168.10.5" | TFTP server IP/hostname used for PXE network booting (DHCP Option 66). |
| BootfileName | String (Path) | "pxelinux.0" | Path/filename of the network boot program for PXE clients (DHCP Option 67). |
| WebProxyServerUrl | String (URL) | "http://.../wpad.dat" | URL pointing to WPAD auto-proxy setup script (DHCP Option 252). |
| LeaseHours | Integer (hours) | 24 | Duration in hours for which dynamic IP address leases remain valid. |
| ArpTimeoutMs | Integer (ms) | 500 | Timeout in milliseconds for ARP probe verification before offering an IP. |

## 3. NTP Configuration ("Ntp")

Configures the Network Time Protocol (NTP) service, controlling stratum depth, UDP socket binding, and upstream sync servers.

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| Enabled | Boolean | true | Enables or disables the NTP server module. |
| ListenAddress | String (IPv4/v6) | "127.0.0.1" | IP address the NTP UDP server binds to. |
| Port | Integer (1-65535) | 1123 | UDP port used for NTP communication. Standard NTP uses UDP port 123. |
| BufferSize | Integer (bytes) | 65536 | Socket receive/send buffer allocation size in bytes. |
| Stratum | Integer (1-15) | 1 | Stratum level reported by this server (1 = primary reference source). |
| ReferenceId | String (4-Char) | "LOCL" | Four-character ASCII code identifying the reference clock source (e.g. LOCL, GPS, PPS). |
| Upstream.Enabled | Boolean | true | Enables synchronization with upstream reference NTP servers. |
| Upstream.Servers | Array<String> | [ "0.pool.ntp.org" ] | List of hostnames or IP addresses of upstream reference NTP servers. |
| Upstream.PollIntervalSeconds | Integer (seconds) | 16 | Interval in seconds between time synchronization queries to upstream servers. |

## 4. Operations & Infrastructure Configuration

Telemetry & Storage

Covers Metrics telemetry, Web UI management interface, Database storage providers, Network Scanner parameters, and Logging severity levels.

### 4.1 Metrics Configuration ("Metrics")

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| Enabled | Boolean | true | Enables HTTP metrics scraping endpoint. |
| StorageEngine | String | "prometheus" | Metrics format and storage engine exporter (e.g. Prometheus, InfluxDB). |
| Location | String (Path) | "/metrics" | URL path endpoint where metrics are exposed. |
| ListenAddress | String (IPv4) | "127.0.0.1" | IP address binding for the telemetry server. |
| ListenPort | Integer (1-65535) | 1080 | HTTP port exposed for metric scrapers. |

### 4.2 Web UI Configuration ("WebUI")

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| Enabled | Boolean | true | Enables or disables the administrative web UI application. |
| ListenAddress | String (IPv4) | "0.0.0.0" | Binding IP address for the Web UI HTTP web server. |
| ListenPort | Integer (1-65535) | 1081 | Port on which the Web UI portal is accessible. |

### 4.3 Database Options ("DbOptions")

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| DatabaseProvider | String | "sqlite" | Database driver engine (e.g. sqlite, postgres). |
| ConnectionString | String | "Data Source=.../astrolabed.db;Cache=Shared" | Database connection string specifying datasource location and connection parameters. |

### 4.4 Network Scanner & Logging ("NetworkScanner", "Logging")

| Property Name | Data Format | Example Value | Description |
| --- | --- | --- | --- |
| NetworkScanner.MaxDegreeOfParallelism | Integer | 100 | Maximum concurrent threads/tasks utilized for active subnet host discovery. |
| NetworkScanner.PingTimeoutMs | Integer (ms) | 200 | ICMP Ping packet response timeout in milliseconds per host probe. |
| Logging.Level | String (Enum) | "Trace" | Minimum log severity level. Valid levels: Trace, Debug, Information, Warning, Error, Critical. |

