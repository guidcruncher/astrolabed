<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { DropdownOption } from './types'

interface Props {
    buttonLabel?: string
    options: DropdownOption[]
}

const props = withDefaults(defineProps<Props>(), {
    buttonLabel: 'Select Option',
})

const emit = defineEmits<{
    (e: 'select', option: DropdownOption): void
}>()

const isOpen = ref<boolean>(false)
const dropdownRef = ref<HTMLElement | null>(null)

const toggleDropdown = (): void => {
    isOpen.value = !isOpen.value
}

const closeDropdown = (): void => {
    isOpen.value = false
}

const handleSelect = (option: DropdownOption): void => {
    if (option.disabled || option.label === '-') return
    emit('select', option)
    closeDropdown()
}

const handleClickOutside = (event: MouseEvent): void => {
    if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
        closeDropdown()
    }
}

const handleKeydown = (event: KeyboardEvent): void => {
    if (event.key === 'Escape' && isOpen.value) {
        closeDropdown()
    }
}

onMounted(() => {
    document.addEventListener('click', handleClickOutside)
    document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
    document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
    <div ref="dropdownRef" class="whiptail-dropdown-container">
        <WhiptailButton :aria-expanded="isOpen" aria-haspopup="true" @click="toggleDropdown">
            <span>{{ props.buttonLabel }}</span>
            <span class="whiptail-icon-arrow" :class="{ 'whiptail-icon-arrow-up': isOpen }">▼</span>
        </WhiptailButton>

        <transition name="whiptail-fade">
            <ul v-if="isOpen" class="whiptail-dropdown-menu" role="menu">
                <template v-for="option in props.options" :key="option.value">
                    <li
                        v-if="option.label === '-'"
                        class="whiptail-dropdown-divider"
                        role="separator"
                        aria-orientation="horizontal"
                    >
                        <hr class="whiptail-divider-line" />
                    </li>

                    <li
                        v-else
                        class="whiptail-dropdown-item"
                        :class="{ 'is-disabled': option.disabled }"
                        role="menuitem"
                        tabindex="0"
                        @click="handleSelect(option)"
                        @keydown.enter.prevent="handleSelect(option)"
                        @keydown.space.prevent="handleSelect(option)"
                    >
                        <slot name="option" :option="option">
                            {{ option.label }}
                        </slot>
                    </li>
                </template>
            </ul>
        </transition>
    </div>
</template>

<style scoped>
.whiptail-dropdown-container {
    position: relative;
    display: inline-block;
}

.whiptail-dropdown-menu {
    position: absolute;
    top: 100%;
    left: 0;
    z-index: 1000;
    margin-top: 0.25rem;
}

.whiptail-icon-arrow {
    display: inline-block;
    margin-left: 0.5rem;
    transition: transform 0.2s ease;
}

.whiptail-icon-arrow-up {
    transform: rotate(180deg);
}

.whiptail-dropdown-item.is-disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.whiptail-dropdown-divider {
    padding: 0.25rem 0;
    pointer-events: none;
    list-style: none;
}

.whiptail-divider-line {
    border: none;
    border-top: 1px solid currentColor;
    opacity: 0.25;
    margin: 0;
}

.whiptail-fade-enter-active,
.whiptail-fade-leave-active {
    transition:
        opacity 0.15s ease,
        transform 0.15s ease;
}

.whiptail-fade-enter-from,
.whiptail-fade-leave-to {
    opacity: 0;
    transform: translateY(-4px);
}
</style>
