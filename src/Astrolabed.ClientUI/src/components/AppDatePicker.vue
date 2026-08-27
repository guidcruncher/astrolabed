<template>
  <div class="relative inline-block text-left" ref="pickerRef">
    <button
      type="button"
      @click="togglePicker"
      class="inline-flex items-center justify-between w-64 px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg shadow-sm text-sm font-medium text-slate-200 hover:bg-slate-700/80 focus:outline-none focus:ring-2 focus:ring-sky-500/50 transition-colors"
    >
      <span class="flex items-center gap-2.5">
        <svg class="w-4 h-4 text-sky-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
          ></path>
        </svg>
        <span>{{ displayDateText }}</span>
      </span>
      <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M19 9l-7 7-7-7"
        ></path>
      </svg>
    </button>

    <div
      v-if="isOpen"
      class="absolute left-0 mt-3 w-80 bg-slate-800 border border-slate-700 rounded-xl shadow-2xl z-50 overflow-hidden divide-y divide-slate-700/80"
    >
      <div class="p-4">
        <div class="flex items-center justify-between mb-3">
          <button
            type="button"
            @click="handlePrevMonth"
            class="p-1 rounded text-slate-400 hover:text-slate-100 hover:bg-slate-700 transition-colors"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M15 19l-7-7 7-7"
              ></path>
            </svg>
          </button>

          <span class="text-xs font-semibold text-slate-200 uppercase tracking-wide">
            {{ currentMonthName }} {{ currentYear }}
          </span>

          <button
            type="button"
            @click="handleNextMonth"
            class="p-1 rounded text-slate-400 hover:text-slate-100 hover:bg-slate-700 transition-colors"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 5l7 7-7 7"
              ></path>
            </svg>
          </button>
        </div>

        <div class="grid grid-cols-7 gap-1 text-center mb-1">
          <span
            v-for="day in weekDays"
            :key="day"
            class="text-[10px] font-medium text-slate-400 uppercase"
          >
            {{ day }}
          </span>
        </div>

        <div class="grid grid-cols-7 gap-1 text-center text-xs">
          <button
            v-for="dayObj in calendarDays"
            :key="dayObj.dateKey"
            type="button"
            @click="handleDayClick(dayObj.date)"
            :disabled="!dayObj.isCurrentMonth"
            :class="[
              'h-8 w-8 mx-auto flex items-center justify-center rounded transition-colors',
              !dayObj.isCurrentMonth ? 'text-slate-600 cursor-not-allowed' : 'text-slate-200',
              dayObj.isSelected ? 'bg-sky-600 text-white font-semibold' : '',
              dayObj.isToday && !dayObj.isSelected
                ? 'border border-sky-500/50 text-sky-400 font-medium'
                : '',
              dayObj.isCurrentMonth && !dayObj.isSelected ? 'hover:bg-slate-700' : '',
            ]"
          >
            {{ dayObj.dayNumber }}
          </button>
        </div>
      </div>

      <div class="flex items-center justify-between px-4 py-3 bg-slate-900/60">
        <button
          type="button"
          @click="selectToday"
          class="text-xs font-medium text-sky-400 hover:text-sky-300 transition-colors"
        >
          Today
        </button>
        <div class="flex items-center gap-2">
          <button
            type="button"
            @click="clearDate"
            class="px-2 py-1 text-xs font-medium text-slate-400 hover:text-slate-200 transition-colors"
          >
            Clear
          </button>
          <button
            type="button"
            @click="isOpen = false"
            class="px-3 py-1 text-xs font-medium text-slate-300 bg-slate-700 hover:bg-slate-600 rounded-lg transition-colors"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue?: Date | null
  }>(),
  {
    modelValue: null,
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: Date | null): void
  (e: 'selected', value: Date | null): void
  (e: 'next', currentMonth: Date): void
  (e: 'previous', currentMonth: Date): void
}>()

const weekDays = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']

const isOpen = ref(false)
const pickerRef = ref<HTMLElement | null>(null)

const currentDate = ref(props.modelValue ? new Date(props.modelValue) : new Date())
const selectedDate = ref<Date | null>(props.modelValue)

watch(
  () => props.modelValue,
  (newVal) => {
    selectedDate.value = newVal
    if (newVal) {
      currentDate.value = new Date(newVal)
    }
  }
)

const currentMonthName = computed(() => {
  return currentDate.value.toLocaleString('default', { month: 'long' })
})

const currentYear = computed(() => {
  return currentDate.value.getFullYear()
})

const displayDateText = computed(() => {
  if (!selectedDate.value) return 'Select a date'
  return selectedDate.value.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
})

const calendarDays = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()

  const firstDayOfMonth = new Date(year, month, 1)
  const lastDayOfMonth = new Date(year, month + 1, 0)

  const startingDayOfWeek = firstDayOfMonth.getDay()
  const totalDays = lastDayOfMonth.getDate()

  const days = []
  const today = new Date()

  const prevMonthLastDay = new Date(year, month, 0).getDate()
  for (let i = startingDayOfWeek - 1; i >= 0; i--) {
    const date = new Date(year, month - 1, prevMonthLastDay - i)
    days.push(createDayObject(date, false, today))
  }

  for (let day = 1; day <= totalDays; day++) {
    const date = new Date(year, month, day)
    days.push(createDayObject(date, true, today))
  }

  const remainingCells = 42 - days.length
  for (let day = 1; day <= remainingCells; day++) {
    const date = new Date(year, month + 1, day)
    days.push(createDayObject(date, false, today))
  }

  return days
})

function createDayObject(date: Date, isCurrentMonth: boolean, today: Date) {
  const dateKey = `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
  const isSelected = selectedDate.value ? isSameDay(date, selectedDate.value) : false
  const isToday = isSameDay(date, today)

  return {
    date,
    dateKey,
    dayNumber: date.getDate(),
    isCurrentMonth,
    isSelected,
    isToday,
  }
}

function isSameDay(d1: Date, d2: Date) {
  return (
    d1.getFullYear() === d2.getFullYear() &&
    d1.getMonth() === d2.getMonth() &&
    d1.getDate() === d2.getDate()
  )
}

function togglePicker() {
  isOpen.value = !isOpen.value
}

function handlePrevMonth() {
  currentDate.value = new Date(currentDate.value.getFullYear(), currentDate.value.getMonth() - 1, 1)
  emit('previous', new Date(currentDate.value))
}

function handleNextMonth() {
  currentDate.value = new Date(currentDate.value.getFullYear(), currentDate.value.getMonth() + 1, 1)
  emit('next', new Date(currentDate.value))
}

function handleDayClick(date: Date) {
  selectedDate.value = date
  emit('update:modelValue', date)
  emit('selected', date)
  isOpen.value = false
}

function selectToday() {
  const today = new Date()
  currentDate.value = new Date(today)
  handleDayClick(today)
}

function clearDate() {
  selectedDate.value = null
  emit('update:modelValue', null)
  emit('selected', null)
  isOpen.value = false
}

function handleClickOutside(event: MouseEvent) {
  if (pickerRef.value && !pickerRef.value.contains(event.target as Node)) {
    isOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>
