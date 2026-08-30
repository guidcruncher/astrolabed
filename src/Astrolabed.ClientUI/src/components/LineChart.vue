<template>
  <div
    ref="containerRef"
    class="w-full bg-slate-800 rounded-2xl shadow-2xl border border-slate-700/50 p-6 space-y-6 select-none"
  >
    <div v-if="showTitle" class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h2 class="text-xl font-bold text-white tracking-wide">{{ title }}</h2>
        <p class="text-sm text-slate-400">Interactive SVG Analytics Chart</p>
      </div>
    </div>

    <div class="relative w-full aspect-[21/9] min-h-[280px]">
      <svg
        class="w-full h-full overflow-visible"
        :viewBox="`0 0 ${width} ${height}`"
        preserveAspectRatio="none"
      >
        <defs>
          <linearGradient id="vue-line-gradient" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stop-color="#6366f1" />
            <stop offset="100%" stop-color="#a855f7" />
          </linearGradient>

          <linearGradient id="vue-area-gradient" x1="0%" y1="0%" x2="0%" y2="100%">
            <stop offset="0%" stop-color="#6366f1" stop-opacity="0.35" />
            <stop offset="100%" stop-color="#6366f1" stop-opacity="0.0" />
          </linearGradient>

          <filter id="vue-glow" x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="4" result="blur" />
            <feComposite in="SourceGraphic" in2="blur" operator="over" />
          </filter>
        </defs>

        <g class="stroke-slate-700/40" stroke-dasharray="4 4" stroke-width="1">
          <line :x1="paddingLeft" y1="40" :x2="width - paddingRight" y2="40" />
          <line :x1="paddingLeft" y1="100" :x2="width - paddingRight" y2="100" />
          <line :x1="paddingLeft" y1="160" :x2="width - paddingRight" y2="160" />
          <line :x1="paddingLeft" y1="220" :x2="width - paddingRight" y2="220" />
          <line
            :x1="paddingLeft"
            y1="260"
            :x2="width - paddingRight"
            y2="260"
            stroke-dasharray="0"
            class="stroke-slate-700"
          />
        </g>

        <g class="fill-slate-400 text-[11px]" text-anchor="end">
          <text :x="paddingLeft - 10" y="44">${{ formatValue(maxDataValue) }}</text>
          <text :x="paddingLeft - 10" y="104">${{ formatValue(maxDataValue * 0.75) }}</text>
          <text :x="paddingLeft - 10" y="164">${{ formatValue(maxDataValue * 0.5) }}</text>
          <text :x="paddingLeft - 10" y="224">${{ formatValue(maxDataValue * 0.25) }}</text>
          <text :x="paddingLeft - 10" y="264">$0</text>
        </g>

        <path fill="url(#vue-area-gradient)" :d="areaPath" />

        <path
          fill="none"
          stroke="url(#vue-line-gradient)"
          stroke-width="3.5"
          stroke-linecap="round"
          stroke-linejoin="round"
          filter="url(#vue-glow)"
          :d="linePath"
        />

        <line
          v-if="hoveredPoint"
          :x1="hoveredPoint.x"
          y1="40"
          :x2="hoveredPoint.x"
          y2="260"
          class="stroke-indigo-400/50 stroke-dasharray-[3_3]"
          stroke-width="1.5"
        />

        <g v-for="(pt, index) in points" :key="index">
          <text :x="pt.x" :y="height - 12" class="fill-slate-400 text-[11px]" text-anchor="middle">
            {{ pt.label }}
          </text>

          <circle
            :cx="pt.x"
            :cy="pt.y"
            :r="hoveredIndex === index ? 7 : 5"
            class="fill-slate-900 stroke-indigo-400 stroke-[3] transition-all duration-200 pointer-events-none"
            :class="{ 'stroke-purple-400': hoveredIndex === index }"
          />

          <rect
            :x="pt.x - stepWidth / 2"
            :y="paddingTop"
            :width="stepWidth"
            :height="chartHeight"
            fill="transparent"
            class="cursor-pointer"
            @mouseenter="onPointHover(pt, index)"
            @mouseleave="onPointLeave"
          />
        </g>
      </svg>

      <div
        v-if="hoveredPoint"
        class="absolute pointer-events-none transition-all duration-150 -translate-x-1/2 -translate-y-full mb-3 bg-slate-900/90 backdrop-blur-md border border-slate-700 text-white text-xs rounded-lg p-2.5 shadow-xl flex flex-col gap-0.5"
        :style="{ left: `${hoveredPoint.relativeX}px`, top: `${hoveredPoint.relativeY}px` }"
      >
        <span class="text-slate-400 font-medium">{{ hoveredPoint.label }}</span>
        <span class="text-indigo-400 font-bold text-sm"
          >${{ formatValue(hoveredPoint.value) }}</span
        >
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'

const props = defineProps({
  modelValue: {
    type: Array,
    required: true,
    default: () => [],
  },
  title: {
    type: String,
    default: 'Monthly Revenue',
  },
  showTitle: {
    type: Boolean,
    default: true,
  },
  maxValue: {
    type: Number,
    default: null,
  },
})

defineEmits(['update:modelValue'])

// Layout Dimensions
const width = 800
const height = 300
const paddingLeft = 50
const paddingRight = 30
const paddingTop = 40
const paddingBottom = 40

const chartWidth = computed(() => width - paddingLeft - paddingRight)
const chartHeight = computed(() => height - paddingTop - paddingBottom)

const containerRef = ref(null)
const containerWidth = ref(width)
const hoveredIndex = ref(null)

// Responsive observer to auto-scale SVG tooltip coordinates
let resizeObserver = null

onMounted(() => {
  if (containerRef.value) {
    resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        containerWidth.value = entry.contentRect.width
      }
    })
    resizeObserver.observe(containerRef.value)
  }
})

onUnmounted(() => {
  if (resizeObserver) resizeObserver.disconnect()
})

// Dynamic scale calculation based on model data
const maxDataValue = computed(() => {
  if (props.maxValue) return props.maxValue
  if (!props.modelValue.length) return 100000
  const max = Math.max(...props.modelValue.map((d) => d.value))
  return Math.ceil(max / 10000) * 10000 || 100000
})

const stepWidth = computed(() => {
  return props.modelValue.length > 1
    ? chartWidth.value / (props.modelValue.length - 1)
    : chartWidth.value
})

const points = computed(() => {
  if (!props.modelValue.length) return []

  return props.modelValue.map((item, index) => {
    const x = paddingLeft + (index / (props.modelValue.length - 1 || 1)) * chartWidth.value
    const y = height - paddingBottom - (item.value / maxDataValue.value) * chartHeight.value

    // Relative offset percentage for mapping tooltips inside responsive container
    const relativeX = (x / width) * containerWidth.value
    const relativeY = (y / height) * ((containerWidth.value * 9) / 21)

    return {
      x,
      y,
      relativeX,
      relativeY,
      label: item.label,
      value: item.value,
    }
  })
})

// Paths computed based on dynamic points
const linePath = computed(() => {
  const pts = points.value
  if (pts.length === 0) return ''
  let d = `M ${pts[0].x},${pts[0].y}`

  for (let i = 0; i < pts.length - 1; i++) {
    const curr = pts[i]
    const next = pts[i + 1]
    const cpX = (curr.x + next.x) / 2
    d += ` C ${cpX},${curr.y} ${cpX},${next.y} ${next.x},${next.y}`
  }
  return d
})

const areaPath = computed(() => {
  if (!linePath.value || points.value.length === 0) return ''
  const bottomY = height - paddingBottom
  const lastPoint = points.value[points.value.length - 1]
  const firstPoint = points.value[0]
  return `${linePath.value} L ${lastPoint.x},${bottomY} L ${firstPoint.x},${bottomY} Z`
})

const hoveredPoint = computed(() => {
  return hoveredIndex.value !== null ? points.value[hoveredIndex.value] : null
})

function onPointHover(pt, index) {
  hoveredIndex.value = index
}

function onPointLeave() {
  hoveredIndex.value = null
}

function formatValue(val) {
  if (val >= 1000) return `${(val / 1000).toFixed(0)}k`
  return val.toLocaleString()
}
</script>
