<script setup lang="ts">
import WhiptailCheckbox from './WhiptailCheckbox.vue'
import type { WhiptailOption } from './types'

const selectedValues = defineModel<Array<string | number>>({ default: () => [] })

defineProps<{
    options: WhiptailOption[]
}>()

const isChecked = (val: string | number): boolean => {
    return selectedValues.value.includes(val)
}

const handleToggle = (val: string | number, shouldCheck: boolean): void => {
    const current = [...selectedValues.value]
    const idx = current.indexOf(val)

    if (shouldCheck && idx === -1) {
        current.push(val)
    } else if (!shouldCheck && idx > -1) {
        current.splice(idx, 1)
    }

    selectedValues.value = current
}
</script>

<template>
    <div class="wt-list wt-checklist">
        <WhiptailCheckbox
            v-for="item in options"
            :key="item.value"
            :model-value="isChecked(item.value)"
            :label="item.label"
            @update:model-value="(val) => handleToggle(item.value, val)"
        />
    </div>
</template>
