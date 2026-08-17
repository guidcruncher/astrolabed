<script setup lang="ts" generic="T extends Record<string, any> = Record<string, any>">
import { ref, computed, watch, onMounted, nextTick } from 'vue'
import type { WhiptailListItem } from './types'

const props = withDefaults(
    defineProps<{
        items: WhiptailListItem[]
        modelValue?: any | any[]
        multiple?: boolean
        height?: string
        disabled?: boolean
    }>(),
    {
        modelValue: undefined,
        multiple: false,
        height: '240px',
        disabled: false,
    },
)

const emit = defineEmits<{
    (e: 'update:modelValue', value: any): void
    (e: 'select', item: WhiptailListItem): void
    (e: 'submit', selectedItems: WhiptailListItem[]): void
}>()

const listRef = ref<HTMLUListElement | null>(null)
const activeIndex = ref<number>(0)

// Internal selection management to support both single and multi-select modes
const selectedValues = computed<Set<any>>(() => {
    if (props.multiple) {
        return new Set(Array.isArray(props.modelValue) ? props.modelValue : [])
    }
    return new Set(
        props.modelValue !== undefined && props.modelValue !== null ? [props.modelValue] : [],
    )
})

// Sync activeIndex bounds when items change
watch(
    () => props.items,
    (newItems) => {
        if (newItems.length === 0) {
            activeIndex.value = -1
        } else if (activeIndex.value >= newItems.length) {
            activeIndex.value = newItems.length - 1
        } else if (activeIndex.value < 0) {
            activeIndex.value = 0
        }
    },
    { immediate: true },
)

const scrollToActive = (): void => {
    nextTick(() => {
        if (!listRef.value) return
        const activeEl = listRef.value.children[activeIndex.value] as HTMLElement
        if (activeEl) {
            activeEl.scrollIntoView({ block: 'nearest' })
        }
    })
}

const setActiveIndex = (index: number): void => {
    if (props.disabled || props.items.length === 0) return
    const boundedIndex = Math.max(0, Math.min(index, props.items.length - 1))
    activeIndex.value = boundedIndex
    scrollToActive()
}

const handleClick = (index: number, item: WhiptailListItem): void => {
    setActiveIndex(index)
    toggleSelection(item)
}

const toggleSelection = (item: WhiptailListItem): void => {
    if (props.disabled || item.disabled) return

    if (props.multiple) {
        const currentSet = new Set(selectedValues.value)
        if (currentSet.has(item.value)) {
            currentSet.delete(item.value)
        } else {
            currentSet.add(item.value)
        }
        const updated = Array.from(currentSet)
        emit('update:modelValue', updated)
    } else {
        emit('update:modelValue', item.value)
    }

    emit('select', item)
}

const handleKeydown = (event: KeyboardEvent): void => {
    if (props.disabled || props.items.length === 0) return

    const currentItem = props.items[activeIndex.value]

    switch (event.key) {
        case 'ArrowDown':
            event.preventDefault()
            setActiveIndex(activeIndex.value + 1)
            break
        case 'ArrowUp':
            event.preventDefault()
            setActiveIndex(activeIndex.value - 1)
            break
        case 'Home':
            event.preventDefault()
            setActiveIndex(0)
            break
        case 'End':
            event.preventDefault()
            setActiveIndex(props.items.length - 1)
            break
        case 'PageDown':
            event.preventDefault()
            setActiveIndex(activeIndex.value + 5)
            break
        case 'PageUp':
            event.preventDefault()
            setActiveIndex(activeIndex.value - 5)
            break
        case ' ':
            event.preventDefault()
            if (currentItem) {
                toggleSelection(currentItem)
            }
            break
        case 'Enter':
            event.preventDefault()
            if (currentItem) {
                if (!selectedValues.value.has(currentItem.value)) {
                    toggleSelection(currentItem)
                }
                const selectedList = props.items.filter((i) => selectedValues.value.has(i.value))
                emit('submit', selectedList)
            }
            break
    }
}

const isSelected = (value: any): boolean => selectedValues.value.has(value)

onMounted(() => {
    if (props.items.length > 0 && activeIndex.value === -1) {
        activeIndex.value = 0
    }
})
</script>

<template>
    <div
        class="wt-list-container"
        :class="{ 'wt-disabled': disabled }"
        tabindex="0"
        @keydown="handleKeydown"
    >
        <ul ref="listRef" class="wt-list" :style="{ maxHeight: height }">
            <li
                v-for="(item, index) in items"
                :key="String(item.value)"
                class="wt-list-item"
                :class="{
                    'wt-active': index === activeIndex,
                    'wt-selected': isSelected(item.value),
                    'wt-item-disabled': item.disabled || disabled,
                }"
                @click="handleClick(index, item)"
            >
                <!-- Prefix Indicator -->
                <span class="wt-indicator">
                    <template v-if="multiple">
                        [{{ isSelected(item.value) ? '*' : ' ' }}]
                    </template>
                    <template v-else> ({{ isSelected(item.value) ? '•' : ' ' }}) </template>
                </span>

                <!-- Item Tag / Key (Optional TUI identifier) -->
                <span v-if="item.tag" class="wt-item-tag">{{ item.tag }}</span>

                <!-- Primary Label -->
                <span class="wt-item-label">
                    <slot name="label" :item="item" :index="index">
                        {{ item.label }}
                    </slot>
                </span>

                <!-- Secondary Description (Optional) -->
                <span v-if="item.description" class="wt-item-desc">
                    <slot name="description" :item="item" :index="index">
                        {{ item.description }}
                    </slot>
                </span>
            </li>

            <li v-if="items.length === 0" class="wt-list-empty">&lt; No items available &gt;</li>
        </ul>
    </div>
</template>

<style scoped>
.wt-list-container {
    border: 2px solid #00aaaa;
    background-color: #000000;
    color: #a8a8a8;
    font-family: 'Courier New', Courier, monospace;
    font-size: 14px;
    outline: none;
    box-sizing: border-box;
    user-select: none;
}

.wt-list-container:focus {
    border-color: #ffffff;
    box-shadow: 0 0 0 1px #ffffff;
}

.wt-list-container.wt-disabled {
    opacity: 0.5;
    pointer-events: none;
}

.wt-list {
    list-style: none;
    margin: 0;
    padding: 0;
    overflow-y: auto;
    scrollbar-width: thin;
    scrollbar-color: #00aaaa #000000;
}

.wt-list-item {
    display: flex;
    align-items: center;
    padding: 4px 8px;
    cursor: pointer;
    white-space: nowrap;
    gap: 8px;
}

.wt-list-item.wt-active {
    background-color: #00aaaa;
    color: #000000;
}

.wt-list-item.wt-selected {
    font-weight: bold;
}

.wt-list-item.wt-active .wt-item-tag,
.wt-list-item.wt-active .wt-indicator {
    color: #000000;
}

.wt-list-item.wt-item-disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.wt-indicator {
    color: #55ffff;
    font-weight: bold;
    flex-shrink: 0;
}

.wt-item-tag {
    color: #ffff55;
    font-weight: bold;
    min-width: 60px;
    flex-shrink: 0;
}

.wt-item-label {
    flex-grow: 1;
    overflow: hidden;
    text-overflow: ellipsis;
}

.wt-item-desc {
    color: #55ffff;
    font-size: 12px;
    margin-left: auto;
    padding-left: 12px;
    flex-shrink: 0;
}

.wt-list-item.wt-active .wt-item-desc {
    color: #000000;
}

.wt-list-empty {
    padding: 12px;
    text-align: center;
    color: #555555;
    font-style: italic;
}
</style>
