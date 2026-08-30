<template>
  <div
    class="w-full h-full bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl p-6 md:p-8 flex flex-col"
  >
    <!-- Header (Conditional) -->
    <div
      v-if="showTitle"
      class="mb-6 flex flex-col md:flex-row md:items-center justify-between gap-2 border-b border-slate-800 pb-4"
    >
      <div>
        <h2 class="text-xl font-bold text-white tracking-tight">{{ title }}</h2>
        <p v-if="subtitle" class="text-xs text-slate-400 mt-1">{{ subtitle }}</p>
      </div>
      <div
        class="self-start md:self-auto bg-slate-800 border border-slate-700 px-3 py-1 rounded-full text-xs font-semibold text-slate-300"
      >
        Total: {{ totalValue }}
      </div>
    </div>

    <!-- Main Content Area (Dynamic Grid Layout) -->
    <div
      class="grid gap-8 items-center flex-1 w-full"
      :class="showLegend ? 'grid-cols-1 md:grid-cols-2' : 'grid-cols-1'"
    >
      <!-- SVG Chart Container (Expands when legend is hidden) -->
      <div class="relative w-full h-full min-h-[240px] flex items-center justify-center">
        <svg
          viewBox="0 0 320 320"
          class="w-full h-full overflow-visible drop-shadow-xl"
          :class="showLegend ? 'max-h-[320px]' : 'max-h-[420px]'"
        >
          <path
            v-for="slice in computedSlices"
            :key="slice.id"
            :d="slice.pathData"
            :fill="slice.color"
            class="transition-transform duration-300 ease-out cursor-pointer hover:opacity-90 stroke-slate-900 stroke-2"
            :style="{
              transformOrigin: '160px 160px',
              transform:
                activeSliceId === slice.id
                  ? `translate(${slice.dx}px, ${slice.dy}px)`
                  : 'translate(0px, 0px)',
            }"
            @mouseenter="setActiveSlice(slice.id)"
            @mouseleave="clearActiveSlice"
            @mousemove="updateTooltipPosition"
            @click="handleSliceClick(slice)"
          />
        </svg>
      </div>

      <!-- Legend (Conditional) -->
      <div v-if="showLegend" class="flex flex-col gap-2.5">
        <div
          v-for="(item, index) in modelValue"
          :key="item.id"
          class="flex items-center justify-between p-2.5 rounded-lg border transition-all duration-200 cursor-pointer"
          :class="[
            activeSliceId === item.id
              ? 'bg-slate-800 border-slate-700'
              : 'border-transparent hover:bg-slate-800/50 hover:border-slate-800',
          ]"
          @mouseenter="setActiveSlice(item.id)"
          @mouseleave="clearActiveSlice"
          @click="handleLegendClick(item.id)"
        >
          <div class="flex items-center gap-3">
            <span
              class="w-3 h-3 rounded-full"
              :style="{ backgroundColor: getItemColor(item, index) }"
            ></span>
            <span class="text-sm font-medium text-slate-300">{{ item.label }}</span>
          </div>
          <span class="text-sm font-bold text-slate-100">{{ item.value }}</span>
        </div>
      </div>
    </div>

    <!-- Floating Tooltip with Slot -->
    <div
      class="fixed opacity-0 pointer-events-none transition-opacity duration-150 ease-out bg-slate-800/90 backdrop-blur-md border border-slate-700 text-white text-xs font-medium px-3 py-1.5 rounded-lg shadow-xl z-50"
      :class="{ 'opacity-100': activeSlice !== null }"
      :style="{ left: `${tooltipPos.x}px`, top: `${tooltipPos.y}px` }"
    >
      <slot name="tooltip" :active-slice="activeSlice">
        <template v-if="activeSlice">
          {{ activeSlice.label }}: {{ activeSlice.value }} ({{
            activeSlice.percentage.toFixed(1)
          }}%)
        </template>
      </slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { type PieChartItem, type ActiveSliceData } from '../types/types'

export interface ComputedSlice extends ActiveSliceData {
  pathData: string
  dx: number
  dy: number
}

export interface TooltipPosition {
  x: number
  y: number
}

interface Props {
  modelValue: PieChartItem[]
  title?: string
  subtitle?: string
  showTitle?: boolean
  showLegend?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Chart Distribution',
  subtitle: '',
  showTitle: true,
  showLegend: true,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: PieChartItem[]): void
  (e: 'slice-click', slice: ActiveSliceData): void
}>()

const activeSliceId = ref<string | number | null>(null)
const tooltipPos = ref<TooltipPosition>({ x: 0, y: 0 })

const cx = 160
const cy = 160
const radius = 120

const DEFAULT_SLATE_PALETTE: readonly string[] = [
  '#38bdf8', // Sky 400
  '#818cf8', // Indigo 400
  '#c084fc', // Purple 400
  '#34d399', // Emerald 400
  '#fbbf24', // Amber 400
  '#f472b6', // Pink 400
  '#2dd4bf', // Teal 400
  '#fb923c', // Orange 400
  '#a78bfa', // Violet 400
  '#f87171', // Red 400
]

function getItemColor(item: PieChartItem, index: number): string {
  if (item.color && item.color.trim() !== '') {
    return item.color
  }
  return DEFAULT_SLATE_PALETTE[index % DEFAULT_SLATE_PALETTE.length]
}

const totalValue = computed<number>(() => {
  return props.modelValue.reduce((sum: number, item: PieChartItem) => sum + item.value, 0)
})

const computedSlices = computed<ComputedSlice[]>(() => {
  const total = totalValue.value
  if (total === 0) return []

  let cumulativeAngle = -Math.PI / 2

  return props.modelValue.map((item: PieChartItem, index: number): ComputedSlice => {
    const percentage = (item.value / total) * 100
    const sliceAngle = (item.value / total) * 2 * Math.PI
    const startAngle = cumulativeAngle
    const endAngle = cumulativeAngle + sliceAngle
    cumulativeAngle = endAngle

    const x1 = cx + radius * Math.cos(startAngle)
    const y1 = cy + radius * Math.sin(startAngle)
    const x2 = cx + radius * Math.cos(endAngle)
    const y2 = cy + radius * Math.sin(endAngle)

    const largeArcFlag = sliceAngle > Math.PI ? 1 : 0
    const pathData = `M ${cx} ${cy} L ${x1} ${y1} A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2} Z`

    const midAngle = startAngle + sliceAngle / 2
    const offsetDistance = 10
    const dx = Math.cos(midAngle) * offsetDistance
    const dy = Math.sin(midAngle) * offsetDistance

    const resolvedColor = getItemColor(item, index)

    return {
      ...item,
      color: resolvedColor,
      percentage,
      pathData,
      dx,
      dy,
    }
  })
})

const activeSlice = computed<ActiveSliceData | null>(() => {
  if (activeSliceId.value === null) return null
  const slice = computedSlices.value.find((item) => item.id === activeSliceId.value)
  if (!slice) return null

  const { pathData, dx, dy, ...data } = slice
  return data
})

function setActiveSlice(id: string | number): void {
  activeSliceId.value = id
}

function clearActiveSlice(): void {
  activeSliceId.value = null
}

function updateTooltipPosition(event: MouseEvent): void {
  tooltipPos.value = {
    x: event.clientX + 12,
    y: event.clientY + 12,
  }
}

function handleSliceClick(slice: ComputedSlice): void {
  const { pathData, dx, dy, ...sliceData } = slice
  emit('slice-click', sliceData)
}

function handleLegendClick(id: string | number): void {
  const slice = computedSlices.value.find((s) => s.id === id)
  if (slice) {
    handleSliceClick(slice)
  }
}
</script>
