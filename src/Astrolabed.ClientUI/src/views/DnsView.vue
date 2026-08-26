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
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useApi } from '../composables/useApi'
import type { DnsResponseEventEntity } from '../types/api'

const { getDnsEvents, purgeDnsEvents } = useApi()
const events = ref<DnsResponseEventEntity[]>([])

const loadLogs = async (): Promise<void> => {
  const data = await getDnsEvents()
  events.value = data?.items || []
}

const handlePurge = async (): Promise<void> => {
  if (confirm('Purge historical DNS event records?')) {
    await purgeDnsEvents()
    await loadLogs()
  }
}

onMounted(loadLogs)
</script>
