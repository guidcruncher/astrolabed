<script setup lang="ts">
interface Props {
    title: string
    isOpen?: boolean
    showOk?: boolean
    okText?: string
    showCancel?: boolean
    cancelText?: string
}

withDefaults(defineProps<Props>(), {
    isOpen: true,
    showOk: true,
    okText: 'OK',
    showCancel: false,
    cancelText: 'Cancel',
})

const emit = defineEmits<{
    (e: 'ok'): void
    (e: 'cancel'): void
    (e: 'close'): void
}>()

const handleBackdropClick = () => {
    emit('cancel')
    emit('close')
}
</script>

<template>
    <Teleport to="body">
        <div v-if="isOpen" class="wt-modal-backdrop" @click.self="handleBackdropClick">
            <div class="wt-dialog" role="dialog" aria-modal="true" :aria-label="title">
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
        </div>
    </Teleport>
</template>

<style scoped>
.wt-modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
}

.wt-dialog {
    background-color: #ffffff;
    border-radius: 8px;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
    min-width: 320px;
    max-width: 90vw;
    max-height: 90vh;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
}
</style>
