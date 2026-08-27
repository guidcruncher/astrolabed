<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">Discovered LAN Devices</h2>
      <button 
        @click="handleCleanup" 
        class="bg-amber-600 hover:bg-amber-500 text-white px-3 py-1.5 rounded text-sm transition-colors"
      >
        Purge Stale Devices
      </button>
    </div>

    <DataGrid
      v-model:page="currentPage"
      v-model:pageSize="pageSize"
      :data="devices"
      :columns="columns"
      :total-count="totalCount"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
      @row-select="handleRowSelect"
      @loaded="handleGridLoaded"
    >
      <template #cell-hostName="{ value }">
        <span class="font-medium text-white">{{ value || 'Unknown' }}</span>
      </template>

      <template #cell-ipAddress="{ value }">
        <span>{{ value }}</span>
      </template>

      <template #cell-macAddress="{ value }">
        <span class="font-mono text-xs">{{ value }}</span>
      </template>

      <template #cell-lastSeen="{ value }">
        <span class="text-xs text-slate-400">
          {{ value ? new Date(value).toLocaleString() : '-' }}
        </span>
      </template>

      <template #empty>
        No discovered LAN devices found.
      </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DiscoveredLanDeviceDto } from '../types/api'

const { getNetworkDevices, cleanupStaleDevices } = useApi()

const devices = ref<DiscoveredLanDeviceDto[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'hostName', label: 'Host Name' },
  { key: 'ipAddress', label: 'IP Address' },
  { key: 'macAddress', label: 'MAC Address' },
  { key: 'lastSeen', label: 'Last Seen' }
]

const loadDevices = async (): Promise<void> => {
  const res = await getNetworkDevices(currentPage.value, pageSize.value)
  devices.value = res?.items || []
  totalCount.value = res?.totalCount || 0
}

const handlePageChange = (newPage: number): void => {
  currentPage.value = newPage
  loadDevices()
}

const handlePageSizeChange = (newSize: number): void => {
  pageSize.value = newSize
  currentPage.value = 1
  loadDevices()
}

const handleRowSelect = (device: DiscoveredLanDeviceDto): void => {
  console.log('Selected LAN Device:', device)
}

const handleGridLoaded = (): void => {
  // Triggered when data updates or grid mounts
}

const handleCleanup = async (): Promise<void> => {
  const cutoff = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString()
  if (confirm('Clean up devices not seen in the last 7 days?')) {
    await cleanupStaleDevices(cutoff)
    currentPage.value = 1
    await loadDevices()
  }
}

onMounted(() => {
  loadDevices()
})
</script>
