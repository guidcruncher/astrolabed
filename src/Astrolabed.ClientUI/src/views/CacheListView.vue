<template>
  <div>
    <AppToolbar>
      <AppButton
        variant="danger"
        @click="handleClear"
        class="inline-flex items-center gap-2 whitespace-nowrap"
      >
        <Trash2 /> Clear DNS Cache
      </AppButton>
    </AppToolbar>

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
      <template #cell-value-payload-questionName="{ row, value }">
        <span class="font-medium text-white">
          {{ typeof value === 'string' ? value : row.payload?.questionName || 'N/A' }}
        </span>
      </template>

      <template #cell-value-payload-questionType="{ row, value }">
        <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">
          {{ formatDnsType(typeof value === 'number' ? value : row.payload?.questionType) }}
        </span>
      </template>

      <template #cell-value-expiresAt="{ row, value }">
        <span class="font-mono text-xs">
          {{ formatDate(value ?? row.expiresAt) }}
        </span>
      </template>

      <template #cell-value-isExpired="{ row, value }">
        <span
          class="text-xs px-2 py-0.5 rounded font-medium"
          :class="
            (value ?? row.isExpired)
              ? 'bg-red-900/50 text-red-300'
              : 'bg-green-900/50 text-green-300'
          "
        >
          {{ (value ?? row.isExpired) ? 'Expired' : 'Active' }}
        </span>
      </template>

      <template #empty> No cached DNS entries found. </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { CacheEntryView } from '../types/api'
import { dnsTypeMap } from '../types/dnstypes'
import { Trash2 } from '@lucide/vue'

const { getCacheEntries, clearCache } = useApi()

const entries = ref<CacheEntryView[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'value.payload.questionName', label: 'Domain Question' },
  { key: 'value.payload.questionType', label: 'Type' },
  { key: 'value.expiresAt', label: 'Expiration' },
  { key: 'value.isExpired', label: 'Status' },
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

const formatDnsType = (type?: number): string => {
  if (type === undefined || type === null) return 'UNKNOWN'
  return dnsTypeMap[type] || `TYPE_${type}`
}

onMounted(() => {
  loadCache()
})
</script>
