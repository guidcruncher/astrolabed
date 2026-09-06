<script setup lang="ts">
import { useModel } from 'vue'
import type { TabOption } from '../types/types'

const props = defineProps<{
  tabs: TabOption[]
  modelValue?: string | number
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number): void
  (e: 'click', tab: TabOption): void
}>()

const handleTabClick = (tab: TabOption) => {
  if (tab.disabled) return

  emit('update:modelValue', tab.id)
  emit('click', tab)
}
</script>

<template>
  <div class="w-full border-b border-gray-200 dark:border-gray-700">
    <nav class="-mb-px flex w-full overflow-x-auto no-scrollbar" aria-label="Tabs">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        type="button"
        :disabled="tab.disabled"
        :class="[
          'flex-1 min-w-[120px] py-4 px-1 text-center border-b-2 font-medium text-sm transition-colors duration-150 ease-in-out whitespace-nowrap focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2',
          modelValue === tab.id
            ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400 dark:border-indigo-400'
            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-600',
          tab.disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer',
        ]"
        :aria-current="modelValue === tab.id ? 'page' : undefined"
        @click="handleTabClick(tab)"
      >
        <slot name="tab" :tab="tab" :is-active="modelValue === tab.id">
          {{ tab.label }}
        </slot>
      </button>
    </nav>
  </div>
</template>

<style scoped>
/* Utility to hide scrollbar while retaining scroll capability on small screens */
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
