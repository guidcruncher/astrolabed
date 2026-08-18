<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import type { WhiptailOption } from './types'

// Double model binding: raw text input & optional selected value
const textValue = defineModel<string | number>({ default: '' })

interface Props {
    options: WhiptailOption[]
    placeholder?: string
    disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    placeholder: '< Type or select option >',
    disabled: false,
})

const emit = defineEmits<{
    (e: 'select', option: WhiptailOption): void
    (e: 'change', value: string | number): void
}>()

const isOpen = ref(false)
const highlightedIndex = ref(-1)
const comboboxRef = ref<HTMLElement | null>(null)

// Automatically sync initial value to option label if a value ID was passed initially
watch(
    [() => props.options, () => textValue.value],
    () => {
        if (textValue.value !== null && textValue.value !== undefined && props.options.length > 0) {
            const matchedOption = props.options.find(
                (opt) =>
                    String(opt.value) === String(textValue.value) ||
                    opt.label === String(textValue.value),
            )
            if (matchedOption && textValue.value !== matchedOption.label) {
                textValue.value = matchedOption.label
            }
        }
    },
    { immediate: true },
)

// Filter options dynamically based on typed text
const filteredOptions = computed(() => {
    if (!textValue.value) return props.options
    const query = String(textValue.value).toLowerCase()
    return props.options.filter(
        (opt) =>
            opt.label.toLowerCase().includes(query) ||
            String(opt.value).toLowerCase().includes(query),
    )
})

const onInput = (event: Event): void => {
    const val = (event.target as HTMLInputElement).value
    textValue.value = val
    isOpen.value = true
    highlightedIndex.value = 0
    emit('change', val)
}

const selectOption = (option: WhiptailOption): void => {
    textValue.value = option.label
    isOpen.value = false
    highlightedIndex.value = -1
    emit('select', option)
    emit('change', option.label)
}

const toggleDropdown = (): void => {
    if (!props.disabled) {
        isOpen.value = !isOpen.value
        if (isOpen.value) highlightedIndex.value = 0
    }
}

// Keyboard Navigation Handler
const handleKeyDown = (event: KeyboardEvent): void => {
    if (props.disabled) return

    if (event.key === 'ArrowDown') {
        event.preventDefault()
        if (!isOpen.value) {
            isOpen.value = true
            highlightedIndex.value = 0
        } else if (filteredOptions.value.length > 0) {
            highlightedIndex.value = (highlightedIndex.value + 1) % filteredOptions.value.length
        }
    } else if (event.key === 'ArrowUp') {
        event.preventDefault()
        if (isOpen.value && filteredOptions.value.length > 0) {
            highlightedIndex.value =
                (highlightedIndex.value - 1 + filteredOptions.value.length) %
                filteredOptions.value.length
        }
    } else if (event.key === 'Enter') {
        if (isOpen.value && highlightedIndex.value >= 0) {
            const selectedOption = filteredOptions.value[highlightedIndex.value]
            if (selectedOption) {
                event.preventDefault()
                selectOption(selectedOption)
            }
        }
    } else if (event.key === 'Escape') {
        isOpen.value = false
        highlightedIndex.value = -1
    }
}

const handleClickOutside = (event: MouseEvent): void => {
    if (comboboxRef.value && !comboboxRef.value.contains(event.target as Node)) {
        isOpen.value = false
        highlightedIndex.value = -1
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
    <div ref="comboboxRef" class="wt-combobox" :class="{ 'wt-disabled': disabled }">
        <div class="wt-combobox-input-wrapper">
            <input
                type="text"
                class="wt-input wt-combobox-input"
                v-model="textValue"
                :placeholder="placeholder"
                :disabled="disabled"
                @input="onInput"
                @focus="isOpen = true"
                @keydown="handleKeyDown"
            />
            <button
                type="button"
                class="wt-combobox-arrow"
                tabindex="-1"
                :disabled="disabled"
                @click="toggleDropdown"
            >
                {{ isOpen ? '▲' : '▼' }}
            </button>
        </div>

        <!-- Filtered Dropdown Popup -->
        <div v-if="isOpen && filteredOptions.length > 0" class="wt-combobox-menu">
            <div
                v-for="(item, index) in filteredOptions"
                :key="item.value"
                class="wt-combobox-item"
                :class="{
                    'wt-item-highlighted': index === highlightedIndex,
                    'wt-item-selected':
                        String(textValue) === String(item.label) ||
                        String(textValue) === String(item.value),
                }"
                @click="selectOption(item)"
                @mouseenter="highlightedIndex = index"
            >
                <span class="wt-item-indicator">
                    {{
                        String(textValue) === String(item.label) ||
                        String(textValue) === String(item.value)
                            ? '*'
                            : ' '
                    }}
                </span>
                <span class="wt-item-label">{{ item.label }}</span>
            </div>
        </div>
    </div>
</template>

<style scoped>
.wt-combobox {
    position: relative;
    width: 100%;
    font-family: inherit;
    user-select: none;
}

.wt-combobox-input-wrapper {
    display: flex;
    align-items: center;
    position: relative;
}

.wt-combobox-input {
    width: 100%;
    padding-right: 30px;
}

.wt-combobox-arrow {
    position: absolute;
    right: 6px;
    background: transparent;
    border: none;
    color: #ffff00;
    cursor: pointer;
    font-size: 0.85rem;
    padding: 0 4px;
}

.wt-combobox-menu {
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    z-index: 50;
    background-color: #000;
    border: 1px solid #ffff00;
    border-top: none;
    max-height: 180px;
    overflow-y: auto;
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.8);
}

.wt-combobox-item {
    display: flex;
    align-items: center;
    padding: 6px 10px;
    color: #e0e0e0;
    cursor: pointer;
}

.wt-combobox-item.wt-item-highlighted {
    background-color: #333;
}

.wt-combobox-item.wt-item-selected {
    background-color: #005f87;
    color: #ffffff;
    font-weight: bold;
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
