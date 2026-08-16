<script setup lang="ts">
import type { WhiptailOption } from './types'

const selectedValue = defineModel<string | number | null>({ default: null })

defineProps<{
    options: WhiptailOption[]
}>()

const selectOption = (val: string | number): void => {
    selectedValue.value = val
}
</script>

<template>
    <div class="wt-list wt-radiolist">
        <div
            v-for="item in options"
            :key="item.value"
            class="wt-list-item"
            @click="selectOption(item.value)"
        >
            <div
                class="wt-radio"
                :class="{ 'wt-radio-checked': selectedValue === item.value }"
            ></div>
            {{ item.label }}
        </div>
    </div>
</template>
