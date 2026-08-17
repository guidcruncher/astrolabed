<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
    useAstrolabedApi,
    type DiscoveredLanDevice,
    type DnsResponseEvent,
} from '../composables/useAstrolabedApi'
import { useAuth } from '../composables/useAuth'
import type { TabItem, PagedResult } from '../components/types'

const { loading, error, getDnsEvents, getDiscoveredNetworkDevices, getLeases, clearDnsCache } =
    useAstrolabedApi()

const { logout } = useAuth()

const activeTab = ref('dns-lookup')

const recentEvents = ref<PagedResult<DnsResponseEvent>>({
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
})

const lanDevices = ref<DiscoveredLanDevice[]>([])
const activeLeasesCount = ref<number>(0)

const dashboardTabs: TabItem[] = [
    { id: 'dns-lookup', label: 'DNS Lookup' },
    { id: 'lan-devices', label: 'LAN Devices' },
    { id: 'dns-logs', label: 'DNS Activity' },
]

const handleLogout = async (): Promise<void> => {
    await logout()
}

const refreshDashboardData = async (): Promise<void> => {
    try {
        const [eventsData, devicesData, leasesData] = await Promise.allSettled([
            getDnsEvents({
                pageNumber: recentEvents.value.pageNumber,
                pageSize: recentEvents.value.pageSize,
            }),
            getDiscoveredNetworkDevices(),
            getLeases(true),
        ])

        if (eventsData.status === 'fulfilled') {
            recentEvents.value = eventsData.value
        }

        if (devicesData.status === 'fulfilled') {
            lanDevices.value = devicesData.value ?? []
        }

        if (leasesData.status === 'fulfilled' && Array.isArray(leasesData.value)) {
            activeLeasesCount.value = leasesData.value.length
        }
    } catch (err) {
        console.error('Failed to load dashboard metrics', err)
    }
}

const handleDnsPageChange = async (page: number): Promise<void> => {
    recentEvents.value.pageNumber = page
    const eventsData = await getDnsEvents({
        pageNumber: page,
        pageSize: recentEvents.value.pageSize,
    })
    recentEvents.value = eventsData
}

const handleDnsPageSizeChange = async (size: number): Promise<void> => {
    const eventsData = await getDnsEvents({ pageNumber: 1, pageSize: size })
    recentEvents.value = {
        ...eventsData,
        pageNumber: 1,
        pageSize: size,
    }
}

const handleFlushCache = async (): Promise<void> => {
    if (confirm('Are you sure you want to flush the system DNS cache?')) {
        await clearDnsCache()
        alert('DNS Cache successfully flushed.')
    }
}

onMounted(() => {
    refreshDashboardData()
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
            <div class="wt-body wt-stat-value">{{ recentEvents.totalCount }}</div>
        </div>
        <div class="wt-dialog wt-stat-box">
            <div class="wt-title">DHCP Leases</div>
            <div class="wt-body wt-stat-value">{{ activeLeasesCount }}</div>
        </div>
        <div class="wt-dialog wt-stat-box">
            <div class="wt-title">LAN Devices</div>
            <div class="wt-body wt-stat-value">{{ lanDevices.length }}</div>
        </div>
    </div>

    <!-- Modularized Tab Navigation -->
    <WhiptailTabs v-model="activeTab" :tabs="dashboardTabs" class="wt-full-width">
        <template #dns-lookup>
            <DnsLookupTab :lan-devices="lanDevices" />
        </template>

        <template #lan-devices>
            <LanDevicesTab :devices="lanDevices" :loading="loading" />
        </template>

        <template #dns-logs>
            <DnsLogsTab
                :events="recentEvents"
                :loading="loading"
                @refresh="refreshDashboardData"
                @page-change="handleDnsPageChange"
                @page-size-change="handleDnsPageSizeChange"
            />
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
