<template>
  <div class="wt-dialog wt-tabs-container">
    <div 
      ref="tabListRef"
      class="wt-tab-bar" 
      role="tablist" 
      aria-label="Tab Navigation"
      @keydown="handleKeyDown"
    >
      <button
        v-for="tab in tabs"
        :key="tab.id"
        :ref="(el) => setTabRef(el, tab.id)"
        type="button"
        role="tab"
        :class="['wt-tab-button', { active: modelValue === tab.id }]"
        :aria-selected="modelValue === tab.id"
        :aria-controls="`tab-panel-${tab.id}`"
        :id="`tab-btn-${tab.id}`"
        :tabindex="modelValue === tab.id ? 0 : -1"
        @click="selectTab(tab.id)"
      >
        <span class="wt-tab-label">
          <span class="wt-hotkey">{{ tab.label.charAt(0) }}</span>{{ tab.label.slice(1) }}
        </span>
      </button>
    </div>

    <div class="wt-body wt-tab-body">
      <div
        v-for="tab in tabs"
        v-show="modelValue === tab.id"
        :key="tab.id"
        :id="`tab-panel-${tab.id}`"
        role="tabpanel"
        :aria-labelledby="`tab-btn-${tab.id}`"
        class="wt-tab-panel"
        tabindex="0"
      >
        <slot :name="tab.id" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, type ComponentPublicInstance } from 'vue';
import type { TabItem } from "./types";

interface Props {
  tabs: TabItem[];
  modelValue: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void;
  (e: 'change', value: string): void;
}>();

const tabListRef = ref<HTMLElement | null>(null);
const tabRefs = ref<Record<string, HTMLButtonElement>>({});

const setTabRef = (el: Element | ComponentPublicInstance | null, id: string) => {
  if (el) {
    tabRefs.value[id] = el as HTMLButtonElement;
  }
};

const selectTab = (id: string): void => {
  if (props.modelValue !== id) {
    emit('update:modelValue', id);
    emit('change', id);
  }
};

const focusTab = (id: string): void => {
  selectTab(id);
  tabRefs.value[id]?.focus();
};

const handleKeyDown = (event: KeyboardEvent): void => {
  const currentIndex = props.tabs.findIndex((tab) => tab.id === props.modelValue);
  if (currentIndex === -1) return;

  let targetIndex = -1;

  switch (event.key) {
    case 'ArrowLeft':
      targetIndex = currentIndex === 0 ? props.tabs.length - 1 : currentIndex - 1;
      break;
    case 'ArrowRight':
      targetIndex = currentIndex === props.tabs.length - 1 ? 0 : currentIndex + 1;
      break;
    case 'Home':
      targetIndex = 0;
      break;
    case 'End':
      targetIndex = props.tabs.length - 1;
      break;
    default:
      return;
  }

  event.preventDefault();
  if (targetIndex !== -1) {
    const targetTab = props.tabs[targetIndex];
    if (targetTab?.id) {
      focusTab(targetTab.id);
    }
  }
};

const handleGlobalAltHotkey = (event: KeyboardEvent): void => {
  if (!event.altKey) return;

  const key = event.key.toLowerCase();
  const matchedTab = props.tabs.find(
    (tab) => tab.label.charAt(0).toLowerCase() === key
  );

  if (matchedTab) {
    event.preventDefault();
    focusTab(matchedTab.id);
  }
};

onMounted(() => {
  window.addEventListener('keydown', handleGlobalAltHotkey);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleGlobalAltHotkey);
});
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=JetBrains+Mono:ital,wght@0,100..800;1,100..800&display=swap');

/* Dialog Container - Updated to fill container width */
.wt-dialog.wt-tabs-container {
  font-family: 'JetBrains Mono', 'Fira Code', 'Cascadia Code', 'Consolas', monospace;
  background-color: #1b1b1b;
  border: 2px solid #c0c0c0;
  box-shadow: 0 0 0 1px #000, 0 0 10px #000;
  margin: 0;
  width: 100%;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

/* Tab Bar Header */
.wt-tab-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  background-color: #000;
  padding: 6px 6px 0 6px;
  border-bottom: 1px solid #c0c0c0;
}

/* Tab Buttons */
.wt-tab-button {
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: bold;
  background-color: #3a3a3a;
  color: #e0e0e0;
  border: 1px solid #5f5f5f;
  border-bottom: none;
  padding: 6px 12px;
  cursor: pointer;
  outline: none;
  margin-bottom: -1px;
}

.wt-tab-button:hover:not(.active) {
  background-color: #4f4f4f;
  color: #fff;
}

.wt-tab-button:focus-visible {
  border-color: #ffff00;
  outline: 1px solid #ffff00;
}

.wt-tab-button.active {
  background-color: #005f87;
  color: #ffffff;
  border-color: #c0c0c0;
  border-bottom: 1px solid #005f87;
}

.wt-hotkey {
  color: #ffff00;
  text-decoration: underline;
}

.wt-tab-button.active .wt-hotkey {
  color: #00ff00;
}

/* Content Area */
.wt-tab-body {
  background-color: #1b1b1b;
  padding: 14px;
  min-height: 120px;
}

.wt-tab-panel {
  color: #e0e0e0;
  font-size: 16px;
  line-height: 1.4;
  outline: none;
}

.wt-tab-panel:focus-visible {
  border: 1px dashed #ffff00;
  padding: 4px;
}
</style>

