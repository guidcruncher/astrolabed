<script setup lang="ts">
const checked = defineModel<boolean>({ default: false })

defineProps<{
    label?: string
}>()

const toggle = (): void => {
    checked.value = !checked.value
}
</script>

<template>
    <div class="wt-list-item" @click="toggle">
        <div class="wt-checkbox" :class="{ 'wt-checkbox-checked': checked }"></div>
        <template v-if="label">{{ label }}</template>
        <slot v-else></slot>
    </div>
</template>
