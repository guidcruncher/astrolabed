n
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAstrolabedApi } from '../composables/useAstrolabedApi'
import { useAuth } from '../composables/useAuth'
import type { DropdownOption, TabItem } from '../components/types'
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const {
    loading,
    error,
    getDnsEvents,
    getDnsCacheResponses,
    getDiscoveredNetworkDevices,
    getLeases,
    clearDnsCache,
} = useAstrolabedApi()

const { logout } = useAuth()

const dropdownOptions: DropdownOption[] = [
    { label: 'Configuration', value: 'config' },
    { label: 'Flush DNS Cache', value: 'flush-cache' },
    { label: 'Logout', value: 'logout' },
]

const activeTab = ref('dns-lookup')

// Local state retained strictly for header metric summary cards
const dnsEventsCount = ref<number>(0)
const dnsCacheCount = ref<number>(0)
const dnsCachePercent = ref<string>('')
const lanDevicesCount = ref<number>(0)
const activeLeasesCount = ref<number>(0)

const dashboardTabs: TabItem[] = [
    { id: 'dns-lookup', label: 'DNS Lookup' },
    { id: 'lan-devices', label: 'LAN Devices' },
    { id: 'dhcp', label: 'DHCP' },
    { id: 'dns-logs', label: 'DNS Activity' },
    { id: 'dns-cache', label: 'DNS Cache' },
    { id: 'ntp', label: 'Time' },
]

const handleLogout = async (): Promise<void> => {
    await logout()
}

const handleSelect = async (option: DropdownOption): Promise<void> => {
    switch (option.value) {
        case 'config':
            router.push('/config')
            break
        case 'flush-cache':
            await handleFlushCache()
            break
        case 'logout':
            await handleLogout()
            break
    }
}

const refreshDashboardMetrics = async (): Promise<void> => {
    try {
        const [logsData, cacheData, devicesData, leasesData] = await Promise.allSettled([
            getDnsEvents({ pageNumber: 1, pageSize: 5 }),
            getDnsCacheResponses({ pageNumber: 1, pageSize: 5 }),
            getDiscoveredNetworkDevices({ pageNumber: 1, pageSize: 5 }),
            getLeases(true, { pageNumber: 1, pageSize: 5 }),
        ])

        dnsCachePercent.value = ''

        if (logsData.status === 'fulfilled') {
            dnsEventsCount.value = logsData.value.totalCount
        }

        if (cacheData.status === 'fulfilled') {
            dnsCacheCount.value = cacheData.value.totalCount
            if (dnsCacheCount.value > 0 && dnsEventsCount.value > 0) {
                dnsCachePercent.value = `{(dnsCacheCount.value / dnsEventsCount.value).toFixed(2)}%`
            }
        }

        if (devicesData.status === 'fulfilled') {
            lanDevicesCount.value = devicesData.value.totalCount
        }

        if (leasesData.status === 'fulfilled') {
            activeLeasesCount.value = leasesData.value.totalCount
        }
    } catch (err) {
        console.error('Failed to load dashboard metrics', err)
    }
}

const handleSelectTab = (id: string) => {
    switch (id) {
        case 'lan-devices':
            break
        case 'dhcp':
            break
        case 'dns-logs':
            break
        case 'dns-cache':
            break
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
                <WhiptailDropdownMenu
                    button-label="Actions Menu"
                    :options="dropdownOptions"
                    @select="handleSelect"
                />
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
            <div class="wt-title">DNS</div>
            <div class="wt-body">
                <div class="wt-stat-value">Query {{ dnsEventsCount }}</div>
                <div class="wt-stat-value">Cached {{ dnsCacheCount }}</div>
            </div>
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
    <WhiptailTabs
        v-model="activeTab"
        @change="handleSelectTab"
        :tabs="dashboardTabs"
        class="wt-full-width"
    >
        <template #dns-lookup>
            <DnsLookupTab />
        </template>

        <template #lan-devices>
            <LanDevicesTab />
        </template>

        <template #dns-logs>
            <DnsLogsTab />
        </template>

        <template #dns-cache>
            <DnsCacheTab />
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
