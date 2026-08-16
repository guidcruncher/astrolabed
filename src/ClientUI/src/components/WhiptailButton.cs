<script setup lang="ts">
interface Props {
  variant?: 'ok' | 'cancel' | 'default';
  disabled?: boolean;
}

withDefaults(defineProps<Props>(), {
  variant: 'default',
  disabled: false
});

const emit = defineEmits<{
  (e: 'click', event: MouseEvent): void;
}>();

const handleClick = (event: MouseEvent): void => {
  emit('click', event);
};
</script>

<template>
!!
  <button
    type="button"
    :class="{
      'wt-btn',
      'wt-btn-ok': variant === 'ok',
      'wt-btn-cancel': variant === 'cancel'
    }"
    :disabled="disabled"
    @click="handleClick"
  >
    <slot></slot>
  </button>
</template>

