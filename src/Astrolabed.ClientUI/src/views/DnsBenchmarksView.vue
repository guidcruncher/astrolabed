<!-- File: DnsBenchmarksView.vue -->
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type DnsBenchmark } from '../types/api'
import { useApi } from '../composables/useApi'

// Access the getDnsBenchmarks method from the API composable
const { getDnsBenchmarks } = useApi()

const benchmarks = ref<DnsBenchmark[]>([])
const isLoading = ref<boolean>(true)
const errorMessage = ref<string | null>(null)

/**
 * Fetches DNS benchmark rankings from the composable on component mount.
 */
const fetchBenchmarks = async (): Promise<void> => {
  isLoading.value = true
  errorMessage.value = null

  try {
    const data = await getDnsBenchmarks()
    benchmarks.value = data
  } catch (err) {
    errorMessage.value =
      err instanceof Error ? err.message : 'Failed to load DNS benchmark rankings.'
  } finally {
    isLoading.value = false
  }
}

/**
 * Returns Tailwind CSS badge classes based on latency speed thresholds.
 */
const getLatencyBadgeClass = (latencyMs: number): string => {
  if (latencyMs <= 15)
    return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300'
  if (latencyMs <= 50) return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300'
  return 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300'
}

/**
 * Returns Tailwind CSS badge classes based on packet loss percentage.
 */
const getPacketLossBadgeClass = (lossPercentage: number): string => {
  if (lossPercentage === 0)
    return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300'
  if (lossPercentage <= 5)
    return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300'
  return 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300'
}

onMounted(() => {
  fetchBenchmarks()
})
</script>

<template>
  <div class="max-w-6xl mx-auto p-6 space-y-6">
    <!-- Header Section -->
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
          Public DNS Server Rankings
        </h1>
        <p class="text-sm text-slate-500 dark:text-slate-400">
          Ranked DNS provider metrics based on average response latency and reliability.
        </p>
      </div>

      <button
        @click="fetchBenchmarks"
        :disabled="isLoading"
        class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors shadow-sm"
      >
        Refresh Rankings
      </button>
    </div>

    <!-- Loading State -->
    <div
      v-if="isLoading"
      class="flex flex-col items-center justify-center p-12 bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm space-y-3"
    >
      <div
        class="w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"
      ></div>
      <p class="text-sm font-medium text-slate-600 dark:text-slate-300">
        Fetching latest DNS benchmarks...
      </p>
    </div>

    <!-- Error State -->
    <div
      v-else-if="errorMessage"
      class="p-4 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-xl flex justify-between items-center"
    >
      <div class="flex items-center space-x-3">
        <span class="font-medium text-rose-800 dark:text-rose-200">{{ errorMessage }}</span>
      </div>
      <button
        @click="fetchBenchmarks"
        class="text-sm font-semibold text-rose-700 hover:text-rose-900 dark:text-rose-300 dark:hover:text-rose-100 underline"
      >
        Try Again
      </button>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="benchmarks.length === 0"
      class="p-12 text-center bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700"
    >
      <p class="text-slate-500 dark:text-slate-400">No DNS benchmark results available.</p>
    </div>

    <!-- Results Table -->
    <div
      v-else
      class="overflow-x-auto bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm"
    >
      <table class="w-full text-left border-collapse">
        <thead>
          <tr
            class="bg-slate-50 dark:bg-slate-700/50 border-b border-slate-200 dark:border-slate-700 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider"
          >
            <th class="py-3.5 px-4 text-center">Rank</th>
            <th class="py-3.5 px-4">DNS Provider</th>
            <th class="py-3.5 px-4 text-right">Avg Latency</th>
            <th class="py-3.5 px-4 text-right">Min Latency</th>
            <th class="py-3.5 px-4 text-right">Max Latency</th>
            <th class="py-3.5 px-4 text-center">Packet Loss</th>
            <th class="py-3.5 px-4 text-center">Endpoints</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-200 dark:divide-slate-700 text-sm">
          <tr
            v-for="server in benchmarks"
            :key="server.serverName"
            class="hover:bg-slate-50/50 dark:hover:bg-slate-700/30 transition-colors"
          >
            <!-- Rank Column -->
            <td class="py-3.5 px-4 text-center font-bold">
              <span
                class="inline-flex items-center justify-center w-7 h-7 rounded-full text-xs"
                :class="{
                  'bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-300 font-extrabold':
                    server.rank === 1,
                  'bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-300':
                    server.rank === 2,
                  'bg-amber-700/20 text-amber-900 dark:text-amber-400': server.rank === 3,
                  'text-slate-500 dark:text-slate-400': server.rank > 3,
                }"
              >
                #{{ server.rank }}
              </span>
            </td>

            <!-- Server Name -->
            <td class="py-3.5 px-4 font-semibold text-slate-900 dark:text-white">
              {{ server.serverName }}
            </td>

            <!-- Combined Average Latency -->
            <td class="py-3.5 px-4 text-right font-mono">
              <span
                class="inline-block px-2 py-0.5 rounded text-xs font-semibold"
                :class="getLatencyBadgeClass(server.combinedAverageLatencyMs)"
              >
                {{ server.combinedAverageLatencyMs.toFixed(2) }} ms
              </span>
            </td>

            <!-- Min Latency -->
            <td class="py-3.5 px-4 text-right font-mono text-slate-600 dark:text-slate-300">
              {{ server.minLatencyMs.toFixed(2) }} ms
            </td>

            <!-- Max Latency -->
            <td class="py-3.5 px-4 text-right font-mono text-slate-600 dark:text-slate-300">
              {{ server.maxLatencyMs.toFixed(2) }} ms
            </td>

            <!-- Packet Loss Percentage -->
            <td class="py-3.5 px-4 text-center font-mono">
              <span
                class="inline-block px-2 py-0.5 rounded text-xs font-semibold"
                :class="getPacketLossBadgeClass(server.combinedPacketLossPercentage)"
              >
                {{ server.combinedPacketLossPercentage }}%
              </span>
            </td>

            <!-- Endpoints Count -->
            <td class="py-3.5 px-4 text-center text-slate-600 dark:text-slate-300 font-mono">
              {{ server.endpointsCount }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
