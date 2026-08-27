<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">DNS Event Logs</h2>
      <AppButton variant="danger" 
        @click="handlePurge" 
      >
        Purge Log History
      </AppButton>
    </div>

    <DataGrid
      v-model:page="currentPage"
      v-model:pageSize="pageSize"
      :data="events"
      :columns="columns"
      :total-count="totalCount"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
      @row-select="handleRowSelect"
      @loaded="handleGridLoaded"
    >
      <template #cell-questionName="{ value }">
        <span class="font-medium text-white">{{ value }}</span>
      </template>

      <template #cell-questionType="{ value }">
        <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">{{ value }}</span>
      </template>

      <template #cell-resolutionSource="{ value }">
        <span class="text-xs">{{ value }}</span>
      </template>

      <template #cell-client="{ row }">
        <span>{{ row.clientName || row.clientEndpoint }}</span>
      </template>

      <template #cell-durationMs="{ value }">
        <span class="font-mono text-xs">{{ value }} ms</span>
      </template>

      <template #empty>
        No DNS event logs found.
      </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DnsResponseEventEntity } from '../types/api'

const { getDnsEvents, purgeDnsEvents } = useApi()

const events = ref<DnsResponseEventEntity[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'questionName', label: 'Domain Question' },
  { key: 'questionType', label: 'Type' },
  { key: 'resolutionSource', label: 'Source' },
  { key: 'client', label: 'Client' },
  { key: 'durationMs', label: 'Latency' }
]

const loadLogs = async (): Promise<void> => {
  const data = await getDnsEvents(currentPage.value, pageSize.value)
  events.value = data?.items || []
  totalCount.value = data?.totalCount || 0
}

const handlePageChange = (newPage: number): void => {
  currentPage.value = newPage
  loadLogs()
}

const handlePageSizeChange = (newSize: number): void => {
  pageSize.value = newSize
  currentPage.value = 1
  loadLogs()
}

const handleRowSelect = (row: DnsResponseEventEntity): void => {
  console.log('Selected DNS Event:', row)
}

const handleGridLoaded = (): void => {
  // Executed on grid initialization and data updates
}

const handlePurge = async (): Promise<void> => {
  if (confirm('Purge historical DNS event records?')) {
    await purgeDnsEvents()
    currentPage.value = 1
    await loadLogs()
  }
}

onMounted(() => {
  loadLogs()
})
</script>
