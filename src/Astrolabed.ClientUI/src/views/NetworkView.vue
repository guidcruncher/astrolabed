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
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useApi } from '../composables/useApi'
import type { DiscoveredLanDeviceDto } from '../types/api'

const { getNetworkDevices, cleanupStaleDevices } = useApi()
const devices = ref<DiscoveredLanDeviceDto[]>([])

const loadDevices = async (): Promise<void> => {
  const res = await getNetworkDevices()
  devices.value = res?.items || []
}

const handleCleanup = async (): Promise<void> => {
  const cutoff = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString()
  if (confirm('Clean up devices not seen in the last 7 days?')) {
    await cleanupStaleDevices(cutoff)
    await loadDevices()
  }
}

onMounted(loadDevices)
</script>
