<template>
  <div>
    <AppToolbar />

    <TabControl v-model="activeTab" :tabs="tabs" class="mb-6" />

    <DashboardTab
      v-if="activeTab === 'dashboard'"
      v-model:start-date="startDate"
      v-model:end-date="endDate"
      :block-rate="blockRate"
      :current-time="currentTime"
      :loading="loading"
      :hour-dns-data="hourDnsData"
      :question-type-data="questionTypeData"
      @date-selected="handleDateSelected"
    />

    <DetailsTab v-else-if="activeTab === 'details'" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { type TabOption } from '../types/types'
import { useApi } from '../composables/useApi'

import type { BlockRateResponse, DnsQuestionTypeSummary, DnsHourlyEventSummary } from '../types/api'

// Tab Control Setup
const activeTab = ref<string>('dashboard')
const tabs: TabOption[] = [
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'details', label: 'Details' },
]

const { getBlockRate, getCurrentTime, getDnsQuestionTypeSummary, getDnsHourlyEventSummary } =
  useApi()

const hourDnsData = ref<DnsHourlyEventSummary[] | null>(null)
const questionTypeData = ref<DnsQuestionTypeSummary[] | null>(null)
const currentTime = ref<Date | null>(null)
const startDate = ref<number>(0)
const endDate = ref<number>(0)
const blockRate = ref<BlockRateResponse>()
const loading = ref<boolean>(false)
const error = ref<string | null>(null)

let timerId: ReturnType<typeof setInterval> | null = null

const handleDateSelected = async (_epochSeconds: number): Promise<void> => {
  await fetchData()
}

const fetchData = async (): Promise<void> => {
  if (loading.value || activeTab.value !== 'dashboard') return

  try {
    loading.value = true
    const endOfDate = (endDate.value + 86399) * 1000
    const startOfDate = startDate.value * 1000
    const now = await getCurrentTime()
    currentTime.value = new Date(now)
    blockRate.value = await getBlockRate()
    hourDnsData.value = await getDnsHourlyEventSummary(startOfDate, endOfDate)
    questionTypeData.value = await getDnsQuestionTypeSummary(startOfDate, endOfDate)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to fetch data'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const calculateDateFromNow = (days: number): number => {
  const now = new Date()
  return Math.floor(
    Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + days, 0, 0, 0, 0) / 1000
  )
}

onMounted(() => {
  startDate.value = calculateDateFromNow(-7)
  endDate.value = calculateDateFromNow(0)
  fetchData()

  timerId = setInterval(fetchData, 15000)
})

onUnmounted(() => {
  if (timerId !== null) {
    clearInterval(timerId)
  }
})
</script>
