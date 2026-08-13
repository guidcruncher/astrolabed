<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Network Services Configuration Documentation</title>
    <style>
        :root {
            --bg-color: #f8f9fa;
            --card-bg: #ffffff;
            --text-main: #212529;
            --text-muted: #6c757d;
            --border-color: #dee2e6;
            --primary: #2b6cb0;
            --accent: #319795;
            --code-bg: #edf2f7;
            --type-color: #805ad5;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            line-height: 1.6;
            background-color: var(--bg-color);
            color: var(--text-main);
            margin: 0;
            padding: 2rem;
        }

        .container {
            max-width: 1100px;
            margin: 0 auto;
        }

        header {
            margin-bottom: 2.5rem;
            border-bottom: 2px solid var(--border-color);
            padding-bottom: 1rem;
        }

        h1 {
            color: var(--primary);
            margin: 0 0 0.5rem 0;
        }

        .subtitle {
            color: var(--text-muted);
            font-size: 1.1rem;
        }

        .section-card {
            background-color: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 8px;
            padding: 1.5rem;
            margin-bottom: 2rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.05);
        }

        .section-card h2 {
            color: var(--primary);
            margin-top: 0;
            border-bottom: 1px solid var(--border-color);
            padding-bottom: 0.5rem;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 1rem;
        }

        th, td {
            text-align: left;
            padding: 0.75rem 1rem;
            border-bottom: 1px solid var(--border-color);
            vertical-align: top;
        }

        th {
            background-color: #f1f5f9;
            color: var(--text-main);
            font-weight: 600;
        }

        tr:last-child td {
            border-bottom: none;
        }

        .key-name {
            font-family: SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
            font-weight: 600;
            color: #2d3748;
        }

        .data-type {
            font-family: SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
            font-size: 0.85rem;
            color: var(--type-color);
            background-color: #f3e8ff;
            padding: 0.15rem 0.4rem;
            border-radius: 4px;
            display: inline-block;
        }

        .default-val {
            font-family: SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
            font-size: 0.85rem;
            color: #2c5282;
            background-color: var(--code-bg);
            padding: 0.15rem 0.4rem;
            border-radius: 4px;
            word-break: break-all;
            display: inline-block;
        }

        .nested-header {
            background-color: #edf2f7;
            font-weight: bold;
            color: var(--accent);
        }
    </style>
</head>
<body>

<div class="container">
    <header>
        <h1>Network Services Configuration Reference</h1>
        <div class="subtitle">Complete documentation for DNS, DHCP, NTP, Metrics, WebUI, and Logging configuration settings.</div>
    </header>

    <!-- DNS Section -->
    <div class="section-card">
        <h2>1. DNS Service Configuration (<code>Dns</code>)</h2>
        <p>Configures the core DNS resolver engine, upstreams, rule-based routing, local hosts overrides, caching, and conditional forwarding.</p>
        
        <table>
            <thead>
                <tr>
                    <th>Option</th>
                    <th>Type</th>
                    <th>Default / Sample</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td class="key-name">Listen.Address</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"127.0.0.1"</span></td>
                    <td>IP address for the DNS service to bind to and listen on.</td>
                </tr>
                <tr>
                    <td class="key-name">Listen.Port</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1053</span></td>
                    <td>Network port used to listen for incoming DNS queries.</td>
                </tr>
                <tr>
                    <td class="key-name">UpstreamTimeoutMs</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1500</span></td>
                    <td>Maximum time in milliseconds to wait for a response from upstream resolvers before timing out.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">DefaultResolvers Array (Fallback / Upstream Servers)</td></tr>
                <tr>
                    <td class="key-name">DefaultResolvers[].Name</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"Cloudflare"</span></td>
                    <td>Human-readable label for identifying the fallback resolver.</td>
                </tr>
                <tr>
                    <td class="key-name">DefaultResolvers[].Address</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"1.1.1.1"</span></td>
                    <td>IP address of the fallback upstream DNS server.</td>
                </tr>
                <tr>
                    <td class="key-name">DefaultResolvers[].Port</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">53</span></td>
                    <td>Target port on the upstream DNS server.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">Resolvers Array (Rule-Based Forwarding & Filtering)</td></tr>
                <tr>
                    <td class="key-name">Resolvers[].Name</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"LocalDNS"</span></td>
                    <td>Friendly name for a specific conditional resolver rule.</td>
                </tr>
                <tr>
                    <td class="key-name">Resolvers[].Address</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"127.0.0.1"</span></td>
                    <td>Target IP address to send matched requests to (Optional if blocking).</td>
                </tr>
                <tr>
                    <td class="key-name">Resolvers[].Port</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">5353</span></td>
                    <td>Target port for matched requests (Optional if blocking).</td>
                </tr>
                <tr>
                    <td class="key-name">Resolvers[].Rule</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"^localdev\."</span></td>
                    <td>Regex pattern matched against requested hostnames.</td>
                </tr>
                <tr>
                    <td class="key-name">Resolvers[].Block</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">false / true</span></td>
                    <td>If <code>true</code>, immediately blocks queries matching the <code>Rule</code> pattern.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">Lists & Override Files</td></tr>
                <tr>
                    <td class="key-name">Blocklists</td>
                    <td><span class="data-type">array[string]</span></td>
                    <td><span class="default-val">[]</span></td>
                    <td>List of URI paths/URLs containing domain blocklists (ad blocking/malware lists).</td>
                </tr>
                <tr>
                    <td class="key-name">Allowlists</td>
                    <td><span class="data-type">array[string]</span></td>
                    <td><span class="default-val">[]</span></td>
                    <td>List of URI paths/URLs containing explicitly allowed domains.</td>
                </tr>
                <tr>
                    <td class="key-name">HostsFiles</td>
                    <td><span class="data-type">array[string]</span></td>
                    <td><span class="default-val">["file://..."]</span></td>
                    <td>URI paths to local custom hosts files for manual DNS mappings.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">Caching Options</td></tr>
                <tr>
                    <td class="key-name">Caching.Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>Enables or disables in-memory DNS record caching.</td>
                </tr>
                <tr>
                    <td class="key-name">Caching.TtlSeconds</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">300</span></td>
                    <td>Time-To-Live duration in seconds for cached DNS query results.</td>
                </tr>
                <tr>
                    <td class="key-name">Caching.MaxEntries</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">2000</span></td>
                    <td>Maximum number of cached DNS entries retained in memory.</td>
                </tr>
                <tr>
                    <td class="key-name">Caching.CleanupIntervalMinutes</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">15</span></td>
                    <td>Frequency in minutes to purge expired records from cache.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">BlockResponse Options</td></tr>
                <tr>
                    <td class="key-name">BlockResponse.Mode</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"NXDOMAIN"</span></td>
                    <td>Response strategy when a domain is blocked (e.g., <code>NXDOMAIN</code> or <code>NullIp</code>).</td>
                </tr>
                <tr>
                    <td class="key-name">BlockResponse.StaticIp</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"0.0.0.0"</span></td>
                    <td>Static IP returned when <code>Mode</code> is configured to respond with a custom IP.</td>
                </tr>
                <tr>
                    <td class="key-name">BlockResponse.Ttl</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">60</span></td>
                    <td>TTL in seconds for blocked DNS responses.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">Conditional Forwarding</td></tr>
                <tr>
                    <td class="key-name">ConditionalForwarding.Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>Enables forwarding local domain and reverse DNS queries to a local DHCP/router device.</td>
                </tr>
                <tr>
                    <td class="key-name">ConditionalForwarding.DhcpServerIp</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.1.1"</span></td>
                    <td>Target IP address of the DHCP server/router holding local client records.</td>
                </tr>
                <tr>
                    <td class="key-name">ConditionalForwarding.DhcpServerPort</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">53</span></td>
                    <td>Port used by the target local DHCP/router DNS service.</td>
                </tr>
                <tr>
                    <td class="key-name">ConditionalForwarding.LocalDomain</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"lan"</span></td>
                    <td>Local suffix appended to local hostname resolution.</td>
                </tr>
                <tr>
                    <td class="key-name">ConditionalForwarding.LocalSubnetCidr</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.1.0/24"</span></td>
                    <td>Subnet range whose reverse PTR lookups should forward to the DHCP server.</td>
                </tr>
                <tr>
                    <td class="key-name">ConditionalForwarding.ForwardNonFqdn</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>If <code>true</code>, single-label names (e.g. <code>mycomputer</code>) are sent directly to the local DHCP server.</td>
                </tr>
            </tbody>
        </table>
    </div>

    <!-- DHCP Section -->
    <div class="section-card">
        <h2>2. DHCP Service Configuration (<code>Dhcp</code>)</h2>
        <p>Configures network IP allocation, lease storage paths, netboot (PXE), and optional parameters provided to network clients.</p>
        
        <table>
            <thead>
                <tr>
                    <th>Option</th>
                    <th>Type</th>
                    <th>Default / Sample</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td class="key-name">Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">false</span></td>
                    <td>Toggles whether the integrated DHCP server process is running.</td>
                </tr>
                <tr>
                    <td class="key-name">ListenAddress</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"0.0.0.0"</span></td>
                    <td>Interface IP to listen for DHCPDISCOVER requests on.</td>
                </tr>
                <tr>
                    <td class="key-name">ListenPort</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1067</span></td>
                    <td>Binding port for incoming DHCP network requests.</td>
                </tr>
                <tr>
                    <td class="key-name">LeaseStorePath</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"../../../netdns-runtime/leases.json"</span></td>
                    <td>Local filesystem location used to store persistent DHCP lease records.</td>
                </tr>
                <tr>
                    <td class="key-name">BadIpStorePath</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"../../../netdns-runtime/badips.json"</span></td>
                    <td>Path to track IP conflict records or blacklisted client IPs.</td>
                </tr>
                <tr>
                    <td class="key-name">PoolCidr</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.10.0/24"</span></td>
                    <td>Network IP range managed by the DHCP pool for issuing dynamic leases.</td>
                </tr>
                <tr>
                    <td class="key-name">ServerIdentifier</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.10.1"</span></td>
                    <td>The primary IP identifier for this DHCP server sent to clients.</td>
                </tr>
                <tr>
                    <td class="key-name">Router</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.10.1"</span></td>
                    <td>Default gateway option (Option 3) assigned to clients.</td>
                </tr>
                <tr>
                    <td class="key-name">DnsServer</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"1.1.1.1"</span></td>
                    <td>Primary DNS server option (Option 6) assigned to connected devices.</td>
                </tr>
                <tr>
                    <td class="key-name">NtpServer</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.10.1"</span></td>
                    <td>NTP server option (Option 42) advertised to clients.</td>
                </tr>
                <tr>
                    <td class="key-name">DomainName</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"corp.internal"</span></td>
                    <td>Domain suffix option (Option 15) handed to DHCP clients.</td>
                </tr>
                <tr>
                    <td class="key-name">InterfaceMtu</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1500</span></td>
                    <td>Network Interface MTU option (Option 26) provided to clients.</td>
                </tr>
                <tr>
                    <td class="key-name">TftpServerName</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"192.168.10.5"</span></td>
                    <td>TFTP server address (Option 66) used for PXE boot configurations.</td>
                </tr>
                <tr>
                    <td class="key-name">BootfileName</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"pxelinux.0"</span></td>
                    <td>Boot image path (Option 67) assigned to PXE client requests.</td>
                </tr>
                <tr>
                    <td class="key-name">WebProxyServerUrl</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"http://wpad..."</span></td>
                    <td>Web Proxy Auto-Discovery (WPAD / Option 252) file URL.</td>
                </tr>
                <tr>
                    <td class="key-name">LeaseHours</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">24</span></td>
                    <td>Validity duration in hours for dynamically assigned IP leases.</td>
                </tr>
                <tr>
                    <td class="key-name">ArpTimeoutMs</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">500</span></td>
                    <td>Timeout in milliseconds for ARP probes to detect active IP conflicts before assigning a lease.</td>
                </tr>
            </tbody>
        </table>
    </div>

    <!-- NTP Section -->
    <div class="section-card">
        <h2>3. Network Time Protocol Service Configuration (<code>Ntp</code>)</h2>
        <p>Controls internal time synchronization service settings and upstream NTP server syncing.</p>
        
        <table>
            <thead>
                <tr>
                    <th>Option</th>
                    <th>Type</th>
                    <th>Default / Sample</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td class="key-name">Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>Enables or disables the local NTP server daemon.</td>
                </tr>
                <tr>
                    <td class="key-name">ListenAddress</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"127.0.0.1"</span></td>
                    <td>Interface address where the NTP daemon listens.</td>
                </tr>
                <tr>
                    <td class="key-name">Port</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1123</span></td>
                    <td>Target port binding for incoming NTP UDP requests.</td>
                </tr>
                <tr>
                    <td class="key-name">BufferSize</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">65536</span></td>
                    <td>Buffer capacity in bytes allocated for network socket packets.</td>
                </tr>
                <tr>
                    <td class="key-name">Stratum</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1</span></td>
                    <td>Advertised NTP stratum level (1 represents a primary hardware reference clock).</td>
                </tr>
                <tr>
                    <td class="key-name">ReferenceId</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"LOCL"</span></td>
                    <td>4-character ASCII identifier code describing the reference clock source.</td>
                </tr>
                <tr class="nested-header"><td colspan="4">Upstream Settings</td></tr>
                <tr>
                    <td class="key-name">Upstream.Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>If true, synchronizes the system clock with public upstream time servers.</td>
                </tr>
                <tr>
                    <td class="key-name">Upstream.Servers</td>
                    <td><span class="data-type">array[string]</span></td>
                    <td><span class="default-val">["0.pool...", ...]</span></td>
                    <td>List of external upstream NTP server domain names or IP addresses.</td>
                </tr>
                <tr>
                    <td class="key-name">Upstream.PollIntervalSeconds</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">16</span></td>
                    <td>Polling frequency in seconds for updating time against upstream servers.</td>
                </tr>
            </tbody>
        </table>
    </div>

    <!-- Metrics, WebUI & Logging Sections -->
    <div class="section-card">
        <h2>4. Monitoring, Admin UI & System Logging</h2>
        <p>Configures telemetry collection, system dashboard options, and logging verbosity.</p>
        
        <table>
            <thead>
                <tr>
                    <th>Option</th>
                    <th>Type</th>
                    <th>Default / Sample</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
                <tr class="nested-header"><td colspan="4">Metrics Configuration (Metrics)</td></tr>
                <tr>
                    <td class="key-name">Metrics.Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>Enables telemetry metrics collection endpoints.</td>
                </tr>
                <tr>
                    <td class="key-name">Metrics.StorageEngine</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"prometheus"</span></td>
                    <td>Format engine type used for metrics export (e.g. Prometheus).</td>
                </tr>
                <tr>
                    <td class="key-name">Metrics.Location</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"/metrics"</span></td>
                    <td>HTTP path endpoint exposed for scraping performance metrics.</td>
                </tr>
                <tr>
                    <td class="key-name">Metrics.ListenAddress</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"127.0.0.1"</span></td>
                    <td>IP address for listening to metrics scraper connections.</td>
                </tr>
                <tr>
                    <td class="key-name">Metrics.ListenPort</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1080</span></td>
                    <td>Port used to expose the metrics endpoint.</td>
                </tr>

                <tr class="nested-header"><td colspan="4">Web UI Dashboard (WebUI)</td></tr>
                <tr>
                    <td class="key-name">WebUI.Enabled</td>
                    <td><span class="data-type">boolean</span></td>
                    <td><span class="default-val">true</span></td>
                    <td>Enables the built-in web management portal.</td>
                </tr>
                <tr>
                    <td class="key-name">WebUI.ListenAddress</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"127.0.0.1"</span></td>
                    <td>Bound interface IP address for administrative HTTP traffic.</td>
                </tr>
                <tr>
                    <td class="key-name">WebUI.ListenPort</td>
                    <td><span class="data-type">integer</span></td>
                    <td><span class="default-val">1081</span></td>
                    <td>HTTP port allocated for accessing the web control panel.</td>
                </tr>

                <tr class="nested-header"><td colspan="4">System Logging (Logging)</td></tr>
                <tr>
                    <td class="key-name">Logging.Level</td>
                    <td><span class="data-type">string</span></td>
                    <td><span class="default-val">"Trace"</span></td>
                    <td>Log verbosity filter (e.g. <code>Trace</code>, <code>Debug</code>, <code>Info</code>, <code>Warning</code>, <code>Error</code>).</td>
                </tr>
            </tbody>
        </table>
    </div>

</div>

</body>
</html>
