<script setup lang="ts">
import { ref, reactive, computed } from 'vue'

export interface AppConfig {
    Dns: {
        Listen: { Address: string; Port: number }
        UpstreamTimeoutMs: number
        DefaultResolvers: Array<{ Name: string; Address: string; Port: number }>
        Resolvers: Array<{
            Name: string
            Address?: string
            Port?: number
            Rule?: string
            Block?: boolean
        }>
        Blocklists: string[]
        Allowlists: string[]
        HostsFiles: string[]
        Caching: {
            Enabled: boolean
            TtlSeconds: number
            MaxEntries: number
            CleanupIntervalMinutes: number
        }
        BlockResponse: { Mode: string; StaticIp: string; Ttl: number }
        ConditionalForwarding: {
            Enabled: boolean
            DhcpServerIp: string
            DhcpServerPort: number
            LocalDomain: string
            LocalSubnetCidr: string
            ForwardNonFqdn: boolean
        }
    }
    Dhcp: {
        Enabled: boolean
        ListenAddress: string
        ListenPort: number
        LeaseStorePath: string
        BadIpStorePath: string
        PoolCidr: string
        ServerIdentifier: string
        Router: string
        DnsServer: string
        NtpServer: string
        DomainName: string
        InterfaceMtu: number
        TftpServerName: string
        BootfileName: string
        WebProxyServerUrl: string
        LeaseHours: number
        ArpTimeoutMs: number
    }
    Ntp: {
        Enabled: boolean
        ListenAddress: string
        Port: number
        BufferSize: number
        Stratum: number
        ReferenceId: string
        Upstream: {
            Enabled: boolean
            Servers: string[]
            PollIntervalSeconds: number
        }
    }
    Metrics: {
        Enabled: boolean
        StorageEngine: string
        Location: string
        ListenAddress: string
        ListenPort: number
    }
    WebUI: {
        Enabled: boolean
        ListenAddress: string
        ListenPort: number
    }
    DbOptions: {
        DatabaseProvider: string
        ConnectionString: string
    }
    NetworkScanner: {
        MaxDegreeOfParallelism: number
        PingTimeoutMs: number
    }
    Logging: {
        Level: string
    }
}

const initialConfig: AppConfig = {
    Dns: {
        Listen: { Address: '127.0.0.1', Port: 1053 },
        UpstreamTimeoutMs: 1500,
        DefaultResolvers: [
            { Name: 'Cloudflare', Address: '1.1.1.1', Port: 53 },
            { Name: 'Cloudflare Secondary', Address: '1.0.0.1', Port: 53 },
        ],
        Resolvers: [
            {
                Name: 'LocalDNS',
                Address: '127.0.0.1',
                Port: 5353,
                Rule: '^localdev\\.',
                Block: false,
            },
            { Name: 'BlockDevAds', Rule: '^(ads|tracking)\\.', Block: true },
        ],
        Blocklists: [],
        Allowlists: [],
        HostsFiles: ['file://../../../netdns-runtime/dns-hosts/custom.list'],
        Caching: { Enabled: true, TtlSeconds: 300, MaxEntries: 2000, CleanupIntervalMinutes: 15 },
        BlockResponse: { Mode: 'NXDOMAIN', StaticIp: '0.0.0.0', Ttl: 60 },
        ConditionalForwarding: {
            Enabled: true,
            DhcpServerIp: '192.168.1.1',
            DhcpServerPort: 53,
            LocalDomain: 'lan',
            LocalSubnetCidr: '192.168.1.0/24',
            ForwardNonFqdn: true,
        },
    },
    Dhcp: {
        Enabled: true,
        ListenAddress: '0.0.0.0',
        ListenPort: 1067,
        LeaseStorePath: '../../../netdns-runtime/leases.json',
        BadIpStorePath: '../../../netdns-runtime/badips.json',
        PoolCidr: '192.168.10.0/24',
        ServerIdentifier: '192.168.10.1',
        Router: '192.168.10.1',
        DnsServer: '1.1.1.1',
        NtpServer: '192.168.10.1',
        DomainName: 'corp.internal',
        InterfaceMtu: 1500,
        TftpServerName: '192.168.10.5',
        BootfileName: 'pxelinux.0',
        WebProxyServerUrl: 'http://wpad.corp.internal/wpad.dat',
        LeaseHours: 24,
        ArpTimeoutMs: 500,
    },
    Ntp: {
        Enabled: true,
        ListenAddress: '127.0.0.1',
        Port: 1123,
        BufferSize: 65536,
        Stratum: 1,
        ReferenceId: 'LOCL',
        Upstream: {
            Enabled: true,
            Servers: ['0.pool.ntp.org', '1.pool.ntp.org'],
            PollIntervalSeconds: 16,
        },
    },
    Metrics: {
        Enabled: true,
        StorageEngine: 'prometheus',
        Location: '/metrics',
        ListenAddress: '127.0.0.1',
        ListenPort: 1080,
    },
    WebUI: {
        Enabled: true,
        ListenAddress: '0.0.0.0',
        ListenPort: 1081,
    },
    DbOptions: {
        DatabaseProvider: 'sqlite',
        ConnectionString: 'Data Source=../../../netdns-runtime/astrolabed.db;Cache=Shared',
    },
    NetworkScanner: {
        MaxDegreeOfParallelism: 100,
        PingTimeoutMs: 200,
    },
    Logging: {
        Level: 'Trace',
    },
}

const config = reactive<AppConfig>(JSON.parse(JSON.stringify(initialConfig)))
const activeTab = ref<'dns' | 'dhcp' | 'ntp' | 'ops' | 'preview'>('dns')
const isSaved = ref(false)

const formattedJson = computed(() => JSON.stringify(config, null, 2))

function resetConfig() {
    Object.assign(config, JSON.parse(JSON.stringify(initialConfig)))
    isSaved.value = false
}

function saveConfig() {
    isSaved.value = true
    setTimeout(() => {
        isSaved.value = false
    }, 3000)
}
</script>

<template>
    <div class="config-editor">
        <header class="editor-header">
            <h2>NetDNS Configuration Control Panel</h2>
            <div class="actions">
                <button class="btn btn-secondary" @click="resetConfig">Reset Defaults</button>
                <button class="btn btn-primary" @click="saveConfig">Save Configuration</button>
            </div>
        </header>

        <div v-if="isSaved" class="alert-success">Configuration saved successfully!</div>

        <nav class="nav-tabs">
            <button :class="{ active: activeTab === 'dns' }" @click="activeTab = 'dns'">
                DNS Settings
            </button>
            <button :class="{ active: activeTab === 'dhcp' }" @click="activeTab = 'dhcp'">
                DHCP Settings
            </button>
            <button :class="{ active: activeTab === 'ntp' }" @click="activeTab = 'ntp'">
                NTP Settings
            </button>
            <button :class="{ active: activeTab === 'ops' }" @click="activeTab = 'ops'">
                Operations & Infra
            </button>
            <button :class="{ active: activeTab === 'preview' }" @click="activeTab = 'preview'">
                JSON Preview
            </button>
        </nav>
        <main class="tab-content">
            <DnsConfigForm v-if="activeTab === 'dns'" :dns="config.Dns" />
            <DhcpConfigForm v-if="activeTab === 'dhcp'" :dhcp="config.Dhcp" />
            <NtpConfigForm v-if="activeTab === 'ntp'" :ntp="config.Ntp" />
            <OperationsConfigForm
                v-if="activeTab === 'ops'"
                :metrics="config.Metrics"
                :web-ui="config.WebUI"
                :db="config.DbOptions"
                :scanner="config.NetworkScanner"
                :logging="config.Logging"
            />
            <div v-if="activeTab === 'preview'" class="json-preview">
                <pre><code>{{ formattedJson }}</code></pre>
            </div>
        </main>
    </div>
</template>

<style scoped>
.config-editor {
    max-width: 1200px;
    margin: 0 auto;
    padding: 24px;
    font-family:
        system-ui,
        -apple-system,
        sans-serif;
    color: #1e293b;
}

.editor-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 2px solid #e2e8f0;
    padding-bottom: 16px;
    margin-bottom: 20px;
}

.editor-header h2 {
    margin: 0;
    font-size: 1.5rem;
    color: #0f172a;
}

.actions {
    display: flex;
    gap: 12px;
}

.btn {
    padding: 8px 16px;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
    border: 1px solid transparent;
}

.btn-primary {
    background-color: #2563eb;
    color: white;
}

.btn-secondary {
    background-color: #f1f5f9;
    color: #475569;
    border-color: #cbd5e1;
}

.alert-success {
    background-color: #dcfce7;
    color: #166534;
    padding: 12px;
    border-radius: 6px;
    margin-bottom: 16px;
    border: 1px solid #bbf7d0;
}

.nav-tabs {
    display: flex;
    gap: 8px;
    border-bottom: 1px solid #cbd5e1;
    margin-bottom: 20px;
}

.nav-tabs button {
    padding: 10px 18px;
    border: none;
    background: none;
    font-weight: 600;
    color: #64748b;
    cursor: pointer;
    border-bottom: 2px solid transparent;
}

.nav-tabs button.active {
    color: #2563eb;
    border-bottom-color: #2563eb;
}

.tab-content {
    background: #ffffff;
    padding: 20px;
    border-radius: 8px;
    border: 1px solid #e2e8f0;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.json-preview pre {
    background: #0f172a;
    color: #38bdf8;
    padding: 16px;
    border-radius: 6px;
    overflow-x: auto;
}
</style>
