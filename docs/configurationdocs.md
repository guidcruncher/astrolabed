# Configuration Reference

Complete documentation for DNS, DHCP, NTP, Metrics, WebUI, and Logging configuration settings.

## 1\. DNS Service Configuration (`Dns`)

Configures the core DNS resolver engine, upstreams, rule-based routing, local hosts overrides, caching, and conditional forwarding.

| Option | Type | Default / Sample | Description |
| --- | --- | --- | --- |
| Listen.Address | string | "127.0.0.1" | IP address for the DNS service to bind to and listen on. |
| Listen.Port | integer | 1053 | Network port used to listen for incoming DNS queries. |
| UpstreamTimeoutMs | integer | 1500 | Maximum time in milliseconds to wait for a response from upstream resolvers before timing out. |
| DefaultResolvers Array (Fallback / Upstream Servers) |
| DefaultResolvers[].Name | string | "Cloudflare" | Human-readable label for identifying the fallback resolver. |
| DefaultResolvers[].Address | string | "1.1.1.1" | IP address of the fallback upstream DNS server. |
| DefaultResolvers[].Port | integer | 53 | Target port on the upstream DNS server. |
| Resolvers Array (Rule-Based Forwarding & Filtering) |
| Resolvers[].Name | string | "LocalDNS" | Friendly name for a specific conditional resolver rule. |
| Resolvers[].Address | string | "127.0.0.1" | Target IP address to send matched requests to (Optional if blocking). |
| Resolvers[].Port | integer | 5353 | Target port for matched requests (Optional if blocking). |
| Resolvers[].Rule | string | "^localdev\." | Regex pattern matched against requested hostnames. |
| Resolvers[].Block | boolean | false / true | If true, immediately blocks queries matching the Rule pattern. |
| Lists & Override Files |
| Blocklists | array[string] | [] | List of URI paths/URLs containing domain blocklists (ad blocking/malware lists). |
| Allowlists | array[string] | [] | List of URI paths/URLs containing explicitly allowed domains. |
| HostsFiles | array[string] | ["file://..."] | URI paths to local custom hosts files for manual DNS mappings. |
| Caching Options |
| Caching.Enabled | boolean | true | Enables or disables in-memory DNS record caching. |
| Caching.TtlSeconds | integer | 300 | Time-To-Live duration in seconds for cached DNS query results. |
| Caching.MaxEntries | integer | 2000 | Maximum number of cached DNS entries retained in memory. |
| Caching.CleanupIntervalMinutes | integer | 15 | Frequency in minutes to purge expired records from cache. |
| BlockResponse Options |
| BlockResponse.Mode | string | "NXDOMAIN" | Response strategy when a domain is blocked (e.g., NXDOMAIN or NullIp). |
| BlockResponse.StaticIp | string | "0.0.0.0" | Static IP returned when Mode is configured to respond with a custom IP. |
| BlockResponse.Ttl | integer | 60 | TTL in seconds for blocked DNS responses. |
| Conditional Forwarding |
| ConditionalForwarding.Enabled | boolean | true | Enables forwarding local domain and reverse DNS queries to a local DHCP/router device. |
| ConditionalForwarding.DhcpServerIp | string | "192.168.1.1" | Target IP address of the DHCP server/router holding local client records. |
| ConditionalForwarding.DhcpServerPort | integer | 53 | Port used by the target local DHCP/router DNS service. |
| ConditionalForwarding.LocalDomain | string | "lan" | Local suffix appended to local hostname resolution. |
| ConditionalForwarding.LocalSubnetCidr | string | "192.168.1.0/24" | Subnet range whose reverse PTR lookups should forward to the DHCP server. |
| ConditionalForwarding.ForwardNonFqdn | boolean | true | If true, single-label names (e.g. mycomputer) are sent directly to the local DHCP server. |

## 2\. DHCP Service Configuration (`Dhcp`)

Configures network IP allocation, lease storage paths, netboot (PXE), and optional parameters provided to network clients.

| Option | Type | Default / Sample | Description |
| --- | --- | --- | --- |
| Enabled | boolean | false | Toggles whether the integrated DHCP server process is running. |
| ListenAddress | string | "0.0.0.0" | Interface IP to listen for DHCPDISCOVER requests on. |
| ListenPort | integer | 1067 | Binding port for incoming DHCP network requests. |
| LeaseStorePath | string | "../../../netdns-runtime/leases.json" | Local filesystem location used to store persistent DHCP lease records. |
| BadIpStorePath | string | "../../../netdns-runtime/badips.json" | Path to track IP conflict records or blacklisted client IPs. |
| PoolCidr | string | "192.168.10.0/24" | Network IP range managed by the DHCP pool for issuing dynamic leases. |
| ServerIdentifier | string | "192.168.10.1" | The primary IP identifier for this DHCP server sent to clients. |
| Router | string | "192.168.10.1" | Default gateway option (Option 3) assigned to clients. |
| DnsServer | string | "1.1.1.1" | Primary DNS server option (Option 6) assigned to connected devices. |
| NtpServer | string | "192.168.10.1" | NTP server option (Option 42) advertised to clients. |
| DomainName | string | "corp.internal" | Domain suffix option (Option 15) handed to DHCP clients. |
| InterfaceMtu | integer | 1500 | Network Interface MTU option (Option 26) provided to clients. |
| TftpServerName | string | "192.168.10.5" | TFTP server address (Option 66) used for PXE boot configurations. |
| BootfileName | string | "pxelinux.0" | Boot image path (Option 67) assigned to PXE client requests. |
| WebProxyServerUrl | string | "http://wpad..." | Web Proxy Auto-Discovery (WPAD / Option 252) file URL. |
| LeaseHours | integer | 24 | Validity duration in hours for dynamically assigned IP leases. |
| ArpTimeoutMs | integer | 500 | Timeout in milliseconds for ARP probes to detect active IP conflicts before assigning a lease. |

## 3\. Network Time Protocol Service Configuration (`Ntp`)

Controls internal time synchronization service settings and upstream NTP server syncing.

| Option | Type | Default / Sample | Description |
| --- | --- | --- | --- |
| Enabled | boolean | true | Enables or disables the local NTP server daemon. |
| ListenAddress | string | "127.0.0.1" | Interface address where the NTP daemon listens. |
| Port | integer | 1123 | Target port binding for incoming NTP UDP requests. |
| BufferSize | integer | 65536 | Buffer capacity in bytes allocated for network socket packets. |
| Stratum | integer | 1 | Advertised NTP stratum level (1 represents a primary hardware reference clock). |
| ReferenceId | string | "LOCL" | 4-character ASCII identifier code describing the reference clock source. |
| Upstream Settings |
| Upstream.Enabled | boolean | true | If true, synchronizes the system clock with public upstream time servers. |
| Upstream.Servers | array[string] | ["0.pool...", ...] | List of external upstream NTP server domain names or IP addresses. |
| Upstream.PollIntervalSeconds | integer | 16 | Polling frequency in seconds for updating time against upstream servers. |

## 4\. Monitoring, Admin UI & System Logging

Configures telemetry collection, system dashboard options, and logging verbosity.

| Option | Type | Default / Sample | Description |
| --- | --- | --- | --- |
| Metrics Configuration (Metrics) |
| Metrics.Enabled | boolean | true | Enables telemetry metrics collection endpoints. |
| Metrics.StorageEngine | string | "prometheus" | Format engine type used for metrics export (e.g. Prometheus). |
| Metrics.Location | string | "/metrics" | HTTP path endpoint exposed for scraping performance metrics. |
| Metrics.ListenAddress | string | "127.0.0.1" | IP address for listening to metrics scraper connections. |
| Metrics.ListenPort | integer | 1080 | Port used to expose the metrics endpoint. |
| Web UI Dashboard (WebUI) |
| WebUI.Enabled | boolean | true | Enables the built-in web management portal. |
| WebUI.ListenAddress | string | "127.0.0.1" | Bound interface IP address for administrative HTTP traffic. |
| WebUI.ListenPort | integer | 1081 | HTTP port allocated for accessing the web control panel. |
| System Logging (Logging) |
| Logging.Level | string | "Trace" | Log verbosity filter (e.g. Trace, Debug, Info, Warning, Error). |
