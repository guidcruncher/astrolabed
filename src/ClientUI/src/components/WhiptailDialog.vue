<script setup lang="ts">
interface Props {
    title: string
    showOk?: boolean
    okText?: string
    showCancel?: boolean
    cancelText?: string
}

withDefaults(defineProps<Props>(), {
    showOk: true,
    okText: 'OK',
    showCancel: false,
    cancelText: 'Cancel',
})

const emit = defineEmits<{
    (e: 'ok'): void
    (e: 'cancel'): void
}>()
</script>

<template>
    <div class="wt-dialog">
        <div class="wt-title">{{ title }}</div>
        <div class="wt-body">
            <slot></slot>
        </div>
        <div class="wt-footer">
            <WhiptailButton v-if="showOk" variant="ok" @click="emit('ok')">
                {{ okText }}
            </WhiptailButton>

            <WhiptailButton v-if="showCancel" variant="cancel" @click="emit('cancel')">
                {{ cancelText }}
            </WhiptailButton>
        </div>
    </div>
</template>
