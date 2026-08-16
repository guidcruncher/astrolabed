<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { WhiptailOption } from './types'

const selectedValue = defineModel<string | number | null>({ default: null })

interface Props {
    options: WhiptailOption[]
    placeholder?: string
    disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    placeholder: '< Select Option >',
    disabled: false,
})

const emit = defineEmits<{
    (e: 'change', value: string | number | null): void
}>()

const isOpen = ref(false)
const dropdownRef = ref<HTMLElement | null>(null)

// Find currently selected option object
const selectedOption = () => {
    return props.options.find((opt) => opt.value === selectedValue.value)
}

const toggleDropdown = (): void => {
    if (!props.disabled) {
        isOpen.value = !isOpen.value
    }
}

const selectOption = (val: string | number): void => {
    selectedValue.value = val
    isOpen.value = false
    emit('change', val)
}

// Close dropdown on outside click
const handleClickOutside = (event: MouseEvent): void => {
    if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
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

<template>
    <div ref="dropdownRef" class="wt-dropdown" :class="{ 'wt-disabled': disabled }">
        <!-- Selected Display Field -->
        <div
            class="wt-dropdown-trigger"
            :class="{ 'wt-active': isOpen }"
            tabindex="0"
            @click="toggleDropdown"
            @keydown.space.prevent="toggleDropdown"
            @keydown.enter.prevent="toggleDropdown"
            @keydown.esc="isOpen = false"
        >
            <span class="wt-dropdown-value">
                {{ selectedOption() ? selectedOption()?.label : placeholder }}
            </span>
            <span class="wt-dropdown-arrow">{{ isOpen ? '▲' : '▼' }}</span>
        </div>

        <!-- Dropdown Menu List -->
        <div v-if="isOpen" class="wt-dropdown-menu">
            <div
                v-for="item in options"
                :key="item.value"
                class="wt-dropdown-item"
                :class="{ 'wt-item-selected': selectedValue === item.value }"
                @click="selectOption(item.value)"
            >
                <span class="wt-item-indicator">
                    {{ selectedValue === item.value ? '*' : ' ' }}
                </span>
                <span class="wt-item-label">{{ item.label }}</span>
            </div>
        </div>
    </div>
</template>

<style scoped>
.wt-dropdown {
    position: relative;
    width: 100%;
    font-family: inherit;
    user-select: none;
}

.wt-dropdown-trigger {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background-color: #000;
    border: 1px solid #5f5f5f;
    color: #e0e0e0;
    padding: 6px 10px;
    cursor: pointer;
    outline: none;
}

.wt-dropdown-trigger:focus,
.wt-dropdown-trigger.wt-active {
    border-color: #ffff00;
    background-color: #111;
}

.wt-dropdown-value {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.wt-dropdown-arrow {
    color: #ffff00;
    font-size: 0.85rem;
    margin-left: 8px;
}

.wt-dropdown-menu {
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    z-index: 50;
    background-color: #000;
    border: 1px solid #ffff00;
    border-top: none;
    max-height: 200px;
    overflow-y: auto;
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.8);
}

.wt-dropdown-item {
    display: flex;
    align-items: center;
    padding: 6px 10px;
    color: #e0e0e0;
    cursor: pointer;
}

.wt-dropdown-item:hover {
    background-color: #333;
}

.wt-item-selected {
    background-color: #005f87;
    color: #ffffff;
    font-weight: bold;
}

.wt-item-selected:hover {
    background-color: #0077ab;
}

.wt-item-indicator {
    width: 16px;
    color: #ffff00;
    font-weight: bold;
}

.wt-disabled {
    opacity: 0.5;
    pointer-events: none;
}
</style>
