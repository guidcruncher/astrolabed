<template>
  <div>
    <h2 class="text-2xl font-bold mb-6">System Overview</h2>

    <div class="relative inline-block w-full">
      <!-- Green LED Indicator (Top Right) -->
      <span
        v-if="loading"
        class="absolute top-3 right-3 z-10 flex h-2.5 w-2.5"
        title="Refreshing..."
      >
        <span
          class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"
        ></span>
        <span
          class="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.8)]"
        ></span>
      </span>

      <Panel v-if="currentTime" title="Current Time">
        <div class="space-y-2.5 py-1">
          <!-- UTC Time Row -->
          <div
            class="flex items-center justify-between rounded-lg bg-slate-900/60 border border-slate-700/60 px-3.5 py-2.5 transition-colors hover:border-slate-600"
          >
            <div class="flex items-center gap-2">
              <Globe class="h-4 w-4 text-sky-400" />
              <span class="text-xs font-semibold uppercase tracking-wider text-slate-400">UTC</span>
            </div>
            <span class="font-mono text-sm font-medium text-sky-300">
              {{ currentTime.toUTCString() }}
            </span>
          </div>

          <!-- Local Time Row -->
          <div
            class="flex items-center justify-between rounded-lg bg-slate-900/60 border border-slate-700/60 px-3.5 py-2.5 transition-colors hover:border-slate-600"
          >
            <div class="flex items-center gap-2">
              <Clock class="h-4 w-4 text-indigo-400" />
              <span class="text-xs font-semibold uppercase tracking-wider text-slate-400"
                >Local</span
              >
            </div>
            <span class="font-mono text-sm font-medium text-slate-200">
              {{ currentTime.toLocaleString() }}
            </span>
          </div>
        </div>
      </Panel>
    </div>

    <div class="h-[380px]">
      <StackedBarChart
        :show-legend="false"
        :show-title="true"
        v-model="hourlyDnsChartData"
        legend-position="bottom"
        :series-list="dnsBarSeries"
        title="DNS Forwarder Activity"
      >
        <template #tooltip="{ active }">
          <div v-if="active" class="flex flex-col gap-0.5 p-0.5">
            <span class="font-bold text-white flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full" :style="{ backgroundColor: active.color }"></span>
              {{ active.barLabel }} - {{ active.seriesLabel }}
            </span>
            <span class="text-slate-300 text-[11px]">
              Value:
              <span class="text-emerald-400 font-medium">{{ active.value.toLocaleString() }}</span>
            </span>
            <span class="text-slate-400 text-[10px] italic">
              Share of Requests: {{ active.percentage.toFixed(2) }}%
            </span>
          </div>
        </template>
      </StackedBarChart>
    </div>

    <div class="h-[380px]">
      <PieChart
        :show-legend="true"
        :show-title="true"
        v-model="questionTypeChartData"
        title="DNS Question Types"
      >
        <template #tooltip="{ activeSlice }">
          <div v-if="activeSlice" class="flex flex-col gap-0.5 p-0.5">
            <span class="font-bold text-white flex items-center gap-1.5">
              <span
                class="w-2 h-2 rounded-full"
                :style="{ backgroundColor: activeSlice.color }"
              ></span>
              {{ activeSlice.label }}
            </span>
            <span class="text-slate-300 text-[11px]">
              Value:
              <span class="text-emerald-400 font-medium"
                >${{ activeSlice.value.toLocaleString() }}</span
              >
            </span>
            <span class="text-slate-400 text-[10px] italic">
              Share: {{ activeSlice.percentage.toFixed(2) }}% of total
            </span>
          </div>
        </template>
      </PieChart>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useDnsTypeColor } from '../composables/useDnsTypeColor'
import { useApi } from '../composables/useApi'
import type {
  DnsQuestionTypeSummary,
  AstrolabedStatusResponse,
  DnsHourlyEventSummary,
} from '../types/api'
import type { PieChartItem, StackedBarItem, StackedBarSeries } from '../types/types'
import { Globe, Clock } from '@lucide/vue'

const { getDnsTypeColorConfig } = useDnsTypeColor()
const { getCurrentTime, getDnsQuestionTypeSummary, getDnsHourlyEventSummary } = useApi()
const hourDnsData = ref<DnsHourlyEventSummary[] | null>(null)
const questionTypeData = ref<DnsQuestionTypeSummary[] | null>(null)
const currentTime = ref<Date | null>(null)

const loading = ref<boolean>(false)
const error = ref<string | null>(null)

let timerId: ReturnType<typeof setInterval> | null = null

const dnsBarSeries: StackedBarSeries[] = [
  { id: 'blocked', label: 'Blocked', color: '#3b82f6' },
  { id: 'allowed', label: 'Allowed', color: '#10b981' },
]

const questionTypeChartData = computed<PieChartItem[]>(() => {
  if (questionTypeData.value == null) return []
  const res: PieChartItem[] = []
  const records: DnsQuestionTypeSummary[] = questionTypeData.value
  const count = records.length

  for (let i = 0; i < count; i++) {
    const color = getDnsTypeColorConfig(records[i].questionType)
    res.push({
      id: records[i].questionType,
      label: records[i].questionType,
      value: records[i].total,
      color: color.fill,
    })
  }

  return res
})

const hourlyDnsChartData = computed<StackedBarItem[]>(() => {
  if (hourDnsData.value == null) return []
  const res: StackedBarItem[] = []
  const records: DnsHourlyEventSummary[] = hourDnsData.value || []
  const count = records.length

  for (let i = 0; i < count; i++) {
    res.push({
      id: `hour${i}`,
      label: `${records[i].eventHour}`,
      values: { blocked: records[i].blocked, allowed: records[i].allowed },
    })
  }

  return res
})

const fetchData = async (): Promise<void> => {
  if (loading.value) return

  try {
    loading.value = true
    const now = await getCurrentTime()
    currentTime.value = new Date(now)
    hourDnsData.value = await getDnsHourlyEventSummary()
    questionTypeData.value = await getDnsQuestionTypeSummary()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to fetch data'
    console.error(err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  // 1. Fetch immediately on component mount
  fetchData()

  // 2. Schedule polling every 15000ms (15 seconds)
  timerId = setInterval(fetchData, 15000)
})

onUnmounted(() => {
  if (timerId !== null) {
    clearInterval(timerId)
  }
})
</script>
