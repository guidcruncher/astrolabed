<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">DNS Event Logs</h2>
      <button @click="handlePurge" class="bg-rose-600 hover:bg-rose-500 text-white px-3 py-1.5 rounded text-sm">Purge Log History</button>
    </div>

    <div class="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
      <table class="w-full text-left text-sm text-slate-300">
        <thead class="bg-slate-700/50 text-slate-400 uppercase text-xs">
          <tr>
            <th class="p-4">Domain Question</th>
            <th class="p-4">Type</th>
            <th class="p-4">Source</th>
            <th class="p-4">Client</th>
            <th class="p-4">Latency</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="evt in events" :key="evt.id" class="border-t border-slate-700">
            <td class="p-4 font-medium text-white">{{ evt.questionName }}</td>
            <td class="p-4"><span class="bg-slate-700 text-xs px-2 py-0.5 rounded">{{ evt.questionType }}</span></td>
            <td class="p-4 text-xs">{{ evt.resolutionSource }}</td>
            <td class="p-4">{{ evt.clientName || evt.clientEndpoint }}</td>
            <td class="p-4 font-mono text-xs">{{ evt.durationMs }} ms</td>
          </tr>
          <tr v-if="events.length === 0">
            <td colspan="5" class="p-4 text-center text-slate-500">No DNS event logs found.</td>
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
            @click="loadLogs(currentPage - 1)"
            :disabled="currentPage <= 1"
            class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
          >
            Previous
          </button>
          <span class="px-2 py-1 text-slate-300">Page {{ currentPage }} of {{ totalPages }}</span>
          <button
            @click="loadLogs(currentPage + 1)"
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
import type { DnsResponseEventEntity } from '../types/api'

const { getDnsEvents, purgeDnsEvents } = useApi()
const events = ref<DnsResponseEventEntity[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalPages = ref<number>(1)
const totalCount = ref<number>(0)

const loadLogs = async (page = 1): Promise<void> => {
  currentPage.value = page
  const data = await getDnsEvents(currentPage.value, pageSize.value)
  events.value = data?.items || []
  totalCount.value = data?.totalCount || 0
  totalPages.value = data?.totalPages || Math.ceil(totalCount.value / pageSize.value) || 1
}

const handlePurge = async (): Promise<void> => {
  if (confirm('Purge historical DNS event records?')) {
    await purgeDnsEvents()
    await loadLogs(1)
  }
}

onMounted(() => loadLogs(1))
</script>
