<template>
  <div>
    <h2 class="text-2xl font-bold mb-6">System Overview</h2>

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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useApi } from '../composables/useApi'
import type { AstrolabedStatusResponse, DnsHourlyEventSummary } from '../types/api'
import type { StackedBarItem, StackedBarSeries } from '../types/types'

const { getDnsHourlyEventSummary } = useApi()
const hourDnsData = ref<DnsHourlyEventSummary[] | null>(null)

const dnsBarSeries: StackedBarSeries[] = [
  { id: 'blocked', label: 'Blocked', color: '#3b82f6' },
  { id: 'allowed', label: 'Allowed', color: '#10b981' },
]

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
  try {
    hourDnsData.value = await getDnsHourlyEventSummary()
  } catch (e) {
    console.error(e)
  }
}

onMounted(fetchData)
</script>
