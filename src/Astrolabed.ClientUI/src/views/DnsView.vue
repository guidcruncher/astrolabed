<template>
  <div>
    <AppToolbar>
      <AppButton
        variant="danger"
        @click="handlePurge"
        class="inline-flex items-center gap-2 whitespace-nowrap"
        ><Trash2 /> Purge Log History
      </AppButton>
    </AppToolbar>

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
      <template #cell-questionName="{ row, value }">
        <span class="inline-flex items-center gap-1 whitespace-nowrap"
          ><ShieldAlert class="text-rose-600" v-if="row.blocked" /><Check
            class="text-emerald-600"
            v-else
          />
          <span class="font-medium text-white"
            >{{ value }}
            <span v-if="row.heuristicScore">h{{ row.heuristicScore?.toFixed(2) }}</span>
          </span></span
        >
      </template>

      <template #cell-questionType="{ value }">
        <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">{{ value }}</span>
      </template>

      <template #cell-resolutionSource="{ value }">
        <span class="text-xs">{{ formatResolution(value) }}</span>
      </template>

      <template #cell-client="{ row }">
        <span>{{ row.clientName || row.clientAddress }}</span>
      </template>

      <template #cell-durationMs="{ value }">
        <span class="font-mono text-xs">{{ value }} ms</span>
      </template>

      <template #empty> No DNS event logs found. </template>
    </DataGrid>
  </div>
  <BaseModal v-model="modalVisible" title="DNS Event Details">
    <table class="w-full text-left text-sm text-gray-500 dark:text-gray-400">
      <tbody class="divide-y divide-gray-200 dark:divide-gray-700" v-if="dnsRow">
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Start Time (UTC)</td>
          <td class="px-3 py-2">{{ formatUtc(dnsRow.startTimeUtc) }}</td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Question</td>
          <td class="px-3 py-2 font-mono text-blue-600 dark:text-blue-400">
            {{ dnsRow.questionName ?? 'N/A' }}
            <span v-if="dnsRow.heuristicScore">h{{ dnsRow.heuristicScore?.toFixed(2) }}</span>
          </td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Type</td>
          <td class="px-3 py-2 font-semibold">{{ dnsRow.questionType ?? 'N/A' }}</td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Client</td>
          <td class="px-3 py-2 font-mono text-xs">
            {{ dnsRow.clientAddress ?? 'N/A' }} {{ dnsRow.clientName ?? 'N/A' }}
          </td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Response</td>
          <td class="px-3 py-2 font-mono">{{ dnsRow.rcode ?? 'N/A' }}</td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Duration (ms)</td>
          <td class="px-3 py-2">
            {{ dnsRow.durationMs !== undefined ? `${dnsRow.durationMs} ms` : 'N/A' }}
          </td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Status</td>
          <td class="px-3 py-2">
            <span
              v-if="dnsRow.blocked === 1"
              class="rounded bg-red-100 px-2.5 py-0.5 text-xs font-medium text-red-800 dark:bg-red-900 dark:text-red-300"
            >
              Blocked
            </span>
            <span
              v-else
              class="rounded bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-800 dark:bg-green-900 dark:text-green-300"
            >
              Allowed
            </span>
          </td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Upstream</td>
          <td class="px-3 py-2 font-mono text-xs">{{ dnsRow.upstream }}</td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">TTL (Seconds)</td>
          <td class="px-3 py-2 font-mono">{{ dnsRow.ttlSeconds }}s</td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Block List</td>
          <td class="px-3 py-2 font-mono">{{ getBlockList(dnsRow.blockRuleId) }}</td>
        </tr>
        <tr class="bg-white hover:bg-gray-50 dark:bg-gray-800 dark:hover:bg-gray-600">
          <td class="px-3 py-2 font-medium text-gray-900 dark:text-white">Block Rule Pattern</td>
          <td class="px-3 py-2 font-mono text-xs">{{ dnsRow.blockRulePattern ?? 'N/A' }}</td>
        </tr>
      </tbody>
    </table>
    <template #actions>
      <button
        type="button"
        class="px-4 py-2 bg-sky-600 hover:bg-sky-500 text-sm font-medium rounded text-white"
        @click="modalVisible = false"
      >
        Close
      </button>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DnsListEntity, DnsResponseEventEntity } from '../types/api'
import { Check, ShieldAlert, Trash2 } from '@lucide/vue'

const { getLists, getDnsEvents, purgeDnsEvents } = useApi()
const modalVisible = ref(false)

const events = ref<DnsResponseEventEntity[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)
const dnsLists = ref<DnsListEntity[]>()
const dnsRow = ref<DnsResponseEventEntity>()

const columns: Column[] = [
  { key: 'questionName', label: 'Domain Question' },
  { key: 'questionType', label: 'Type' },
  { key: 'resolutionSource', label: 'Source' },
  { key: 'client', label: 'Client' },
  { key: 'upstream', label: 'Upstream' },
  { key: 'durationMs', label: 'Latency' },
]

const loadLogs = async (): Promise<void> => {
  const data = await getDnsEvents(currentPage.value, pageSize.value)
  events.value = data?.items || []
  totalCount.value = data?.totalCount || 0
}

const getBlockList = (id: any) => {
  if (!id) {
    return 'N/A'
  }
  if (!dnsLists.value) {
    return '-'
  }

  for (var i = 0; i < dnsLists.value.length; i++) {
    if (dnsLists.value[i].id == id) {
      return dnsLists.value[i].name
    }
  }

  return 'N/A'
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
  dnsRow.value = row
  modalVisible.value = true
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

const formatResolution = (value: any) => {
  if (value) {
    return value.replace('BLOCKED_', '').replace('CONDITIONAL_PTR_', '')
  }
  return ''
}

const formatUtc = (timestamp?: number): string => {
  if (!timestamp) return 'N/A'
  return new Date(timestamp).toUTCString()
}

onMounted(async () => {
  dnsLists.value = await getLists()
  loadLogs()
})
</script>
