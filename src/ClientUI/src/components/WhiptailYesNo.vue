<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue';

interface Props {
  title?: string;
  message: string;
  yesText?: string;
  noText?: string;
}

withDefaults(defineProps<Props>(), {
  title: 'Yes / No',
  yesText: 'Yes',
  noText: 'No'
});

const emit = defineEmits<{
  (e: 'yes'): void;
  (e: 'no'): void;
}>();
</script>

<template>
  <WhiptailDialog 
    :title="title" 
    :ok-text="yesText" 
    :cancel-text="noText" 
    :show-cancel="true"
    @ok="emit('yes')"
    @cancel="emit('no')"
  >
    <div class="wt-message">
      {{ message }}
    </div>
  </WhiptailDialog>
</template>
