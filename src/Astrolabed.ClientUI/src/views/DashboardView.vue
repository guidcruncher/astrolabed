<template>
  <div>
    <h2 class="text-2xl font-bold mb-6">System Overview</h2>

    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div class="bg-slate-800 p-6 rounded-lg border border-slate-700">
        <h3 class="text-sm font-semibold text-slate-400 uppercase">Operational Status</h3>
        <p class="text-2xl font-bold mt-2" :class="status?.status === 'Healthy' ? 'text-emerald-400' : 'text-amber-400'">
          {{ status?.status || 'Loading...' }}
        </p>
        <span class="text-xs text-slate-500 mt-4 block">
          Last Check: {{ status?.timestamp ? new Date(status.timestamp).toLocaleString() : 'N/A' }}
        </span>
      </div>

      <div class="bg-slate-800 p-6 rounded-lg border border-slate-700">
        <h3 class="text-sm font-semibold text-slate-400 uppercase">Cached DNS Items</h3>
        <p class="text-3xl font-bold mt-2 text-sky-400">{{ cacheCount !== null ? cacheCount : '-' }}</p>
        <button
          @click="handleClearCache"
          class="mt-4 text-xs bg-rose-600 hover:bg-rose-500 text-white px-3 py-1.5 rounded transition"
        >
          Purge DNS Cache
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useApi } from '../composables/useApi'
import type { AstrolabedStatusResponse } from '../types/api'

const { getStatus, getCacheCount, clearCache } = useApi()
const status = ref<AstrolabedStatusResponse | null>(null)
const cacheCount = ref<number | null>(null)

const fetchData = async (): Promise<void> => {
  try {
    status.value = await getStatus()
    const countRes = await getCacheCount()
    cacheCount.value = countRes?.count ?? 0
  } catch (e) {
    console.error(e)
  }
}

const handleClearCache = async (): Promise<void> => {
  if (confirm('Are you sure you want to purge the DNS cache?')) {
    await clearCache()
    await fetchData()
  }
}

onMounted(fetchData)
</script>
