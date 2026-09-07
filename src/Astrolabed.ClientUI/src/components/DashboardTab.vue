<template>
  <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6 items-stretch">
    <div class="relative w-full flex flex-col h-full">
      <Panel title="Filters" class="h-full flex flex-col">
        <div class="flex flex-col sm:flex-row gap-4 flex-1 items-start">
          <DatePicker
            :model-value="startDate"
            label="Start"
            placeholder="YYYY-MM-DD"
            input-id="start-date"
            class="w-full sm:w-auto flex-1"
            @update:model-value="emit('update:startDate', $event)"
            @date-selected="emit('dateSelected', $event)"
          />

          <DatePicker
            :model-value="endDate"
            label="End"
            placeholder="YYYY-MM-DD"
            input-id="end-date"
            class="w-full sm:w-auto flex-1"
            @update:model-value="emit('update:endDate', $event)"
            @date-selected="emit('dateSelected', $event)"
          />
        </div>
        <div v-if="blockRate">Block Rate: {{ blockRate.blockRatePercentage }}%</div>
      </Panel>
    </div>

    <div class="relative w-full flex flex-col h-full">
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

      <Panel v-if="currentTime" title="Current Time" class="h-full">
        <div class="space-y-2.5 py-1">
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
              {{ formatUtcToLocalBrowserTime(currentTime) }}
            </span>
          </div>
        </div>
      </Panel>
    </div>
  </div>

  <div class="h-[380px]">
    <StackedBarChart
      :show-legend="false"
      :show-title="true"
      :model-value="hourlyDnsChartData"
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
      :model-value="questionTypeChartData"
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
            <span class="text-emerald-400 font-medium">{{
              activeSlice.value.toLocaleString()
            }}</span>
          </span>
          <span class="text-slate-400 text-[10px] italic">
            Share: {{ activeSlice.percentage.toFixed(2) }}% of total
          </span>
        </div>
      </template>
    </PieChart>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Globe, Clock } from '@lucide/vue'
import { useDnsTypeColor } from '../composables/useDnsTypeColor'
import { useDateUtils } from '../composables/useDateUtils'

import type { BlockRateResponse, DnsQuestionTypeSummary, DnsHourlyEventSummary } from '../types/api'
import type { PieChartItem, StackedBarItem, StackedBarSeries } from '../types/types'

const props = defineProps<{
  startDate: number
  endDate: number
  blockRate?: BlockRateResponse
  currentTime: Date | null
  loading: boolean
  hourDnsData: DnsHourlyEventSummary[] | null
  questionTypeData: DnsQuestionTypeSummary[] | null
}>()

const emit = defineEmits<{
  (e: 'update:startDate', value: number | null): void
  (e: 'update:endDate', value: number | null): void
  (e: 'dateSelected', epochSeconds: number): void
}>()

const { formatUtcToLocalBrowserTime } = useDateUtils()
const { getDnsTypeColorConfig } = useDnsTypeColor()

const dnsBarSeries: StackedBarSeries[] = [
  { id: 'blocked', label: 'Blocked', color: '#3b82f6' },
  { id: 'allowed', label: 'Allowed', color: '#10b981' },
]

const questionTypeChartData = computed<PieChartItem[]>(() => {
  if (props.questionTypeData == null) return []
  const res: PieChartItem[] = []
  const records = props.questionTypeData
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
  if (props.hourDnsData == null) return []
  const res: StackedBarItem[] = []
  const records = props.hourDnsData
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
</script>
