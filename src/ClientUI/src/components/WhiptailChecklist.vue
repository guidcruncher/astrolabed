<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue'
import type { WhiptailOption } from './types'

const selectedValues = defineModel<Array<string | number>>({ default: () => [] })

interface Props {
    title?: string
    options: WhiptailOption[]
    okText?: string
}

withDefaults(defineProps<Props>(), {
    title: 'Checklist',
    okText: 'OK',
})

const emit = defineEmits<{
    (e: 'ok', selected: Array<string | number>): void
}>()

const toggleOption = (val: string | number): void => {
    const current = [...selectedValues.value]
    const idx = current.indexOf(val)
    if (idx > -1) {
        current.splice(idx, 1)
    } else {
        current.push(val)
    }
    selectedValues.value = current
}

const isChecked = (val: string | number): boolean => selectedValues.value.includes(val)
</script>

<template>
    <WhiptailDialog :title="title" :ok-text="okText" @ok="emit('ok', selectedValues)">
        <div class="wt-list wt-checklist">
            <div
                v-for="item in options"
                :key="item.value"
                class="wt-list-item"
                @click="toggleOption(item.value)"
            >
                <div
                    class="wt-checkbox"
                    :class="{ 'wt-checkbox-checked': isChecked(item.value) }"
                ></div>
                {{ item.label }}
            </div>
        </div>
    </WhiptailDialog>
</template>
