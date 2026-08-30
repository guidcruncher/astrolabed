<template>
  <main
    class="min-h-screen bg-slate-950 text-slate-100 flex flex-col items-center justify-center p-4 sm:p-8"
  >
    <div class="w-full max-w-4xl space-y-6">
      <div
        class="bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-xl flex flex-wrap gap-4 items-center justify-between"
      >
        <div>
          <h1 class="text-xl font-bold text-white">Interactive Chart Visualizations</h1>
          <p class="text-xs text-slate-400 mt-0.5">
            Test reactivity, custom slots, and click events across Line, Pie, and Stacked Bar
            components
          </p>
        </div>

        <div class="flex flex-wrap gap-3">
          <button
            @click="randomizeValues"
            class="px-4 py-2 text-xs font-semibold bg-blue-600 hover:bg-blue-500 active:bg-blue-700 text-white rounded-lg transition-colors shadow-sm"
          >
            Randomize Values
          </button>
          <button
            @click="addCategory"
            class="px-4 py-2 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 active:bg-emerald-700 text-white rounded-lg transition-colors shadow-sm"
          >
            + Add Data Point
          </button>
          <button
            @click="resetData"
            class="px-4 py-2 text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg transition-colors border border-slate-700"
          >
            Reset
          </button>
        </div>
      </div>

      <div class="h-[380px]">
        <LineChart
          v-model="lineData"
          title="Monthly Revenue Trend (Line)"
          :show-title="true"
          @point-click="onPointClick"
        />
      </div>

      <div class="h-[480px]">
        <PieChart
          :show-legend="false"
          :show-title="false"
          v-model="pieData"
          title="Revenue Breakdown (Pie)"
          subtitle="Q3 Performance Metrics"
          @slice-click="onSliceClick"
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

      <div class="h-[480px]">
        <StackedBarChart
          :show-legend="true"
          :show-title="false"
          v-model="barData"
          legend-position="bottom"
          :series-list="barSeries"
          title="Quarterly Sales (Stacked Bar)"
          subtitle="Regional Breakdown per Quarter"
          @bar-click="onBarClick"
        >
          <template #tooltip="{ active }">
            <div v-if="active" class="flex flex-col gap-0.5 p-0.5">
              <span class="font-bold text-white flex items-center gap-1.5">
                <span
                  class="w-2 h-2 rounded-full"
                  :style="{ backgroundColor: active.color }"
                ></span>
                {{ active.barLabel }} - {{ active.seriesLabel }}
              </span>
              <span class="text-slate-300 text-[11px]">
                Value:
                <span class="text-emerald-400 font-medium"
                  >${{ active.value.toLocaleString() }}</span
                >
              </span>
              <span class="text-slate-400 text-[10px] italic">
                Share of Bar: {{ active.percentage.toFixed(2) }}%
              </span>
            </div>
          </template>
        </StackedBarChart>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-slate-900 border border-slate-800 rounded-2xl p-4 text-xs">
          <div class="flex items-center justify-between mb-2">
            <span class="font-semibold text-slate-300">Last Clicked Point (Line):</span>
            <span v-if="!lastClickedPoint" class="text-slate-500 italic">None</span>
          </div>

          <div
            v-if="lastClickedPoint"
            class="bg-slate-950 p-3 rounded-lg border border-slate-800 flex items-center justify-between"
          >
            <span class="font-bold text-white">{{ lastClickedPoint.month }}</span>
            <span class="text-slate-400"
              >Value:
              <strong class="text-emerald-400"
                >${{ lastClickedPoint.value.toLocaleString() }}</strong
              ></span
            >
          </div>
        </div>

        <div class="bg-slate-900 border border-slate-800 rounded-2xl p-4 text-xs">
          <div class="flex items-center justify-between mb-2">
            <span class="font-semibold text-slate-300">Last Clicked Slice (Pie):</span>
            <span v-if="!lastClickedSlice" class="text-slate-500 italic">None</span>
          </div>

          <div
            v-if="lastClickedSlice"
            class="bg-slate-950 p-3 rounded-lg border border-slate-800 flex items-center justify-between"
          >
            <div class="flex items-center gap-2">
              <span
                class="w-3 h-3 rounded-full"
                :style="{ backgroundColor: lastClickedSlice.color }"
              ></span>
              <span class="font-bold text-white">{{ lastClickedSlice.label }}</span>
            </div>
            <div class="flex gap-2 text-slate-400">
              <span
                >Value: <strong class="text-slate-200">{{ lastClickedSlice.value }}</strong></span
              >
              <span
                >Share:
                <strong class="text-slate-200"
                  >{{ lastClickedSlice.percentage.toFixed(1) }}%</strong
                ></span
              >
            </div>
          </div>
        </div>

        <div class="bg-slate-900 border border-slate-800 rounded-2xl p-4 text-xs">
          <div class="flex items-center justify-between mb-2">
            <span class="font-semibold text-slate-300">Last Clicked Segment (Bar):</span>
            <span v-if="!lastClickedSegment" class="text-slate-500 italic">None</span>
          </div>

          <div
            v-if="lastClickedSegment"
            class="bg-slate-950 p-3 rounded-lg border border-slate-800 flex items-center justify-between"
          >
            <div class="flex items-center gap-2">
              <span
                class="w-3 h-3 rounded-full"
                :style="{ backgroundColor: lastClickedSegment.color }"
              ></span>
              <span class="font-bold text-white">{{ lastClickedSegment.barLabel }}</span>
            </div>
            <div class="flex gap-2 text-slate-400">
              <span
                >Value: <strong class="text-slate-200">{{ lastClickedSegment.value }}</strong></span
              >
              <span
                >Share:
                <strong class="text-slate-200"
                  >{{ lastClickedSegment.percentage.toFixed(1) }}%</strong
                ></span
              >
            </div>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import {
  type PieChartItem,
  type ActiveSliceData,
  type StackedBarItem,
  type StackedBarSeries,
  type ActiveSegmentData,
  type LineChartItem,
} from '../types/types'

// Initial Line Data
const initialLineData: LineChartItem[] = [
  { label: 'Jan', value: 32000 },
  { label: 'Feb', value: 45000 },
  { label: 'Mar', value: 42000 },
  { label: 'Apr', value: 68000 },
  { label: 'May', value: 61000 },
  { label: 'Jun', value: 85000 },
  { label: 'Jul', value: 79000 },
  { label: 'Aug', value: 95000 },
]

// Initial Pie Data
const initialPieData: PieChartItem[] = [
  { id: 'enterprise', label: 'Enterprise Subscriptions', value: 4500, color: '#3b82f6' },
  { id: 'pro', label: 'Pro Tier Plans', value: 2800, color: '#10b981' },
  { id: 'starter', label: 'Starter Tier Plans', value: 1500, color: '#8b5cf6' },
  { id: 'addons', label: 'Marketplace Addons', value: 900, color: '#f59e0b' },
]

// Initial Stacked Bar Data & Series Definition
const barSeries: StackedBarSeries[] = [
  { id: 'na', label: 'North America', color: '#3b82f6' },
  { id: 'eu', label: 'Europe', color: '#10b981' },
  { id: 'apac', label: 'Asia Pacific', color: '#8b5cf6' },
]

const initialBarData: StackedBarItem[] = [
  { id: 'q1', label: 'Q1', values: { na: 1200, eu: 800, apac: 500 } },
  { id: 'q2', label: 'Q2', values: { na: 1500, eu: 950, apac: 700 } },
  { id: 'q3', label: 'Q3', values: { na: 1800, eu: 1100, apac: 850 } },
  { id: 'q4', label: 'Q4', values: { na: 2100, eu: 1300, apac: 1000 } },
]

const lineData = ref<LineChartItem[]>([...initialLineData])
const pieData = ref<PieChartItem[]>([...initialPieData])
const barData = ref<StackedBarItem[]>([...initialBarData])

const lastClickedPoint = ref<LineChartItem | null>(null)
const lastClickedSlice = ref<ActiveSliceData | null>(null)
const lastClickedSegment = ref<ActiveSegmentData | null>(null)

const monthNames = ['Sep', 'Oct', 'Nov', 'Dec']

function randomizeValues(): void {
  // Randomize Line values
  lineData.value = lineData.value.map((item) => ({
    ...item,
    value: Math.floor(Math.random() * 70000) + 20000,
  }))

  // Randomize Pie values
  pieData.value = pieData.value.map((item) => ({
    ...item,
    value: Math.floor(Math.random() * 5000) + 500,
  }))

  // Randomize Bar values
  barData.value = barData.value.map((bar) => ({
    ...bar,
    values: {
      na: Math.floor(Math.random() * 2000) + 500,
      eu: Math.floor(Math.random() * 1500) + 400,
      apac: Math.floor(Math.random() * 1200) + 300,
    },
  }))
}

function addCategory(): void {
  // Add new point to Line Chart
  const nextMonth = monthNames[lineData.value.length - 8] || `M${lineData.value.length + 1}`
  lineData.value.push({
    month: nextMonth,
    value: Math.floor(Math.random() * 70000) + 20000,
  })

  // Add new slice to Pie Chart
  const newPieId = `custom-${Date.now()}`
  const colors = ['#ec4899', '#06b6d4', '#f97316', '#84cc16']
  const randomColor = colors[Math.floor(Math.random() * colors.length)]

  pieData.value.push({
    id: newPieId,
    label: `New Segment ${pieData.value.length + 1}`,
    value: Math.floor(Math.random() * 3000) + 1000,
    color: randomColor,
  })

  // Add new quarter to Stacked Bar Chart
  const nextQ = `Q${barData.value.length + 1}`
  barData.value.push({
    id: `q${barData.value.length + 1}`,
    label: nextQ,
    values: {
      na: Math.floor(Math.random() * 2000) + 500,
      eu: Math.floor(Math.random() * 1500) + 400,
      apac: Math.floor(Math.random() * 1200) + 300,
    },
  })
}

function resetData(): void {
  lineData.value = JSON.parse(JSON.stringify(initialLineData))
  pieData.value = JSON.parse(JSON.stringify(initialPieData))
  barData.value = JSON.parse(JSON.stringify(initialBarData))
  lastClickedPoint.value = null
  lastClickedSlice.value = null
  lastClickedSegment.value = null
}

function onPointClick(point: LineChartItem): void {
  lastClickedPoint.value = point
}

function onSliceClick(slice: ActiveSliceData): void {
  lastClickedSlice.value = slice
}

function onBarClick(segment: ActiveSegmentData): void {
  lastClickedSegment.value = segment
}
</script>
