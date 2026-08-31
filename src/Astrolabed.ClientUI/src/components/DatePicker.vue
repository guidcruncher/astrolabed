<template>
  <div class="relative inline-block w-64 text-left" ref="datepickerWrapper">
    <label v-if="label" :for="inputId" class="block text-sm font-medium text-slate-300 mb-1">
      {{ label }}
    </label>
    <div class="relative">
      <input
        :id="inputId"
        type="text"
        readonly
        :placeholder="placeholder"
        :value="formattedDisplayDate"
        @click="toggleCalendar"
        class="w-full px-3.5 py-2 bg-slate-800 border border-slate-700 rounded-lg shadow-sm text-slate-100 text-sm placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 cursor-pointer"
      />
      <button
        type="button"
        @click="toggleCalendar"
        class="absolute inset-y-0 right-0 flex items-center pr-3 text-slate-400 hover:text-slate-200"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
          ></path>
        </svg>
      </button>
    </div>

    <div
      v-if="isOpen"
      class="absolute left-0 top-full z-50 mt-1.5 w-72 bg-slate-800 rounded-xl shadow-2xl border border-slate-700 p-4 transition-all duration-200"
    >
      <div class="flex items-center justify-between mb-3">
        <button
          type="button"
          @click="prevMonth"
          class="p-1 rounded-lg text-slate-300 hover:bg-slate-700 hover:text-white focus:outline-none"
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

        <span class="text-sm font-semibold text-slate-100">
          {{ monthNames[displayedMonth] }} {{ displayedYear }}
        </span>

        <button
          type="button"
          @click="nextMonth"
          class="p-1 rounded-lg text-slate-300 hover:bg-slate-700 hover:text-white focus:outline-none"
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

      <div class="grid grid-cols-7 gap-1 text-center text-xs font-semibold text-slate-400 mb-2">
        <div>Su</div>
        <div>Mo</div>
        <div>Tu</div>
        <div>We</div>
        <div>Th</div>
        <div>Fr</div>
        <div>Sa</div>
      </div>

      <div class="grid grid-cols-7 gap-1 text-center text-sm">
        <div
          v-for="day in prevMonthFillerDays"
          :key="'prev-' + day"
          class="py-1.5 text-slate-600 text-xs flex items-center justify-center"
        >
          {{ day }}
        </div>

        <button
          v-for="day in currentMonthDays"
          :key="'current-' + day"
          type="button"
          @click="selectDate(day)"
          :class="getDayClasses(day)"
        >
          {{ day }}
        </button>

        <div
          v-for="day in nextMonthFillerDays"
          :key="'next-' + day"
          class="py-1.5 text-slate-600 text-xs flex items-center justify-center"
        >
          {{ day }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'

interface Props {
  modelValue?: number | null // Epoch seconds at midnight
  label?: string
  placeholder?: string
  inputId?: string
  useUtc?: boolean // Set to true for UTC midnight, false for Local midnight
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  label: 'Select Date',
  placeholder: 'Select a date',
  inputId: 'date-input',
  useUtc: false,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: number | null): void
  (e: 'date-selected', value: number): void
}>()

const isOpen = ref<boolean>(false)
const datepickerWrapper = ref<HTMLDivElement | null>(null)

const monthNames: readonly string[] = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
]

const today: Date = new Date()
const displayedMonth = ref<number>(today.getMonth())
const displayedYear = ref<number>(today.getFullYear())

// Sync displayed calendar view if modelValue epoch changes from outside
watch(
  () => props.modelValue,
  (newEpochSeconds) => {
    if (newEpochSeconds !== null && newEpochSeconds !== undefined) {
      const date = new Date(newEpochSeconds * 1000)
      displayedMonth.value = props.useUtc ? date.getUTCMonth() : date.getMonth()
      displayedYear.value = props.useUtc ? date.getUTCFullYear() : date.getFullYear()
    }
  },
  { immediate: true }
)

// Formatted display string derived from epoch modelValue
const formattedDisplayDate = computed<string>(() => {
  if (props.modelValue === null || props.modelValue === undefined) {
    return ''
  }
  const date = new Date(props.modelValue * 1000)
  const year = props.useUtc ? date.getUTCFullYear() : date.getFullYear()
  const month = String((props.useUtc ? date.getUTCMonth() : date.getMonth()) + 1).padStart(2, '0')
  const day = String(props.useUtc ? date.getUTCDate() : date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
})

// Grid Calculations
const prevMonthFillerDays = computed<number[]>(() => {
  const firstDayIndex = new Date(displayedYear.value, displayedMonth.value, 1).getDay()
  const prevMonthTotalDays = new Date(displayedYear.value, displayedMonth.value, 0).getDate()
  const days: number[] = []

  for (let i = firstDayIndex; i > 0; i--) {
    days.push(prevMonthTotalDays - i + 1)
  }
  return days
})

const currentMonthDays = computed<number>(() => {
  return new Date(displayedYear.value, displayedMonth.value + 1, 0).getDate()
})

const nextMonthFillerDays = computed<number[]>(() => {
  const totalCells = prevMonthFillerDays.value.length + currentMonthDays.value
  const nextDays = (7 - (totalCells % 7)) % 7
  const days: number[] = []

  for (let i = 1; i <= nextDays; i++) {
    days.push(i)
  }
  return days
})

const getDayClasses = (day: number): string => {
  let isSelected = false

  if (props.modelValue !== null && props.modelValue !== undefined) {
    const selectedDate = new Date(props.modelValue * 1000)
    const selYear = props.useUtc ? selectedDate.getUTCFullYear() : selectedDate.getFullYear()
    const selMonth = props.useUtc ? selectedDate.getUTCMonth() : selectedDate.getMonth()
    const selDay = props.useUtc ? selectedDate.getUTCDate() : selectedDate.getDate()

    isSelected =
      selYear === displayedYear.value && selMonth === displayedMonth.value && selDay === day
  }

  const isToday =
    today.getFullYear() === displayedYear.value &&
    today.getMonth() === displayedMonth.value &&
    today.getDate() === day

  const base =
    'py-1.5 text-xs rounded-lg font-medium transition-colors duration-150 focus:outline-none flex items-center justify-center '

  if (isSelected) {
    return base + 'bg-indigo-500 text-white hover:bg-indigo-600'
  }
  if (isToday) {
    return (
      base +
      'bg-indigo-950/60 text-indigo-400 font-bold border border-indigo-700/60 hover:bg-indigo-900/60'
    )
  }
  return base + 'text-slate-200 hover:bg-slate-700 hover:text-white'
}

// Controls & Navigation
const prevMonth = (): void => {
  if (displayedMonth.value === 0) {
    displayedMonth.value = 11
    displayedYear.value--
  } else {
    displayedMonth.value--
  }
}

const nextMonth = (): void => {
  if (displayedMonth.value === 11) {
    displayedMonth.value = 0
    displayedYear.value++
  } else {
    displayedMonth.value++
  }
}

const toggleCalendar = (): void => {
  isOpen.value = !isOpen.value
}

const closeCalendar = (): void => {
  isOpen.value = false
}

// Selection logic explicitly resetting time to 00:00:00.000 (Midnight)
const selectDate = (day: number): void => {
  let epochSeconds: number

  if (props.useUtc) {
    // Generate UTC Midnight Epoch Seconds
    const utcTime = Date.UTC(displayedYear.value, displayedMonth.value, day, 0, 0, 0, 0)
    epochSeconds = Math.floor(utcTime / 1000)
  } else {
    // Generate Local Midnight Epoch Seconds
    const localDate = new Date(displayedYear.value, displayedMonth.value, day)
    localDate.setHours(0, 0, 0, 0)
    epochSeconds = Math.floor(localDate.getTime() / 1000)
  }

  emit('update:modelValue', epochSeconds)
  emit('date-selected', epochSeconds)

  closeCalendar()
}

const handleClickOutside = (event: MouseEvent): void => {
  if (datepickerWrapper.value && !datepickerWrapper.value.contains(event.target as Node)) {
    closeCalendar()
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>
