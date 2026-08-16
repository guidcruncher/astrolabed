<template>
  <div class="whiptail-tabs-container">
    <div 
      ref="tabListRef"
      class="whiptail-tab-bar" 
      role="tablist" 
      aria-label="Tab Navigation"
      @keydown="handleKeyDown"
    >
      <button
        v-for="(tab, index) in tabs"
        :key="tab.id"
        :ref="(el) => setTabRef(el, tab.id)"
        type="button"
        role="tab"
        :class="['whiptail-tab-button', { active: modelValue === tab.id }]"
        :aria-selected="modelValue === tab.id"
        :aria-controls="`tab-panel-${tab.id}`"
        :id="`tab-btn-${tab.id}`"
        :tabindex="modelValue === tab.id ? 0 : -1"
        @click="selectTab(tab.id)"
      >
        <span class="whiptail-tab-label">
          <span class="whiptail-hotkey">{{ tab.label.charAt(0) }}</span>{{ tab.label.slice(1) }}
        </span>
      </button>
    </div>

    <div class="whiptail-tab-body">
      <div
        v-for="tab in tabs"
        v-show="modelValue === tab.id"
        :key="tab.id"
        :id="`tab-panel-${tab.id}`"
        role="tabpanel"
        :aria-labelledby="`tab-btn-${tab.id}`"
        class="whiptail-tab-panel"
        tabindex="0"
      >
        <slot :name="tab.id" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, type ComponentPublicInstance } from 'vue';
import type { TabItem } from "./types"

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
    focusTab(props.tabs[targetIndex].id);
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
/* Whiptail / Newt TUI Theme Tokens */
.whiptail-tabs-container {
  --wt-bg: #c0c0c0;
  --wt-fg: #000000;
  --wt-box-bg: #0000a8;
  --wt-active-bg: #00aaaa;
  --wt-active-fg: #ffffff;
  --wt-border-light: #ffffff;
  --wt-border-dark: #808080;
  --wt-shadow: #555555;

  font-family: 'Courier New', Courier, monospace;
  background-color: var(--wt-box-bg);
  color: var(--wt-fg);
  padding: 8px;
  border: 2px solid var(--wt-border-light);
  box-shadow: 4px 4px 0px var(--wt-shadow);
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

/* Tab Bar Layout */
.whiptail-tab-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  background-color: var(--wt-bg);
  padding: 4px 4px 0 4px;
  border-bottom: 2px solid var(--wt-border-dark);
}

/* Tab Buttons */
.whiptail-tab-button {
  font-family: inherit;
  font-size: 14px;
  font-weight: bold;
  background-color: var(--wt-bg);
  color: var(--wt-fg);
  border-top: 2px solid var(--wt-border-light);
  border-left: 2px solid var(--wt-border-light);
  border-right: 2px solid var(--wt-border-dark);
  border-bottom: none;
  padding: 4px 12px;
  cursor: pointer;
  outline: none;
  margin-bottom: -2px;
}

.whiptail-tab-button:hover {
  background-color: #d4d4d4;
}

.whiptail-tab-button:focus-visible {
  outline: 2px dotted #000000;
  outline-offset: -4px;
}

.whiptail-tab-button.active {
  background-color: var(--wt-active-bg);
  color: var(--wt-active-fg);
  border-top: 2px solid var(--wt-border-light);
  border-left: 2px solid var(--wt-border-light);
  border-right: 2px solid var(--wt-border-dark);
}

.whiptail-hotkey {
  text-decoration: underline;
}

/* Content Area */
.whiptail-tab-body {
  background-color: var(--wt-bg);
  border-left: 2px solid var(--wt-border-light);
  border-right: 2px solid var(--wt-border-dark);
  border-bottom: 2px solid var(--wt-border-dark);
  padding: 12px;
  min-height: 100px;
}

.whiptail-tab-panel {
  color: var(--wt-fg);
  font-size: 14px;
  line-height: 1.4;
  outline: none;
}

.whiptail-tab-panel:focus-visible {
  outline: 1px dashed var(--wt-fg);
  outline-offset: -2px;
}
</style>
