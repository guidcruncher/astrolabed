<script setup lang="ts">
interface Props {
    for?: string
    required?: boolean
}

defineProps<Props>()
</script>

<template>
    <label :for="for" class="wt-label">
        <slot></slot>
        <span v-if="required" class="wt-label-required">*</span>
    </label>
</template>

<style scoped>
.wt-label {
    color: #ffff00;
    font-size: 0.95rem;
    font-weight: bold;
    user-select: none;
    display: inline-block;
}

.wt-label-required {
    color: #ff0000;
    margin-left: 4px;
}
</style>
