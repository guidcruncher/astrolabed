<script setup lang="ts">
import { ref, nextTick, onMounted, onUnmounted } from 'vue'
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
const menuRef = ref<HTMLElement | null>(null)

// Positioning state flags
const isDropup = ref<boolean>(false)
const isRightAligned = ref<boolean>(false)

const adjustPosition = async (): Promise<void> => {
    if (!isOpen.value) return

    await nextTick()

    if (!dropdownRef.value || !menuRef.value) return

    const containerRect = dropdownRef.value.getBoundingClientRect()
    const menuRect = menuRef.value.getBoundingClientRect()
    const viewportWidth = window.innerWidth
    const viewportHeight = window.innerHeight

    // Check bottom overflow: flip upward if space below is insufficient but top has room
    const spaceBelow = viewportHeight - containerRect.bottom
    const spaceAbove = containerRect.top
    isDropup.value = spaceBelow < menuRect.height && spaceAbove > spaceBelow

    // Check right overflow: align right edge to button if extending right clips screen
    const spaceRight = viewportWidth - containerRect.left
    isRightAligned.value = spaceRight < menuRect.width
}

const toggleDropdown = async (): Promise<void> => {
    isOpen.value = !isOpen.value
    if (isOpen.value) {
        await adjustPosition()
    }
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

const handleResizeOrScroll = (): void => {
    if (isOpen.value) {
        adjustPosition()
    }
}

onMounted(() => {
    document.addEventListener('click', handleClickOutside)
    document.addEventListener('keydown', handleKeydown)
    window.addEventListener('resize', handleResizeOrScroll)
    window.addEventListener('scroll', handleResizeOrScroll, true)
})

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
    document.removeEventListener('keydown', handleKeydown)
    window.removeEventListener('resize', handleResizeOrScroll)
    window.removeEventListener('scroll', handleResizeOrScroll, true)
})
</script>

<template>
    <div ref="dropdownRef" class="whiptail-dropdown-container">
        <WhiptailButton :aria-expanded="isOpen" aria-haspopup="true" @click="toggleDropdown">
            <span>{{ props.buttonLabel }}</span>
            <span class="whiptail-icon-arrow" :class="{ 'whiptail-icon-arrow-up': isOpen }">▼</span>
        </WhiptailButton>

        <transition name="whiptail-fade">
            <ul
                v-if="isOpen"
                ref="menuRef"
                class="whiptail-dropdown-menu"
                :class="{ 'is-dropup': isDropup, 'is-right-aligned': isRightAligned }"
                role="menu"
            >
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
    padding: 0.5rem 0;
    list-style: none;
    min-width: 10rem;
    max-height: 80vh;
    overflow-y: auto;

    /* Ensure solid non-transparent background and elevation */
    background-color: var(--whiptail-bg, #ffffff);
    color: var(--whiptail-text, #212529);
    border: 1px solid var(--whiptail-border, #cccccc);
    border-radius: 0.25rem;
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
}

/* Off-screen collision adjustments */
.whiptail-dropdown-menu.is-dropup {
    top: auto;
    bottom: 100%;
    margin-top: 0;
    margin-bottom: 0.25rem;
}

.whiptail-dropdown-menu.is-right-aligned {
    left: auto;
    right: 0;
}

.whiptail-icon-arrow {
    display: inline-block;
    margin-left: 0.5rem;
    transition: transform 0.2s ease;
}

.whiptail-icon-arrow-up {
    transform: rotate(180deg);
}

.whiptail-dropdown-item {
    padding: 0.5rem 1rem;
    cursor: pointer;
    user-select: none;
}

.whiptail-dropdown-item:hover,
.whiptail-dropdown-item:focus {
    background-color: var(--whiptail-hover-bg, #f8f9fa);
    outline: none;
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
    border-top: 1px solid var(--whiptail-border, #cccccc);
    opacity: 0.5;
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
