<!-- File: HeuristicScoreView.vue -->
<script setup lang="ts">
import { ref, computed } from 'vue'
import { useHeuristicScan } from '../composables/useHeuristicScan'

const props = withDefaults(
  defineProps<{
    initialDomain?: string
    threshold?: number
  }>(),
  {
    initialDomain: '',
    threshold: 40.0,
  }
)

const targetDomain = ref<string>(props.initialDomain)
const { assessment, isLoading, error, scanDomain } = useHeuristicScan()

// Trigger initial scan if domain prop is passed on mount
if (props.initialDomain) {
  scanDomain(props.initialDomain)
}

const handleScan = () => {
  if (targetDomain.value) {
    scanDomain(targetDomain.value)
  }
}

// Computed threat level status
const threatLevel = computed(() => {
  if (!assessment.value) return 'none'
  if (assessment.value.totalScore === 0) return 'whitelisted'
  if (assessment.value.totalScore < props.threshold) return 'low'
  if (assessment.value.totalScore < 70) return 'medium'
  return 'high'
})

// Badge dynamic styling based on threat score
const badgeStyles = computed(() => {
  switch (threatLevel.value) {
    case 'whitelisted':
      return 'bg-emerald-50 text-emerald-700 ring-emerald-600/20 dark:bg-emerald-950/50 dark:text-emerald-400 dark:ring-emerald-500/30'
    case 'low':
      return 'bg-amber-50 text-amber-800 ring-amber-600/20 dark:bg-amber-950/50 dark:text-amber-400 dark:ring-amber-500/30'
    case 'medium':
      return 'bg-orange-50 text-orange-800 ring-orange-600/20 dark:bg-orange-950/50 dark:text-orange-400 dark:ring-orange-500/30'
    case 'high':
      return 'bg-red-50 text-red-700 ring-red-600/10 dark:bg-red-950/50 dark:text-red-400 dark:ring-red-500/30'
    default:
      return ''
  }
})

// Score bar width capped at 100%
const scoreBarWidth = computed(() => {
  if (!assessment.value) return '0%'
  return `${Math.min(Math.max(assessment.value.totalScore, 0), 100)}%`
})

// Score bar color based on risk level
const scoreBarColor = computed(() => {
  switch (threatLevel.value) {
    case 'whitelisted':
      return 'bg-emerald-500'
    case 'low':
      return 'bg-amber-500'
    case 'medium':
      return 'bg-orange-500'
    case 'high':
      return 'bg-red-500'
    default:
      return 'bg-slate-300'
  }
})

const getRuleStyle = (rule: string) => {
  const lower = rule.toLowerCase()
  if (lower.includes('exact keyword')) {
    return 'bg-red-100 text-red-800 dark:bg-red-900/60 dark:text-red-300'
  }
  if (lower.includes('partial keyword')) {
    return 'bg-amber-100 text-amber-800 dark:bg-amber-900/60 dark:text-amber-300'
  }
  if (lower.includes('entropy')) {
    return 'bg-purple-100 text-purple-800 dark:bg-purple-900/60 dark:text-purple-300'
  }
  if (lower.includes('depth') || lower.includes('numeric')) {
    return 'bg-sky-100 text-sky-800 dark:bg-sky-900/60 dark:text-sky-300'
  }
  return 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300'
}
</script>

<template>
  <div
    class="mx-auto max-w-3xl rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900"
  >
    <!-- Search Form Input -->
    <form @submit.prevent="handleScan" class="mb-6 flex gap-2">
      <input
        v-model="targetDomain"
        type="text"
        placeholder="Enter domain to scan (e.g. ad.example.com)..."
        class="w-full rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-slate-500 focus:outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100 dark:placeholder-slate-500"
        :disabled="isLoading"
      />
      <button
        type="submit"
        :disabled="isLoading || !targetDomain"
        class="inline-flex items-center rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50 dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200"
      >
        <span v-if="isLoading">Scanning...</span>
        <span v-else>Scan</span>
      </button>
    </form>

    <!-- Error State -->
    <div
      v-if="error"
      class="mb-6 rounded-lg bg-red-50 p-4 text-sm text-red-700 dark:bg-red-950/50 dark:text-red-400"
    >
      {{ error }}
    </div>

    <!-- Loading Skeleton -->
    <div v-if="isLoading" class="animate-pulse space-y-4">
      <div class="h-8 rounded bg-slate-200 dark:bg-slate-800"></div>
      <div class="h-3 rounded bg-slate-200 dark:bg-slate-800"></div>
      <div class="h-12 rounded bg-slate-200 dark:bg-slate-800"></div>
    </div>

    <!-- Assessment Results Section -->
    <div v-else-if="assessment">
      <!-- Header Section -->
      <div
        class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-4 dark:border-slate-800"
      >
        <div>
          <h2 class="text-xs font-semibold uppercase tracking-wider text-slate-400">
            Target Domain
          </h2>
          <p class="font-mono text-xl font-bold text-slate-900 dark:text-slate-100">
            {{ assessment.domain }}
          </p>
        </div>

        <!-- Result Status Badge -->
        <span
          :class="[
            'inline-flex items-center rounded-md px-3 py-1.5 text-xs font-semibold ring-1 ring-inset',
            badgeStyles,
          ]"
        >
          <template v-if="threatLevel === 'whitelisted'">
            <span>Allowed (Whitelisted)</span>
          </template>
          <template v-else>
            <span>{{ assessment.isAdDomain ? 'Blocked (Ad Domain)' : 'Allowed (Safe)' }}</span>
          </template>
          <span class="ml-1.5 border-l border-current/20 pl-1.5"
            >Score: {{ assessment.totalScore.toFixed(1) }}</span
          >
        </span>
      </div>

      <!-- Threat Score Bar -->
      <div class="mt-6">
        <div class="flex items-center justify-between text-xs font-medium text-slate-500 mb-1">
          <span>Threat Score Meter</span>
          <span>Threshold: {{ threshold.toFixed(1) }}</span>
        </div>
        <div
          class="relative h-3 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800"
        >
          <div
            class="h-full transition-all duration-500 ease-out"
            :class="scoreBarColor"
            :style="{ width: scoreBarWidth }"
          ></div>
        </div>
      </div>

      <!-- Triggered Rules Section -->
      <div class="mt-6">
        <h3 class="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-3">
          Triggered Heuristic Rules ({{ assessment.triggeredRules.length }})
        </h3>

        <div v-if="assessment.triggeredRules.length > 0" class="flex flex-wrap gap-2">
          <span
            v-for="(rule, index) in assessment.triggeredRules"
            :key="index"
            :class="['rounded-md px-2.5 py-1 text-xs font-medium', getRuleStyle(rule)]"
          >
            {{ rule }}
          </span>
        </div>

        <div
          v-else
          class="rounded-md bg-slate-50 p-3 text-xs text-slate-500 dark:bg-slate-800/50 dark:text-slate-400"
        >
          No suspicious heuristic rules were triggered for this domain.
        </div>
      </div>
    </div>
  </div>
</template>
