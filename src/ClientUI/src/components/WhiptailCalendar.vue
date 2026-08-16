<script setup lang="ts">
import { ref, computed } from 'vue'

// Selected date binding
const selectedDate = defineModel<Date>({ default: () => new Date() })

const emit = defineEmits<{
    (e: 'date-selected', date: Date): void
}>()

// Navigation state for currently viewed month/year
const currentViewDate = ref<Date>(new Date(selectedDate.value))

const currentYear = computed(() => currentViewDate.value.getFullYear())
const currentMonth = computed(() => currentViewDate.value.getMonth())

const monthNames = [
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

const dayNames = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']

// Calculate days to display in the grid
const calendarDays = computed(() => {
    const year = currentYear.value
    const month = currentMonth.value

    const firstDayOfMonth = new Date(year, month, 1).getDay()
    const daysInMonth = new Date(year, month + 1, 0).getDate()
    const daysInPrevMonth = new Date(year, month, 0).getDate()

    const days: Array<{ day: number; isCurrentMonth: boolean; date: Date }> = []

    // Previous month padding days
    for (let i = firstDayOfMonth - 1; i >= 0; i--) {
        const day = daysInPrevMonth - i
        days.push({
            day,
            isCurrentMonth: false,
            date: new Date(year, month - 1, day),
        })
    }

    // Current month days
    for (let i = 1; i <= daysInMonth; i++) {
        days.push({
            day: i,
            isCurrentMonth: true,
            date: new Date(year, month, i),
        })
    }

    // Next month padding days to complete 42 cells (6 rows x 7 days)
    const remainingCells = 42 - days.length
    for (let i = 1; i <= remainingCells; i++) {
        days.push({
            day: i,
            isCurrentMonth: false,
            date: new Date(year, month + 1, i),
        })
    }

    return days
})

const isSelected = (date: Date): boolean => {
    return (
        date.getDate() === selectedDate.value.getDate() &&
        date.getMonth() === selectedDate.value.getMonth() &&
        date.getFullYear() === selectedDate.value.getFullYear()
    )
}

const selectDate = (date: Date): void => {
    selectedDate.value = new Date(date)
    currentViewDate.value = new Date(date)
    emit('date-selected', selectedDate.value)
}

const changeMonth = (delta: number): void => {
    currentViewDate.value = new Date(currentYear.value, currentMonth.value + delta, 1)
}

const changeYear = (delta: number): void => {
    currentViewDate.value = new Date(currentYear.value + delta, currentMonth.value, 1)
}
</script>

<template>
    <div class="wt-calendar">
        <!-- Calendar Navigation Header -->
        <div class="wt-calendar-header">
            <div class="wt-nav-group">
                <WhiptailButton @click="changeYear(-1)">&lt;&lt;</WhiptailButton>
                <WhiptailButton @click="changeMonth(-1)">&lt;</WhiptailButton>
            </div>
            <span class="wt-calendar-title">
                {{ monthNames[currentMonth] }} {{ currentYear }}
            </span>
            <div class="wt-nav-group">
                <WhiptailButton @click="changeMonth(1)">&gt;</WhiptailButton>
                <WhiptailButton @click="changeYear(1)">&gt;&gt;</WhiptailButton>
            </div>
        </div>

        <!-- Calendar Grid -->
        <div class="wt-calendar-grid">
            <div v-for="day in dayNames" :key="day" class="wt-calendar-day-header">
                {{ day }}
            </div>

            <div
                v-for="(item, idx) in calendarDays"
                :key="idx"
                class="wt-calendar-cell"
                :class="{
                    'wt-cell-out': !item.isCurrentMonth,
                    'wt-cell-selected': isSelected(item.date),
                }"
                @click="selectDate(item.date)"
            >
                {{ item.day }}
            </div>
        </div>
    </div>
</template>

<style scoped>
.wt-calendar {
    user-select: none;
    background-color: #1b1b1b;
    border: 1px solid #5f5f5f;
    padding: 10px;
}

.wt-calendar-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
}

.wt-nav-group {
    display: flex;
    gap: 4px;
}

.wt-nav-group :deep(.wt-btn) {
    min-width: 36px;
    padding: 2px 6px;
}

.wt-calendar-title {
    font-weight: bold;
    color: #ffff00;
    text-align: center;
}

.wt-calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 2px;
    background-color: #5f5f5f;
    border: 1px solid #5f5f5f;
    padding: 2px;
}

.wt-calendar-day-header {
    background-color: #005f87;
    color: #fff;
    text-align: center;
    padding: 4px 0;
    font-weight: bold;
}

.wt-calendar-cell {
    background-color: #000;
    color: #e0e0e0;
    text-align: center;
    padding: 6px 0;
    cursor: pointer;
}

.wt-calendar-cell:hover {
    background-color: #333;
}

.wt-cell-out {
    color: #5f5f5f;
}

.wt-cell-selected {
    background-color: #00a000;
    color: #000;
    font-weight: bold;
}

.wt-cell-selected:hover {
    background-color: #00c000;
}
</style>
