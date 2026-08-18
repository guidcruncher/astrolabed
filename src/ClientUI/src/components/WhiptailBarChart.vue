<template>
    <div class="whiptail-chart-container">
        <svg
            class="whiptail-chart-svg"
            viewBox="0 0 520 280"
            preserveAspectRatio="xMidYMid meet"
            xmlns="http://www.w3.org/2000/svg"
        >
            <!-- Overall Chart Title -->
            <text v-if="title" x="270" y="18" class="chart-title" text-anchor="middle">
                {{ title }}
            </text>

            <!-- Y-Axis Title -->
            <text
                v-if="yAxisLabel"
                x="15"
                y="125"
                class="axis-title y-title"
                text-anchor="middle"
                transform="rotate(-90 15 125)"
            >
                {{ yAxisLabel }}
            </text>

            <!-- Y-Axis Grid Lines & Tick Labels -->
            <g class="y-axis">
                <template v-for="tick in yTicks" :key="tick.value">
                    <line x1="55" :y1="tick.y" x2="495" :y2="tick.y" class="grid-line" />
                    <text x="50" :y="tick.y + 3" class="axis-label y-label" text-anchor="end">
                        {{ tick.value }}%
                    </text>
                </template>
            </g>

            <!-- Main Axes Lines -->
            <g class="axes">
                <!-- Y-Axis Line -->
                <line x1="55" y1="30" x2="55" y2="220" class="axis-line" />
                <!-- X-Axis Line -->
                <line x1="55" y1="220" x2="495" y2="220" class="axis-line" />
            </g>

            <!-- Bars -->
            <g class="bars">
                <rect
                    v-for="(val, index) in normalizedValues"
                    :key="index"
                    :x="58 + index * 18"
                    :y="220 - (val / 100) * 190"
                    width="12"
                    :height="(val / 100) * 190"
                    class="bar"
                    @click="handleBarClick(index, val)"
                >
                    <title>{{ barTitles[index] || `Bar ${index + 1}` }}: {{ val }}%</title>
                </rect>
            </g>

            <!-- X-Axis Bar Titles / Categories -->
            <g class="x-axis">
                <text
                    v-for="(_, index) in normalizedValues"
                    :key="index"
                    :x="64 + index * 18"
                    y="238"
                    class="axis-label x-label"
                    text-anchor="middle"
                >
                    {{ formattedBarTitle(index) }}
                </text>
            </g>

            <!-- X-Axis Title -->
            <text v-if="xAxisLabel" x="275" y="270" class="axis-title x-title" text-anchor="middle">
                {{ xAxisLabel }}
            </text>
        </svg>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { type BarClickPayload } from './types'

interface Props {
    values?: number[]
    barTitles?: string[]
    xAxisLabel?: string
    yAxisLabel?: string
    title?: string
}

const props = withDefaults(defineProps<Props>(), {
    values: () => [],
    barTitles: () => [],
    xAxisLabel: '',
    yAxisLabel: '',
    title: '',
})

const emit = defineEmits<{
    (e: 'barClick', payload: BarClickPayload): void
}>()

// Pad or clip values array to 24 elements clamped [0, 100]
const normalizedValues = computed(() => {
    const list = props.values.slice(0, 24)
    while (list.length < 24) {
        list.push(0)
    }
    return list.map((val) => Math.min(100, Math.max(0, val)))
})

// Format bar title for x-axis display
const formattedBarTitle = (index: number): string => {
    if (props.barTitles && props.barTitles[index] !== undefined) {
        const label = String(props.barTitles[index])
        return label.length > 3 ? label.substring(0, 2) + '…' : label
    }
    return String(index).padStart(2, '0')
}

// Calculate Y-axis tick mark coordinates (0%, 25%, 50%, 75%, 100%)
const yTicks = computed(() => {
    const steps = [0, 25, 50, 75, 100]
    return steps.map((value) => ({
        value,
        y: 220 - (value / 100) * 190,
    }))
})

// Emit the click event with bar metadata
const handleBarClick = (index: number, value: number) => {
    const barTitle = props.barTitles[index] || String(index).padStart(2, '0')
    emit('barClick', {
        index,
        value,
        title: barTitle,
    })
}
</script>

<style scoped>
.whiptail-chart-container {
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: var(--wt-bg, #000000);
    padding: 8px;
    box-sizing: border-box;
}

.whiptail-chart-svg {
    width: 100%;
    height: 100%;
    max-width: 100%;
    max-height: 100%;
    overflow: visible;
    font-family: var(--wt-font-family, 'Courier New', Courier, monospace);
}

.chart-title {
    fill: var(--wt-title-fg, #00ffff);
    font-size: 13px;
    font-weight: bold;
    letter-spacing: 1px;
    user-select: none;
}

.axis-line {
    stroke: var(--wt-border, #555555);
    stroke-width: 1.5;
    shape-rendering: crispEdges;
}

.grid-line {
    stroke: var(--wt-grid, #222222);
    stroke-width: 1;
    stroke-dasharray: 2, 2;
    shape-rendering: crispEdges;
}

.axis-label {
    fill: var(--wt-fg, #aaaaaa);
    font-size: 9px;
    user-select: none;
}

.axis-title {
    fill: var(--wt-title-fg, #00ffff);
    font-size: 11px;
    font-weight: bold;
    user-select: none;
}

.bar {
    fill: var(--wt-bar, #00aaaa);
    cursor: pointer;
    transition:
        height 0.3s ease,
        y 0.3s ease,
        fill 0.2s ease;
}

.bar:hover {
    fill: var(--wt-bar-hover, #00ffff);
}
</style>
