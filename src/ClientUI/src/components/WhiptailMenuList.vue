<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue';
import type { WhiptailOption } from './types';

const selectedValue = defineModel<string | number | null>({ default: null });

interface Props {
  title?: string;
  items: WhiptailOption[];
  okText?: string;
}

withDefaults(defineProps<Props>(), {
  title: 'Menu List',
  okText: 'OK'
});

const emit = defineEmits<{
  (e: 'ok', selected: string | number | null): void;
}>();

const selectItem = (val: string | number): void => {
  selectedValue.value = val;
};
</script>

<template>
  <WhiptailDialog :title="title" :ok-text="okText" @ok="emit('ok', selectedValue)">
    <div class="wt-list wt-menulist">
      <div 
        v-for="item in items" 
        :key="item.value" 
        class="wt-list-item"
        :class="{ 'wt-list-item-selected': selectedValue === item.value }"
        @click="selectItem(item.value)"
      >
        {{ item.label }}
        <span class="wt-menu-arrow">→</span>
      </div>
    </div>
  </WhiptailDialog>
</template>
