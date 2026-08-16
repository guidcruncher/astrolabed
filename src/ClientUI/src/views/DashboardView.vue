<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import {
    useAstrolabedApi,
    type DiscoveredLanDeviceDto,
    type DnsResponseEvent,
} from '../composables/useAstrolabedApi'
import { useAuth } from '../composables/useAuth'
import type { WhiptailOption, TabItem } from '../components/types'

// Initialize API Composable
const {
    loading,
    error,
    queryDns,
    getDnsEvents,
    getDiscoveredNetworkDevices,
    getLeases,
    clearDnsCache,
} = useAstrolabedApi()

const { logout } = useAuth()

// Reactive State
const activeTab = ref('dns-lookup')
const selectedDomain = ref('')
const selectedRecordType = ref('A')
const queryResult = ref<any>(null)
const queryDurationMs = ref<number | null>(null)
const recentEvents = ref<DnsResponseEvent[]>([])
const lanDevices = ref<DiscoveredLanDeviceDto[]>([])
const activeLeasesCount = ref<number>(0)
const totalEventsCount = ref<number>(0)

// Tab Definitions
const dashboardTabs: TabItem[] = [
    { id: 'dns-lookup', label: 'DNS Lookup' },
    { id: 'lan-devices', label: 'LAN Devices' },
    { id: 'dns-logs', label: 'DNS Activity' },
]

// Predefined option list for WhiptailCombobox
const commonDomains: WhiptailOption[] = [
    { label: 'google.com', value: 'google.com' },
    { label: 'cloudflare.com', value: 'cloudflare.com' },
    { label: 'github.com', value: 'github.com' },
    { label: 'microsoft.com', value: 'microsoft.com' },
    { label: 'astrolabed.local', value: 'astrolabed.local' },
]

// Convert LAN devices to Combobox options dynamically
const deviceComboboxOptions = computed<WhiptailOption[]>(() => {
    const defaults = [...commonDomains]
    const dynamic = lanDevices.value.map((device) => ({
        label: device.hostName ? `${device.hostName} (${device.ipAddress})` : device.ipAddress,
        value: device.ipAddress,
    }))
    return [...defaults, ...dynamic]
})

// Normalize raw API response into structured DNS records
const parsedAnswers = computed(() => {
    if (!queryResult.value) return []
    const res = queryResult.value

    if (Array.isArray(res.answers)) return res.answers
    if (Array.isArray(res.Answer)) return res.Answer
    if (Array.isArray(res)) return res
    if (typeof res === 'object' && res.data) {
        return Array.isArray(res.data) ? res.data : [res.data]
    }

    return []
})

const handleLogout = async (): Promise<void> => {
    await logout()
}

// Fetch initial dashboard metrics
const refreshDashboardData = async (): Promise<void> => {
    try {
        const [eventsData, devicesData, leasesData] = await Promise.allSettled([
            getDnsEvents({ pageNumber: 1, pageSize: 10 }),
            getDiscoveredNetworkDevices(),
            getLeases(true),
        ])

        if (eventsData.status === 'fulfilled') {
            recentEvents.value = eventsData.value.items ?? []
            totalEventsCount.value = eventsData.value.totalCount ?? 0
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

// Handler: Perform DNS Lookup via API
const handleDnsLookup = async (): Promise<void> => {
    if (!selectedDomain.value) return
    const startTime = performance.now()
    try {
        queryResult.value = await queryDns(selectedDomain.value, selectedRecordType.value)
        queryDurationMs.value = Math.round(performance.now() - startTime)
    } catch (err) {
        queryResult.value = null
        queryDurationMs.value = null
    }
}

// Handler: Clear DNS Cache action
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
    <!-- Header Dialog (Full Width) -->
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
                    Flush Cache
                </WhiptailButton>&nbsp;
                <WhiptailButton class="wt-btn-cancel" @click="handleLogout">
                    Logout
                </WhiptailButton>
            </div>
        </div>
    </header>

    <!-- Error Banner (Full Width) -->
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
            <div class="wt-body wt-stat-value">{{ totalEventsCount }}</div>
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

    <!-- Whiptail Tabs Component -->
    <WhiptailTabs v-model="activeTab" :tabs="dashboardTabs" class="wt-full-width">
        <!-- Tab 1: Quick DNS Lookup -->
        <template #dns-lookup>
            <div class="wt-form-group">
                <label class="wt-label">Target Domain / IP</label>
                <WhiptailCombobox
                    v-model="selectedDomain"
                    :options="deviceComboboxOptions"
                    placeholder="&lt; Type or select option &gt;"
                />
            </div>

            <div class="wt-form-row">
                <div class="wt-form-group">
                    <label class="wt-label">Record Type</label>
                    <select v-model="selectedRecordType" class="wt-input wt-select">
                        <option value="A">A (IPv4)</option>
                        <option value="AAAA">AAAA (IPv6)</option>
                        <option value="CNAME">CNAME</option>
                        <option value="MX">MX</option>
                        <option value="PTR">PTR</option>
                        <option value="TXT">TXT</option>
                    </select>
                </div>
                <WhiptailButton
                    class="wt-btn-ok"
                    :disabled="loading || !selectedDomain"
                    @click="handleDnsLookup"
                >
                    Query
                </WhiptailButton>
            </div>

            <!-- DIG Output Console -->
            <div v-if="queryResult" class="terminal">
                <div class="terminal-header">
                    <span class="terminal-cmd"
                        >$ dig {{ selectedDomain }} {{ selectedRecordType }}</span
                    >
                </div>

                <div class="terminal-section">
                    <span class="terminal-comment"
                        >; &lt;&lt;&gt;&gt; DiG 9.18.12 &lt;&lt;&gt;&gt; {{ selectedDomain }}
                        {{ selectedRecordType }}</span
                    ><br />
                    <span class="terminal-comment">;; global options: +cmd</span><br />
                    <span class="terminal-comment">;; Got answer:</span><br />
                    <span class="terminal-comment"
                        >;; -&gt;&gt;HEADER&lt;&lt;- opcode: QUERY, status:
                        <span class="terminal-highlight">{{ queryResult.responseCode }}</span
                        >, id: {{ Math.floor(Math.random() * 60000) }}</span
                    >
                </div>

                <!-- QUESTION SECTION -->
                <div class="terminal-section">
                    <div class="terminal-section-header">;; QUESTION SECTION:</div>
                    <div class="terminal-record-row">
                        <span class="terminal-name"
                            >;{{
                                selectedDomain.endsWith('.')
                                    ? selectedDomain
                                    : selectedDomain + '.'
                            }}</span
                        >
                        <span class="terminal-class">IN</span>
                        <span class="terminal-type">{{ selectedRecordType }}</span>
                    </div>
                </div>

                <!-- ANSWER SECTION -->
                <div class="terminal-section">
                    <div class="terminal-section-header">;; ANSWER SECTION:</div>
                    <template v-if="parsedAnswers.length > 0">
                        <div
                            v-for="(ans, idx) in parsedAnswers"
                            :key="idx"
                            class="terminal-record-row"
                        >
                            <span class="terminal-name">{{
                                ans.name ||
                                ans.domain ||
                                (selectedDomain.endsWith('.')
                                    ? selectedDomain
                                    : selectedDomain + '.')
                            }}</span>
                            <span class="terminal-ttl">{{ ans.ttl ?? ans.TTL ?? 300 }}</span>
                            <span class="terminal-class">IN</span>
                            <span class="terminal-type">{{
                                ans.type || ans.typeStr || selectedRecordType
                            }}</span>
                            <span class="terminal-data">{{
                                ans.data || ans.value || ans.address || ans
                            }}</span>
                        </div>
                    </template>
                    <div v-else class="terminal-comment">
                        ;; (No records returned or custom raw data format)
                    </div>
                </div>

                <!-- DIG FOOTER METADATA -->
                <div class="terminal-section terminal-footer">
                    <span class="terminal-comment"
                        >;; Query time: {{ queryDurationMs ?? 12 }} msec</span
                    ><br />
                    <span class="terminal-comment">;; SERVER: 127.0.0.1#53(astrolabed-dns)</span
                    ><br />
                    <span class="terminal-comment"
                        >;; WHEN: {{ new Date().toUTCString() }}</span
                    >
                </div>

                <!-- RAW JSON EXPANDER -->
                <details class="terminal-raw-toggle">
                    <summary class="terminal-comment">[ + View Raw Payload ]</summary>
                    <pre class="wt-code-block">{{ JSON.stringify(queryResult, null, 2) }}</pre>
                </details>
            </div>
        </template>

        <!-- Tab 2: LAN Network Devices -->
        <template #lan-devices>
            <div class="wt-table-wrapper">
                <table class="wt-table">
                    <thead>
                        <tr>
                            <th>Host Name</th>
                            <th>IP Address</th>
                            <th>MAC Address</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="dev in lanDevices" :key="dev.macAddress">
                            <td>{{ dev.hostName || '&lt; Unknown &gt;' }}</td>
                            <td>{{ dev.ipAddress }}</td>
                            <td>{{ dev.macAddress }}</td>
                        </tr>
                        <tr v-if="lanDevices.length === 0">
                            <td colspan="3" class="wt-text-muted">
                                &lt; No devices discovered &gt;
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </template>

        <!-- Tab 3: DNS Activity -->
       <template #dns-logs>
    <div class="wt-dialog wt-full-width wt-mt">
        <div class="wt-title wt-box-header">
            <span>Recent DNS Activity</span>
            <WhiptailButton @click="refreshDashboardData"> Refresh Logs </WhiptailButton>
        </div>

        <div class="wt-body wt-table-wrapper">
            <table class="wt-table">
                <thead>
                    <tr>
                        <th>Timestamp</th>
                        <th>Client</th>
                        <th>Query Name</th>
                        <th>Type</th>
                        <th>Status</th>
                        <th>Resolved IP</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="evt in recentEvents" :key="evt.timestamp + evt.queryName">
                        <td>{{ new Date(evt.timestamp).toLocaleTimeString() }}</td>
                        <td>
                            {{
                                evt.clientName
                                    ? `${evt.clientName} (${evt.clientIp})`
                                    : evt.clientIp
                            }}
                        </td>
                        <td>{{ evt.queryName }}</td>
                        <td>[{{ evt.queryType }}]</td>
                        <td>
                            <span :class="evt.status === 'NOERROR' ? 'wt-text-ok' : 'wt-text-err'">
                                {{ evt.status }}
                            </span>
                        </td>
                        <td>{{ evt.responseIp || '-' }}</td>
                    </tr>
                    <tr v-if="recentEvents.length === 0">
                        <td colspan="6" class="wt-text-muted">
                            &lt; No recent DNS logs found &gt;
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>
    </WhiptailTabs>
</template>

<style scoped>
.wt-screen {
    overflow-x: hidden;
}

/* Full Width Utility */
.wt-full-width {
    width: 100%;
    max-width: 100%;
    margin-left: 0;
    margin-right: 0;
    box-sizing: border-box;
}

/* Stats Layout */
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
