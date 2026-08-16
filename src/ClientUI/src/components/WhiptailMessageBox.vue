<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue';

interface Props {
  title?: string;
  message: string;
  okText?: string;
}

withDefaults(defineProps<Props>(), {
  title: 'Message Box',
  okText: 'OK'
});

const emit = defineEmits<{
  (e: 'ok'): void;
}>();
</script>

<template>
  <WhiptailDialog :title="title" :ok-text="okText" @ok="emit('ok')">
    <div class="wt-message">
      {{ message }}
    </div>
  </WhiptailDialog>
</template>
