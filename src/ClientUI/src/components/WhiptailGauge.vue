<script setup lang="ts">
import { computed } from 'vue'

// Percentage value bounded between 0 and 100
const progress = defineModel<number>({ default: 0 })

interface Props {
    label?: string
    showPercentage?: boolean
    height?: number
}

const props = withDefaults(defineProps<Props>(), {
    label: '',
    showPercentage: true,
    height: 20,
})

const clampedProgress = computed(() => {
    const val = Number(progress.value) || 0
    return Math.min(100, Math.max(0, val))
})
</script>

<template>
    <div class="wt-gauge-wrapper">
        <div v-if="label || showPercentage" class="wt-gauge-header">
            <span class="wt-gauge-label">{{ label }}</span>
            <span v-if="showPercentage" class="wt-gauge-percent">
                {{ Math.round(clampedProgress) }}%
            </span>
        </div>

        <!-- Gauge Outer Track -->
        <div class="wt-gauge" :style="{ height: `${height}px` }">
            <!-- Active Filled Bar -->
            <div class="wt-gauge-fill" :style="{ width: `${clampedProgress}%` }"></div>

            <!-- Center ASCII Percentage Overlay -->
            <span v-if="showPercentage" class="wt-gauge-text">
                {{ Math.round(clampedProgress) }}%
            </span>
        </div>
    </div>
</template>

<style scoped>
.wt-gauge-wrapper {
    width: 100%;
    font-family: inherit;
    user-select: none;
}

.wt-gauge-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 4px;
    font-size: 0.9rem;
}

.wt-gauge-label {
    color: #ffff00;
    font-weight: bold;
}

.wt-gauge-percent {
    color: #00ff00;
    font-weight: bold;
}

.wt-gauge {
    margin-top: 4px;
    border: 1px solid #5f5f5f;
    background-color: #000;
    position: relative;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
}

.wt-gauge-fill {
    position: absolute;
    top: 0;
    left: 0;
    bottom: 0;
    background-color: #00af00;
    transition: width 0.2s ease-out;
}

.wt-gauge-text {
    position: relative;
    z-index: 2;
    color: #ffffff;
    font-weight: bold;
    font-size: 0.85rem;
    text-shadow: 1px 1px 2px #000000;
}
</style>
