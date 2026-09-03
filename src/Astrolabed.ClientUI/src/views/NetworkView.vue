<template>
  <div>
    <AppToolbar>
      <AppButton
        variant="warn"
        @click="handleCleanup"
        class="inline-flex items-center gap-2 whitespace-nowrap"
        ><Trash2 /> Purge Stale Devices
      </AppButton>
    </AppToolbar>

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
      <template #cell-deviceType="{ row, value }"
        ><span class="font-medium text-white"><DeviceType :deviceType="row.deviceType" /></span
      ></template>

      <template #cell-hostName="{ row, value }">
        <div class="flex flex-col items-start gap-1">
          <span class="font-medium text-white">{{ value || 'Unknown' }}</span>
          <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">
            {{ row.deviceType || 'Unknown' }}
          </span>
        </div>
      </template>

      <template #cell-ipAddress="{ value }">
        <span>{{ value }}</span>
      </template>

      <template #cell-macAddress="{ value }">
        <span class="font-mono text-xs">{{ value }}</span>
      </template>

      <template #cell-vendor="{ value }">
        <span>{{ value }}</span>
      </template>

      <template #cell-lastSeen="{ value }">
        <span class="text-xs text-slate-400">
          {{ value ? formatDate(value) : '-' }}
        </span>
      </template>

      <template #empty> No discovered LAN devices found. </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DiscoveredLanDeviceDto } from '../types/api'
import { CircleQuestionMark, Trash2 } from '@lucide/vue'

const { getNetworkDevices, cleanupStaleDevices } = useApi()

const devices = ref<DiscoveredLanDeviceDto[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'deviceType', label: ' ' },
  { key: 'hostName', label: 'Host Name' },
  { key: 'ipAddress', label: 'IP Address' },
  { key: 'macAddress', label: 'MAC Address' },
  { key: 'vendor', label: 'Vendor' },
  { key: 'lastSeen', label: 'Last Seen' },
]

const formatDate = (dateValue?: unknown): string => {
  if (
    !dateValue ||
    (typeof dateValue !== 'string' && typeof dateValue !== 'number' && !(dateValue instanceof Date))
  ) {
    return 'N/A'
  }
  const parsed = new Date(dateValue)
  return isNaN(parsed.getTime()) ? 'N/A' : parsed.toLocaleString()
}

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
