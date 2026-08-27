<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">DNS Cache Entries</h2>
      <AppButton variant="danger" @click="handleClear">
        Clear DNS Cache
      </AppButton>
    </div>

    <DataGrid
      v-model:page="currentPage"
      v-model:pageSize="pageSize"
      :data="entries"
      :columns="columns"
      :total-count="totalCount"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
      @row-select="handleRowSelect"
      @loaded="handleGridLoaded"
    >
      <template #cell-questionName="{ row }">
        <span class="font-medium text-white">{{ row.payload?.questionName || 'N/A' }}</span>
      </template>

      <template #cell-questionType="{ row }">
        <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">
          {{ formatDnsType(row.payload?.questionType) }}
        </span>
      </template>

      <template #cell-expiresAt="{ value }">
        <span class="font-mono text-xs">{{ formatDate(value) }}</span>
      </template>

      <template #cell-status="{ row }">
        <span
          class="text-xs px-2 py-0.5 rounded font-medium"
          :class="row.isExpired ? 'bg-red-900/50 text-red-300' : 'bg-green-900/50 text-green-300'"
        >
          {{ row.isExpired ? 'Expired' : 'Active' }}
        </span>
      </template>

      <template #empty>
        No cached DNS entries found.
      </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { CacheEntryView } from '../types/api'

const { getCacheEntries, clearCache } = useApi()

const entries = ref<CacheEntryView[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'questionName', label: 'Domain Question' },
  { key: 'questionType', label: 'Type' },
  { key: 'expiresAt', label: 'Expiration' },
  { key: 'status', label: 'Status' }
]

const loadCache = async (): Promise<void> => {
  const data = await getCacheEntries(currentPage.value, pageSize.value)
  entries.value = data?.items || []
  totalCount.value = data?.totalCount || 0
}

const handlePageChange = (newPage: number): void => {
  currentPage.value = newPage
  loadCache()
}

const handlePageSizeChange = (newSize: number): void => {
  pageSize.value = newSize
  currentPage.value = 1
  loadCache()
}

const handleRowSelect = (row: CacheEntryView): void => {
  console.log('Selected Cache Entry:', row)
}

const handleGridLoaded = (): void => {
  // Executed on grid initialization and data updates
}

const handleClear = async (): Promise<void> => {
  if (confirm('Are you sure you want to clear all DNS cache entries?')) {
    await clearCache()
    currentPage.value = 1
    await loadCache()
  }
}

const formatDate = (dateString?: string): string => {
  if (!dateString) return 'N/A'
  return new Date(dateString).toLocaleString()
}

const formatDnsType = (type?: number): string => {
  if (type === undefined || type === null) return 'UNKNOWN'
  const dnsTypeMap: Record<number, string> = {
    1: 'A',
    2: 'NS',
    5: 'CNAME',
    6: 'SOA',
    12: 'PTR',
    15: 'MX',
    16: 'TXT',
    28: 'AAAA',
    33: 'SRV',
    41: 'OPT'
  }
  return dnsTypeMap[type] || `TYPE_${type}`
}

onMounted(() => {
  loadCache()
})
</script>
