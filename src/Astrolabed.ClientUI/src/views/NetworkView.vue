<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">Discovered LAN Devices</h2>
      <button @click="handleCleanup" class="bg-amber-600 hover:bg-amber-500 text-white px-3 py-1.5 rounded text-sm">Purge Stale Devices</button>
    </div>

    <div class="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
      <table class="w-full text-left text-sm text-slate-300">
        <thead class="bg-slate-700/50 text-slate-400 uppercase text-xs">
          <tr>
            <th class="p-4">Host Name</th>
            <th class="p-4">IP Address</th>
            <th class="p-4">MAC Address</th>
            <th class="p-4">Last Seen</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="dev in devices" :key="dev.macAddress" class="border-t border-slate-700">
            <td class="p-4 font-medium text-white">{{ dev.hostName || 'Unknown' }}</td>
            <td class="p-4">{{ dev.ipAddress }}</td>
            <td class="p-4 font-mono text-xs">{{ dev.macAddress }}</td>
            <td class="p-4 text-xs text-slate-400">{{ new Date(dev.lastSeen).toLocaleString() }}</td>
          </tr>
          <tr v-if="devices.length === 0">
            <td colspan="4" class="p-4 text-center text-slate-500">No discovered LAN devices found.</td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination Footer -->
      <div class="p-4 bg-slate-800 border-t border-slate-700 flex items-center justify-between text-xs text-slate-400">
        <div>
          Showing {{ totalCount === 0 ? 0 : ((currentPage - 1) * pageSize) + 1 }} to {{ Math.min(currentPage * pageSize, totalCount) }} of {{ totalCount }} entries
        </div>
        <div class="flex items-center space-x-2">
          <button
            @click="loadDevices(currentPage - 1)"
            :disabled="currentPage <= 1"
            class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
          >
            Previous
          </button>
          <span class="px-2 py-1 text-slate-300">Page {{ currentPage }} of {{ totalPages }}</span>
          <button
            @click="loadDevices(currentPage + 1)"
            :disabled="currentPage >= totalPages"
            class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useApi } from '../composables/useApi'
import type { DiscoveredLanDeviceDto } from '../types/api'

const { getNetworkDevices, cleanupStaleDevices } = useApi()
const devices = ref<DiscoveredLanDeviceDto[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalPages = ref<number>(1)
const totalCount = ref<number>(0)

const loadDevices = async (page = 1): Promise<void> => {
  currentPage.value = page
  const res = await getNetworkDevices(currentPage.value, pageSize.value)
  devices.value = res?.items || []
  totalCount.value = res?.totalCount || 0
  totalPages.value = res?.totalPages || Math.ceil(totalCount.value / pageSize.value) || 1
}

const handleCleanup = async (): Promise<void> => {
  const cutoff = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString()
  if (confirm('Clean up devices not seen in the last 7 days?')) {
    await cleanupStaleDevices(cutoff)
    await loadDevices(1)
  }
}

onMounted(() => loadDevices(1))
</script>
