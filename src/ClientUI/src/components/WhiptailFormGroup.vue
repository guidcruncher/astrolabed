<script setup lang="ts">
import WhiptailLabel from './WhiptailLabel.vue'

interface Props {
    label?: string
    forId?: string
    required?: boolean
    hint?: string
    error?: string
}

defineProps<Props>()
</script>

<template>
    <div class="wt-form-group">
        <WhiptailLabel v-if="label || $slots.label" :for-id="forId" :required="required">
            <slot name="label">{{ label }}</slot>
        </WhiptailLabel>

        <div class="wt-form-control">
            <slot></slot>
        </div>

        <p v-if="error" class="wt-form-error">{{ error }}</p>
        <p v-else-if="hint" class="wt-form-hint">{{ hint }}</p>
    </div>
</template>

<style scoped>
.wt-form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-bottom: 12px;
}

.wt-form-control {
    width: 100%;
}

.wt-form-hint {
    color: #8a8a8a;
    font-size: 0.85rem;
    margin: 0;
}

.wt-form-error {
    color: #ff5555;
    font-size: 0.85rem;
    margin: 0;
}
</style>
