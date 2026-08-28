<template>
  <div
    v-if="modelValue"
    class="fixed inset-0 bg-black/60 flex items-center justify-center p-4 z-50"
  >
    <div class="bg-slate-800 rounded-lg p-6 max-w-md w-full border border-slate-700 shadow-xl">
      <!-- Title Slot with fallback prop -->
      <div class="mb-4 flex items-center justify-between">
        <h3 class="text-lg font-bold text-white">
          <slot name="title">{{ title }}</slot>
        </h3>
        <button
          type="button"
          class="text-slate-400 hover:text-white text-sm"
          @click="handleButtonClick('close')"
        >
          <X />
        </button>
      </div>

      <!-- Body Slot -->
      <div class="mb-6">
        <slot></slot>
      </div>

      <!-- Footer / Actions Slot with default button setup -->
      <div class="flex justify-end space-x-2">
        <slot name="actions" :handle-click="handleButtonClick">
          <button
            type="button"
            class="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-sm font-medium rounded text-slate-200"
            @click="handleButtonClick('cancel')"
          >
            {{ cancelLabel }}
          </button>
          <button
            type="button"
            class="px-4 py-2 bg-sky-600 hover:bg-sky-500 text-sm font-medium rounded text-white"
            @click="handleButtonClick('confirm')"
          >
            {{ confirmLabel }}
          </button>
        </slot>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { type ModalAction } from '../types/types'
import { X } from '@lucide/vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    title?: string
    confirmLabel?: string
    cancelLabel?: string
    autoCloseOnCancel?: boolean
  }>(),
  {
    title: '',
    confirmLabel: 'Confirm',
    cancelLabel: 'Cancel',
    autoCloseOnCancel: true,
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'button-click', action: ModalAction): void
}>()

const handleButtonClick = (action: ModalAction): void => {
  emit('button-click', action)

  if (props.autoCloseOnCancel && (action === 'cancel' || action === 'close')) {
    emit('update:modelValue', false)
  }
}
</script>
