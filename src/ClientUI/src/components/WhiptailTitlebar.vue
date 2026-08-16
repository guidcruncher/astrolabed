<script setup lang="ts">
interface Props {
    title?: string
}

withDefaults(defineProps<Props>(), {
    title: '',
})
</script>

<template>
    <div class="wt-titlebar">
        <div class="wt-titlebar-text">
            <slot>{{ title }}</slot>
        </div>
        <div v-if="$slots.actions" class="wt-titlebar-actions">
            <slot name="actions"></slot>
        </div>
    </div>
</template>

<style scoped>
.wt-titlebar {
    background-color: #005f87;
    color: #ffffff;
    padding: 4px 8px;
    font-weight: bold;
    border-bottom: 1px solid #000000;
    display: flex;
    align-items: center;
    justify-content: space-between;
    user-select: none;
}

.wt-titlebar-text {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.wt-titlebar-actions {
    display: flex;
    align-items: center;
    gap: 4px;
    margin-left: 12px;
}

/* Deep selector to ensure header buttons fit snugly inside titlebar */
.wt-titlebar-actions :deep(.wt-btn) {
    min-width: auto;
    padding: 1px 6px;
    font-size: 0.9rem;
    line-height: 1;
}
</style>
