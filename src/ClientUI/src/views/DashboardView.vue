<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import {
  useAstrolabedApi,
  type DiscoveredLanDeviceDto,
  type DnsResponseEvent,
} from '../composables/useAstrolabedApi'
import WhiptailCombobox from '../components/WhiptailCombobox.vue'
import WhiptailButton from '../components/WhiptailButton.vue'
import type { WhiptailOption } from '../components/types'

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

// Reactive State
const selectedDomain = ref('')
const selectedRecordType = ref('A')
const queryResult = ref<any>(null)
const recentEvents = ref<DnsResponseEvent[]>([])
const lanDevices = ref<DiscoveredLanDeviceDto[]>([])
const activeLeasesCount = ref<number>(0)
const totalEventsCount = ref<number>(0)

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
  try {
    queryResult.value = await queryDns(selectedDomain.value, selectedRecordType.value)
  } catch (err) {
    queryResult.value = null
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
  <div class="wt-screen">
    <!-- Header Dialog -->
    <header class="wt-dialog">
      <div class="wt-title wt-header-title">
        <span>[ Astrolabed Control Center ]</span>
        <span class="wt-status-indicator" :class="{ 'wt-status-busy': loading }">
          &lt; {{ loading ? 'PROCESSING' : 'ONLINE' }} &gt;
        </span>
      </div>
      <div class="wt-body wt-header-body">
        <span>System Diagnostics & Status Overview</span>
        <WhiptailButton class="wt-btn-cancel" @click="handleFlushCache">
          Flush Cache
        </WhiptailButton>
      </div>
    </header>

    <!-- Error Banner -->
    <div v-if="error" class="wt-dialog wt-alert-error">
      <div class="wt-title wt-title-error">SYSTEM ERROR</div>
      <div class="wt-body">
        {{ typeof error === 'string' ? error : error.detail || error.title || 'An API error occurred' }}
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

    <!-- Main Content Layout -->
    <div class="wt-dashboard-body">
      <!-- Left Column: Interactive DNS Query Sandbox -->
      <section class="wt-dialog">
        <div class="wt-title">Quick DNS Lookup</div>
        <div class="wt-body">
          <div class="wt-form-group">
            <label class="wt-label">Target Domain / IP</label>
            <WhiptailCombobox
              v-model="selectedDomain"
              :options="deviceComboboxOptions"
              placeholder="< Type or select option >"
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

          <!-- Raw JSON Query Result -->
          <div v-if="queryResult" class="wt-message wt-result-box">
            <pre class="wt-code-block">{{ JSON.stringify(queryResult, null, 2) }}</pre>
          </div>
        </div>
      </section>

      <!-- Right Column: Discovered Devices Table -->
      <section class="wt-dialog">
        <div class="wt-title">LAN Network Devices</div>
        <div class="wt-body wt-table-wrapper">
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
                <td>{{ dev.hostName || '< Unknown >' }}</td>
                <td>{{ dev.ipAddress }}</td>
                <td>{{ dev.macAddress }}</td>
              </tr>
              <tr v-if="lanDevices.length === 0">
                <td colspan="3" class="wt-text-muted">&lt; No devices discovered &gt;</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>

    <!-- Recent DNS Response Events Feed -->
    <section class="wt-dialog wt-mt">
      <div class="wt-title wt-box-header">
        <span>Recent DNS Activity</span>
        <WhiptailButton @click="refreshDashboardData">
          Refresh Logs
        </WhiptailButton>
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
              <td>{{ evt.clientName ? `${evt.clientName} (${evt.clientIp})` : evt.clientIp }}</td>
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
              <td colspan="6" class="wt-text-muted">&lt; No recent DNS logs found &gt;</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;700&display=swap');

.wt-screen {
  width: 100vw;
  min-height: 100vh;
  padding: 20px;
  background-color: #000;
  color: #e0e0e0;
  font-family: 'JetBrains Mono', 'Fira Code', 'Cascadia Code', 'Consolas', monospace;
  font-size: 16px;
  box-sizing: border-box;
}

.wt-dialog {
  background-color: #1b1b1b;
  border: 2px solid #c0c0c0;
  box-shadow:
    0 0 0 1px #000,
    0 0 10px #000;
  margin: 0 0 20px 0;
  max-width: 100%;
}

.wt-title {
  background-color: #005f87;
  color: #fff;
  padding: 6px 10px;
  font-weight: bold;
  border-bottom: 1px solid #000;
}

.wt-title-error {
  background-color: #870000;
}

.wt-body {
  padding: 12px 14px;
  line-height: 1.4;
}

.wt-footer {
  padding: 8px 10px;
  border-top: 1px solid #000;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  background-color: #1b1b1b;
}

/* Header & Title Adjustments */
.wt-header-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.wt-header-body {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.wt-status-indicator {
  color: #00ff00;
  font-weight: bold;
}

.wt-status-busy {
  color: #ffff00;
}

/* Stats Layout */
.wt-stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 20px;
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

/* Alert Error Override */
.wt-alert-error {
  border-color: #ff0000;
}

/* Main Body Layout */
.wt-dashboard-body {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

@media (max-width: 900px) {
  .wt-dashboard-body {
    grid-template-columns: 1fr;
  }
}

/* Form Styles using standard .wt-input and .wt-label */
.wt-form-group {
  margin-bottom: 12px;
}

.wt-form-row {
  display: flex;
  gap: 12px;
  align-items: flex-end;
}

.wt-label {
  display: block;
  font-size: 0.9rem;
  color: #c0c0c0;
  margin-bottom: 4px;
}

.wt-input {
  width: 100%;
  padding: 4px 6px;
  background-color: #000;
  border: 1px solid #5f5f5f;
  color: #e0e0e0;
  font-family: inherit;
}

.wt-input:focus {
  outline: none;
  border-color: #ffff00;
}

.wt-select {
  cursor: pointer;
}

/* Code Output */
.wt-message {
  padding: 10px;
  background-color: #000;
  border: 1px solid #5f5f5f;
  margin-top: 10px;
}

.wt-result-box {
  margin-top: 12px;
}

.wt-code-block {
  margin: 0;
  color: #00ff00;
  font-size: 0.9rem;
  overflow-x: auto;
  white-space: pre-wrap;
  font-family: inherit;
}

/* Table Design matching Whiptail aesthetics */
.wt-table-wrapper {
  overflow-x: auto;
}

.wt-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.9rem;
}

.wt-table th {
  border-bottom: 1px solid #c0c0c0;
  padding: 6px;
  color: #ffff00;
}

.wt-table td {
  border-bottom: 1px solid #333333;
  padding: 6px;
}

.wt-table tr:hover {
  background-color: #333;
}

.wt-text-ok {
  color: #00ff00;
}

.wt-text-err {
  color: #ff0000;
}

.wt-text-muted {
  color: #5f5f5f;
  text-align: center;
}

.wt-box-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.wt-mt {
  margin-top: 20px;
}
</style>
