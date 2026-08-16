<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import {
  useAstrolabedApi,
  type DiscoveredLanDeviceDto,
  type DnsResponseEvent,
} from '../composables/useAstrolabedApi'
import WhiptailCombobox from '../components/WhiptailCombobox.vue'
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
  <div class="wt-dashboard">
    <!-- Header -->
    <header class="wt-header">
      <div class="wt-header-title">
        <h1>Astrolabed Control Center</h1>
        <span class="wt-badge" :class="{ 'wt-badge-active': !loading }">
          {{ loading ? 'SYSTEM BUSY' : 'ONLINE' }}
        </span>
      </div>
      <button type="button" class="wt-button wt-button-danger" @click="handleFlushCache">
        Flush DNS Cache
      </button>
    </header>

    <!-- Error Banner -->
    <div v-if="error" class="wt-alert-error">
      <strong>System Alert:</strong>
      {{ typeof error === 'string' ? error : error.detail || error.title || 'An API error occurred' }}
    </div>

    <!-- Stat Cards Grid -->
    <div class="wt-stats-grid">
      <div class="wt-card">
        <div class="wt-card-label">Total DNS Events</div>
        <div class="wt-card-value">{{ totalEventsCount }}</div>
      </div>
      <div class="wt-card">
        <div class="wt-card-label">Active DHCP Leases</div>
        <div class="wt-card-value">{{ activeLeasesCount }}</div>
      </div>
      <div class="wt-card">
        <div class="wt-card-label">Discovered Network Devices</div>
        <div class="wt-card-value">{{ lanDevices.length }}</div>
      </div>
    </div>

    <!-- Main Content Layout -->
    <div class="wt-dashboard-body">
      <!-- Left Column: Interactive DNS Query Sandbox -->
      <section class="wt-card wt-panel">
        <h2>Quick DNS Lookup</h2>
        <div class="wt-form-group">
          <label class="wt-label">Target Domain / IP</label>
          <!-- Custom Whiptail Combobox -->
          <WhiptailCombobox
            v-model="selectedDomain"
            :options="deviceComboboxOptions"
            placeholder="Type or select domain..."
          />
        </div>

        <div class="wt-form-row">
          <div class="wt-form-group">
            <label class="wt-label">Record Type</label>
            <select v-model="selectedRecordType" class="wt-select">
              <option value="A">A (IPv4)</option>
              <option value="AAAA">AAAA (IPv6)</option>
              <option value="CNAME">CNAME</option>
              <option value="MX">MX</option>
              <option value="TXT">TXT</option>
            </select>
          </div>
          <button
            type="button"
            class="wt-button wt-button-primary"
            :disabled="loading || !selectedDomain"
            @click="handleDnsLookup"
          >
            Query
          </button>
        </div>

        <!-- Raw JSON Query Result -->
        <div v-if="queryResult" class="wt-result-box">
          <div class="wt-result-header">Response Data:</div>
          <pre class="wt-code-block">{{ JSON.stringify(queryResult, null, 2) }}</pre>
        </div>
      </section>

      <!-- Right Column: Discovered Devices Table -->
      <section class="wt-card wt-panel">
        <h2>LAN Network Devices</h2>
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
                <td>{{ dev.hostName || '< Unknown >' }}</td>
                <td class="wt-code-text">{{ dev.ipAddress }}</td>
                <td class="wt-code-text">{{ dev.macAddress }}</td>
              </tr>
              <tr v-if="lanDevices.length === 0">
                <td colspan="3" class="wt-text-muted">No devices discovered yet.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>

    <!-- Recent DNS Response Events Feed -->
    <section class="wt-card wt-panel wt-mt">
      <div class="wt-panel-header">
        <h2>Recent DNS Activity</h2>
        <button type="button" class="wt-button-link" @click="refreshDashboardData">Refresh Logs</button>
      </div>

      <div class="wt-table-wrapper">
        <table class="wt-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Client IP</th>
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
              <td class="wt-code-text">{{ evt.queryName }}</td>
              <td><span class="wt-tag">{{ evt.queryType }}</span></td>
              <td>
                <span
                  class="wt-status"
                  :class="evt.status === 'NOERROR' ? 'wt-status-ok' : 'wt-status-err'"
                >
                  {{ evt.status }}
                </span>
              </td>
              <td class="wt-code-text">{{ evt.responseIp || '-' }}</td>
            </tr>
            <tr v-if="recentEvents.length === 0">
              <td colspan="6" class="wt-text-muted">No recent DNS query logs found.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
.wt-dashboard {
  padding: 24px;
  background-color: #0d1117;
  color: #c9d1d9;
  min-height: 100vh;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.wt-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  border-bottom: 1px solid #30363d;
  padding-bottom: 16px;
}

.wt-header-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.wt-header h1 {
  margin: 0;
  font-size: 1.5rem;
  color: #f0f6fc;
}

.wt-badge {
  padding: 2px 8px;
  font-size: 0.75rem;
  border-radius: 12px;
  background-color: #30363d;
  color: #8b949e;
  font-weight: bold;
}

.wt-badge-active {
  background-color: #238636;
  color: #ffffff;
}

.wt-alert-error {
  background-color: #3c1e1e;
  border: 1px solid #f85149;
  color: #ff7b72;
  padding: 12px;
  border-radius: 6px;
  margin-bottom: 20px;
}

.wt-stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.wt-card {
  background-color: #161b22;
  border: 1px solid #30363d;
  border-radius: 6px;
  padding: 16px;
}

.wt-card-label {
  font-size: 0.85rem;
  color: #8b949e;
  margin-bottom: 8px;
}

.wt-card-value {
  font-size: 1.8rem;
  font-weight: bold;
  color: #f0f6fc;
}

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

.wt-panel h2 {
  margin-top: 0;
  font-size: 1.1rem;
  color: #f0f6fc;
  margin-bottom: 16px;
}

.wt-panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.wt-form-group {
  margin-bottom: 16px;
}

.wt-form-row {
  display: flex;
  gap: 12px;
  align-items: flex-end;
}

.wt-label {
  display: block;
  font-size: 0.85rem;
  margin-bottom: 6px;
  color: #8b949e;
}

.wt-select {
  width: 100%;
  padding: 8px;
  background-color: #0d1117;
  border: 1px solid #30363d;
  color: #c9d1d9;
  border-radius: 4px;
}

.wt-button {
  padding: 8px 16px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  border: none;
}

.wt-button-primary {
  background-color: #238636;
  color: #ffffff;
}

.wt-button-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.wt-button-danger {
  background-color: #da3633;
  color: #ffffff;
}

.wt-button-link {
  background: none;
  border: none;
  color: #58a6ff;
  cursor: pointer;
  text-decoration: underline;
}

.wt-result-box {
  margin-top: 16px;
  background-color: #0d1117;
  border: 1px solid #30363d;
  border-radius: 4px;
  padding: 12px;
}

.wt-code-block {
  margin: 8px 0 0;
  font-family: monospace;
  font-size: 0.85rem;
  color: #7ee787;
  overflow-x: auto;
}

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
  border-bottom: 1px solid #30363d;
  padding: 8px;
  color: #8b949e;
}

.wt-table td {
  border-bottom: 1px solid #21262d;
  padding: 10px 8px;
}

.wt-code-text {
  font-family: monospace;
}

.wt-tag {
  background-color: #21262d;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 0.75rem;
}

.wt-status-ok {
  color: #7ee787;
}

.wt-status-err {
  color: #ff7b72;
}

.wt-text-muted {
  color: #8b949e;
  text-align: center;
}

.wt-mt {
  margin-top: 20px;
}
</style>
