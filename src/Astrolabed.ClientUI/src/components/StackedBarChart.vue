<template>
  <div class="w-full h-full bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl p-6 md:p-8 flex flex-col">
    <!-- Header (Conditional) -->
    <div 
      v-if="showTitle" 
      class="mb-6 flex flex-col md:flex-row md:items-center justify-between gap-2 border-b border-slate-800 pb-4"
    >
      <div>
        <h2 class="text-xl font-bold text-white tracking-tight">{{ title }}</h2>
        <p v-if="subtitle" class="text-xs text-slate-400 mt-1">{{ subtitle }}</p>
      </div>
      <div class="self-start md:self-auto bg-slate-800 border border-slate-700 px-3 py-1 rounded-full text-xs font-semibold text-slate-300">
        Total: {{ grandTotal }}
      </div>
    </div>

    <!-- Main Content Area (Dynamic Layout based on Legend Position) -->
    <div 
      class="flex flex-1 w-full gap-8 items-center"
      :class="layoutClasses"
    >
      <!-- SVG Chart Container -->
      <div class="relative w-full h-full min-h-[240px] flex-1 flex items-center justify-center">
        <svg 
          viewBox="0 0 400 320" 
          class="w-full h-full overflow-visible drop-shadow-xl max-h-[320px]"
        >
          <!-- Grid Lines -->
          <g class="stroke-slate-800 stroke-1">
            <line x1="50" y1="20" x2="380" y2="20" stroke-dasharray="4" />
            <line x1="50" y1="82.5" x2="380" y2="82.5" stroke-dasharray="4" />
            <line x1="50" y1="145" x2="380" y2="145" stroke-dasharray="4" />
            <line x1="50" y1="207.5" x2="380" y2="207.5" stroke-dasharray="4" />
            <line x1="50" y1="270" x2="380" y2="270" />
          </g>

          <!-- Y-Axis Labels -->
          <text 
            v-for="(tick, idx) in yAxisTicks" 
            :key="idx" 
            x="42" 
            :y="tick.y" 
            text-anchor="end" 
            dominant-baseline="middle" 
            class="fill-slate-400 text-[10px] font-medium"
          >
            {{ tick.value }}
          </text>

          <!-- Stacked Bars -->
          <g v-for="bar in computedBars" :key="bar.id">
            <rect
              v-for="segment in bar.segments"
              :key="segment.seriesId"
              :x="segment.x"
              :y="segment.y"
              :width="segment.width"
              :height="segment.height"
              :fill="segment.color"
              class="transition-all duration-300 ease-out cursor-pointer hover:opacity-90 stroke-slate-900 stroke-1"
              :style="{
                transform: isSegmentActive(bar.id, segment.seriesId) ? 'scaleY(1.02)' : 'scaleY(1)',
                transformOrigin: `${segment.x + segment.width / 2}px ${segment.y + segment.height}px`
              }"
              @mouseenter="setActiveSegment(bar, segment)"
              @mouseleave="clearActiveSegment"
              @mousemove="updateTooltipPosition"
              @click="handleSegmentClick(bar, segment)"
            />

            <!-- X-Axis Bar Label -->
            <text 
              :x="bar.x + bar.width / 2" 
              y="290" 
              text-anchor="middle" 
              class="fill-slate-300 text-[11px] font-medium"
            >
              {{ bar.label }}
            </text>
          </g>
        </svg>
      </div>

      <!-- Legend (Conditional) -->
      <div 
        v-if="showLegend" 
        class="flex gap-2.5"
        :class="legendClasses"
      >
        <div
          v-for="series in seriesList"
          :key="series.id"
          class="flex items-center justify-between p-2.5 rounded-lg border transition-all duration-200 cursor-pointer"
          :class="[
            activeSeriesId === series.id 
              ? 'bg-slate-800 border-slate-700' 
              : 'border-transparent hover:bg-slate-800/50 hover:border-slate-800',
            isHorizontalLegend ? 'flex-1 min-w-[140px]' : 'w-full'
          ]"
          @mouseenter="activeSeriesId = series.id"
          @mouseleave="activeSeriesId = null"
        >
          <div class="flex items-center gap-3">
            <span 
              class="w-3 h-3 rounded-full flex-shrink-0" 
              :style="{ backgroundColor: series.color }"
            ></span>
            <span class="text-sm font-medium text-slate-300">{{ series.label }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Floating Tooltip with Slot -->
    <div 
      class="fixed opacity-0 pointer-events-none transition-opacity duration-150 ease-out bg-slate-800/90 backdrop-blur-md border border-slate-700 text-white text-xs font-medium px-3 py-1.5 rounded-lg shadow-xl z-50"
      :class="{ 'opacity-100': activeSegment !== null }"
      :style="{ left: `${tooltipPos.x}px`, top: `${tooltipPos.y}px` }"
    >
      <slot name="tooltip" :active="activeSegment">
        <template v-if="activeSegment">
          {{ activeSegment.barLabel }} ({{ activeSegment.seriesLabel }}): {{ activeSegment.value }} ({{ activeSegment.percentage.toFixed(1) }}%)
        </template>
      </slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import {
  type StackedBarItem,
  type StackedBarSeries,
  type ActiveSegmentData,
  type LegendPositon
} from '../types/types';

export interface ComputedBarSegment {
  seriesId: string | number;
  seriesLabel: string;
  color: string;
  value: number;
  percentage: number;
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface ComputedBar {
  id: string | number;
  label: string;
  x: number;
  width: number;
  total: number;
  segments: ComputedBarSegment[];
}

export interface TooltipPosition {
  x: number;
  y: number;
}


interface Props {
  modelValue: StackedBarItem[];
  seriesList: StackedBarSeries[];
  title?: string;
  subtitle?: string;
  showTitle?: boolean;
  showLegend?: boolean;
  legendPosition?: LegendPosition;
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Stacked Distribution',
  subtitle: '',
  showTitle: true,
  showLegend: true,
  legendPosition: 'right'
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: StackedBarItem[]): void;
  (e: 'bar-click', segment: ActiveSegmentData): void;
}>();

const activeSegment = ref<ActiveSegmentData | null>(null);
const activeSeriesId = ref<string | number | null>(null);
const tooltipPos = ref<TooltipPosition>({ x: 0, y: 0 });

const isHorizontalLegend = computed(() => {
  return props.legendPosition === 'top' || props.legendPosition === 'bottom';
});

const layoutClasses = computed(() => {
  if (!props.showLegend) return 'flex-col';

  switch (props.legendPosition) {
    case 'left':
      return 'flex-col md:flex-row-reverse';
    case 'top':
      return 'flex-col-reverse';
    case 'bottom':
      return 'flex-col';
    case 'right':
    default:
      return 'flex-col md:flex-row';
  }
});

const legendClasses = computed(() => {
  if (isHorizontalLegend.value) {
    return 'flex-row flex-wrap w-full justify-center';
  }
  return 'flex-col w-full md:w-64';
});

const chartTop = 20;
const chartBottom = 270;
const chartHeight = chartBottom - chartTop;
const chartLeft = 50;
const chartRight = 380;
const chartWidth = chartRight - chartLeft;

const grandTotal = computed<number>(() => {
  return props.modelValue.reduce((totalSum, bar) => {
    const barSum = Object.values(bar.values).reduce((sum, val) => sum + val, 0);
    return totalSum + barSum;
  }, 0);
});

const maxBarTotal = computed<number>(() => {
  const max = Math.max(
    ...props.modelValue.map((bar) =>
      Object.values(bar.values).reduce((sum, val) => sum + val, 0)
    ),
    1
  );
  return Math.ceil(max / 10) * 10;
});

const yAxisTicks = computed(() => {
  const max = maxBarTotal.value;
  return [
    { value: max, y: chartTop },
    { value: Math.round(max * 0.75), y: chartTop + chartHeight * 0.25 },
    { value: Math.round(max * 0.5), y: chartTop + chartHeight * 0.5 },
    { value: Math.round(max * 0.25), y: chartTop + chartHeight * 0.75 },
    { value: 0, y: chartBottom }
  ];
});

const computedBars = computed<ComputedBar[]>(() => {
  const count = props.modelValue.length;
  if (count === 0) return [];

  const slotWidth = chartWidth / count;
  const barWidth = Math.min(slotWidth * 0.55, 50);

  return props.modelValue.map((bar, index): ComputedBar => {
    const barX = chartLeft + index * slotWidth + (slotWidth - barWidth) / 2;
    const barTotal = Object.values(bar.values).reduce((sum, val) => sum + val, 0);

    let currentY = chartBottom;
    const segments: ComputedBarSegment[] = [];

    props.seriesList.forEach((series) => {
      const val = bar.values[series.id] || 0;
      if (val <= 0) return;

      const segHeight = (val / maxBarTotal.value) * chartHeight;
      currentY -= segHeight;

      segments.push({
        seriesId: series.id,
        seriesLabel: series.label,
        color: series.color,
        value: val,
        percentage: barTotal > 0 ? (val / barTotal) * 100 : 0,
        x: barX,
        y: currentY,
        width: barWidth,
        height: segHeight
      });
    });

    return {
      id: bar.id,
      label: bar.label,
      x: barX,
      width: barWidth,
      total: barTotal,
      segments
    };
  });
});

function getSeriesTotal(seriesId: string | number): number {
  return props.modelValue.reduce((sum, bar) => sum + (bar.values[seriesId] || 0), 0);
}

function isSegmentActive(barId: string | number, seriesId: string | number): boolean {
  if (activeSegment.value) {
    return activeSegment.value.barId === barId && activeSegment.value.seriesId === seriesId;
  }
  return activeSeriesId.value === seriesId;
}

function setActiveSegment(bar: ComputedBar, segment: ComputedBarSegment): void {
  activeSegment.value = {
    barId: bar.id,
    barLabel: bar.label,
    seriesId: segment.seriesId,
    seriesLabel: segment.seriesLabel,
    value: segment.value,
    percentage: segment.percentage,
    color: segment.color
  };
}

function clearActiveSegment(): void {
  activeSegment.value = null;
}

function updateTooltipPosition(event: MouseEvent): void {
  tooltipPos.value = {
    x: event.clientX + 12,
    y: event.clientY + 12
  };
}

function handleSegmentClick(bar: ComputedBar, segment: ComputedBarSegment): void {
  const activeData: ActiveSegmentData = {
    barId: bar.id,
    barLabel: bar.label,
    seriesId: segment.seriesId,
    seriesLabel: segment.seriesLabel,
    value: segment.value,
    percentage: segment.percentage,
    color: segment.color
  };
  emit('bar-click', activeData);
}
</script>
