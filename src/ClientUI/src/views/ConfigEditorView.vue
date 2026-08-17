<script setup lang="ts">
import { ref } from 'vue'

import type { TabItem } from '../components/types'

const initialConfig = {
    Dns: {
        Listen: {
            Address: '127.0.0.1',
            Port: 1053,
        },
        UpstreamTimeoutMs: 1500,
        DefaultResolvers: [
            {
                Name: 'Cloudflare',
                Address: '1.1.1.1',
                Port: 53,
            },
            {
                Name: 'Cloudflare Secondary',
                Address: '1.0.0.1',
                Port: 53,
            },
        ],
        Resolvers: [
            {
                Name: 'LocalDNS',
                Address: '127.0.0.1',
                Port: 5353,
                Rule: '^localdev\\.',
                Block: false,
            },
            {
                Name: 'BlockDevAds',
                Rule: '^(ads|tracking)\\.',
                Block: true,
            },
        ],
        Blocklists: [],
        Allowlists: [],
        HostsFiles: ['file://../../../netdns-runtime/dns-hosts/custom.list'],
        Caching: {
            Enabled: true,
            TtlSeconds: 300,
            MaxEntries: 2000,
            CleanupIntervalMinutes: 15,
        },
        BlockResponse: {
            Mode: 'NXDOMAIN',
            StaticIp: '0.0.0.0',
            Ttl: 60,
        },
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

const emit = defineEmits<{
    (e: 'save', updatedConfig: Record<string, any>): void
    (e: 'cancel'): void
}>()

// Clone configuration to prevent mutating upstream props directly
const configData = ref<Record<string, any>>(JSON.parse(JSON.stringify(initialConfig)))
const activeTab = ref<string>('dns')

const tabs: TabItem[] = [
    { id: 'dns', label: 'DNS Services' },
    { id: 'dhcp', label: 'DHCP Config' },
    { id: 'ntp', label: 'NTP Time' },
    { id: 'metrics', label: 'Metrics' },
    { id: 'webui', label: 'Web UI' },
    { id: 'db', label: 'Database' },
    { id: 'scanner', label: 'NetScanner' },
    { id: 'logging', label: 'Logging' },
]

const handleSave = (): void => {
    emit('save', configData.value)
}

const handleCancel = (): void => {
    emit('cancel')
}
</script>

<template>
    <div class="wt-screen">
        <div class="wt-dialog wt-config-editor-dialog">
            <!-- Terminal Dialog Header -->
            <div class="wt-title wt-header-title">
                <span>[ CONFIGURATION OPTIONS EDITOR ]</span>
                <span class="wt-status-indicator">• READY</span>
            </div>

            <!-- Tabbed Configuration Sections -->
            <WhiptailTabs v-model="activeTab" :tabs="tabs">
                <template #dns>
                    <DnsConfigForm v-model="configData.Dns" />
                </template>

                <template #dhcp>
                    <DhcpConfigForm v-model="configData.Dhcp" />
                </template>

                <template #ntp>
                    <NtpConfigForm v-model="configData.Ntp" />
                </template>

                <template #metrics>
                    <MetricsConfigForm v-model="configData.Metrics" />
                </template>

                <template #webui>
                    <WebUiConfigForm v-model="configData.WebUI" />
                </template>

                <template #db>
                    <DbOptionsConfigForm v-model="configData.DbOptions" />
                </template>

                <template #scanner>
                    <NetworkScannerConfigForm v-model="configData.NetworkScanner" />
                </template>

                <template #logging>
                    <LoggingConfigForm v-model="configData.Logging" />
                </template>
            </WhiptailTabs>

            <!-- Dialog Footer Actions -->
            <div class="wt-footer">
                <WhiptailButton variant="cancel" @click="handleCancel">Cancel</WhiptailButton>
                <WhiptailButton variant="ok" @click="handleSave">Save Config</WhiptailButton>
            </div>
        </div>
    </div>
</template>

<style scoped>
.wt-config-editor-dialog {
    max-width: 900px;
    width: 100%;
}
</style>
