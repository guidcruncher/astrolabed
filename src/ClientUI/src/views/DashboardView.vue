<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAstrolabedApi } from '../composables/useAstrolabedApi'
import { useAuth } from '../composables/useAuth'
import type { TabItem } from '../components/types'

const { loading, error, getDnsEvents, getDiscoveredNetworkDevices, getLeases, clearDnsCache } =
    useAstrolabedApi()

const { logout } = useAuth()

const activeTab = ref('dns-lookup')

// Local state retained strictly for header metric summary cards
const dnsEventsCount = ref<number>(0)
const lanDevicesCount = ref<number>(0)
const activeLeasesCount = ref<number>(0)

const dashboardTabs: TabItem[] = [
    { id: 'dns-lookup', label: 'DNS Lookup' },
    { id: 'lan-devices', label: 'LAN Devices' },
    { id: 'dhcp', label: 'DHCP' },
    { id: 'dns-logs', label: 'DNS Activity' },
    { id: 'ntp', label: 'Time' },
]

const handleLogout = async (): Promise<void> => {
    await logout()
}

const refreshDashboardMetrics = async (): Promise<void> => {
    try {
        const [logsData, devicesData, leasesData] = await Promise.allSettled([
            getDnsEvents(),
            getDiscoveredNetworkDevices(),
            getLeases(true),
        ])

        if (logsData.status === 'fulfilled' && Array.isArray(logsData.value)) {
            dnsEventsCount.value = logsData.value.length
        }

        if (devicesData.status === 'fulfilled' && Array.isArray(devicesData.value)) {
            lanDevicesCount.value = devicesData.value.length
        }

        if (leasesData.status === 'fulfilled' && Array.isArray(leasesData.value)) {
            activeLeasesCount.value = leasesData.value.length
        }
    } catch (err) {
        console.error('Failed to load dashboard metrics', err)
    }
}

const handleFlushCache = async (): Promise<void> => {
    if (confirm('Are you sure you want to flush the system DNS cache?')) {
        await clearDnsCache()
        alert('DNS Cache successfully flushed.')
    }
}

onMounted(() => {
    refreshDashboardMetrics()
})
</script>

<template>
    <!-- Header Dialog -->
    <header class="wt-dialog wt-full-width wt-mt">
        <div class="wt-title wt-header-title">
            <span>[ Astrolabed Control Center ]</span>
            <span class="wt-status-indicator" :class="{ 'wt-status-busy': loading }">
                &lt; {{ loading ? 'PROCESSING' : 'ONLINE' }} &gt;
            </span>
        </div>
        <div class="wt-body wt-header-body">
            <div style="float: left">
                <span>System Diagnostics & Status Overview</span>
            </div>
            <div style="float: right">
                <WhiptailButton class="wt-btn-cancel" @click="handleFlushCache">
                    Flush Cache </WhiptailButton
                >&nbsp;
                <WhiptailButton class="wt-btn-cancel" @click="handleLogout">
                    Logout
                </WhiptailButton>
            </div>
        </div>
    </header>

    <!-- Error Banner -->
    <div v-if="error" class="wt-dialog wt-alert-error wt-full-width">
        <div class="wt-title wt-title-error">SYSTEM ERROR</div>
        <div class="wt-body">
            {{
                typeof error === 'string'
                    ? error
                    : error.detail || error.title || 'An API error occurred'
            }}
        </div>
    </div>

    <!-- Stat Cards Grid -->
    <div class="wt-stats-grid">
        <div class="wt-dialog wt-stat-box">
            <div class="wt-title">DNS Events</div>
            <div class="wt-body wt-stat-value">{{ dnsEventsCount }}</div>
        </div>
        <div class="wt-dialog wt-stat-box">
            <div class="wt-title">DHCP Leases</div>
            <div class="wt-body wt-stat-value">{{ activeLeasesCount }}</div>
        </div>
        <div class="wt-dialog wt-stat-box">
            <div class="wt-title">LAN Devices</div>
            <div class="wt-body wt-stat-value">{{ lanDevicesCount }}</div>
        </div>
    </div>

    <!-- Modularized Tab Navigation -->
    <WhiptailTabs v-model="activeTab" :tabs="dashboardTabs" class="wt-full-width">
        <template #dns-lookup>
            <DnsLookupTab />
        </template>

        <template #lan-devices>
            <LanDevicesTab />
        </template>

        <template #dns-logs>
            <DnsLogsTab />
        </template>

        <template #dhcp>
            <DhcpLeaseTab />
        </template>

        <template #ntp>
            <NtpLookupTab />
        </template>
    </WhiptailTabs>
</template>

<style scoped>
.wt-full-width {
    width: 100%;
    max-width: 100%;
    margin-left: 0;
    margin-right: 0;
    box-sizing: border-box;
}

.wt-stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 20px;
    margin-bottom: 20px;
    width: 100%;
}

.wt-stat-box {
    margin: 0;
}

.wt-stat-value {
    font-size: 2rem;
    color: #fff;
    font-weight: bold;
    text-align: center;
}
</style>
