<template>
  <button
    :type="type"
    :disabled="disabled"
    :class="[
      'px-4 py-2 rounded-md text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2',
      disabled ? disabledClasses : variantClasses[variant],
    ]"
    @click="handleClick"
  >
    <slot />
  </button>
</template>

<script setup lang="ts">
type ButtonVariant = 'primary' | 'default' | 'okay' | 'warn' | 'danger'

interface Props {
  variant?: ButtonVariant
  disabled?: boolean
  type?: 'button' | 'submit' | 'reset'
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'default',
  disabled: false,
  type: 'button',
})

const emit = defineEmits<{
  (e: 'click', event: MouseEvent): void
}>()

const disabledClasses = 'bg-slate-300 text-slate-500 cursor-not-allowed opacity-60'

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-sky-600 hover:bg-sky-500 text-white focus:ring-sky-500',
  default: 'bg-slate-700 hover:bg-slate-600 text-white focus:ring-slate-500',
  okay: 'bg-emerald-600 hover:bg-emerald-500 text-white focus:ring-emerald-500',
  warn: 'bg-amber-600 hover:bg-amber-500 text-white focus:ring-amber-500',
  danger: 'bg-rose-600 hover:bg-rose-500 text-white focus:ring-rose-500',
}

function handleClick(event: MouseEvent): void {
  if (!props.disabled) {
    emit('click', event)
  }
}
</script>
