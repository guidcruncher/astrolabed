<script setup lang="ts">
interface Props {
  title: string;
  showOk?: boolean;
  okText?: string;
  showCancel?: boolean;
  cancelText?: string;
}

withDefaults(defineProps<Props>(), {
  showOk: true,
  okText: 'OK',
  showCancel: false,
  cancelText: 'Cancel'
});

const emit = defineEmits<{
  (e: 'ok'): void;
  (e: 'cancel'): void;
}>();
</script>

<template>
  <div class="wt-dialog">
    <div class="wt-title">{{ title }}</div>
    <div class="wt-body">
      <slot></slot>
    </div>
    <div class="wt-footer">
      <button 
        v-if="showOk" 
        type="button"
        class="wt-btn wt-btn-ok" 
        @click="emit('ok')"
      >
        {{ okText }}
      </button>
      <button 
        v-if="showCancel" 
        type="button"
        class="wt-btn wt-btn-cancel" 
        @click="emit('cancel')"
      >
        {{ cancelText }}
      </button>
    </div>
  </div>
</template>
