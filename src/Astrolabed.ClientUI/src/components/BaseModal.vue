<template>
  <div
    v-if="modelValue"
    class="fixed inset-0 bg-black/60 flex items-center justify-center p-4 z-50"
  >
    <!-- Modal Card with viewport max-height constraint -->
    <div
      class="bg-slate-800 rounded-lg p-6 max-w-md w-full border border-slate-700 shadow-xl max-h-[calc(100vh-2rem)] flex flex-col"
    >
      <!-- Title Slot (fixed header) -->
      <div class="mb-4 flex items-center justify-between shrink-0">
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

      <!-- Body Slot (scrollable area when content exceeds modal height) -->
      <div class="mb-6 overflow-y-auto min-h-0 flex-1 pr-1">
        <slot></slot>
      </div>

      <!-- Footer / Actions Slot (fixed footer) -->
      <div class="flex justify-end space-x-2 shrink-0 pt-2">
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
