<template>
  <aside
    :class="[
      'bg-slate-800 border-b md:border-b-0 md:border-r border-slate-700 p-4 transition-all duration-300 flex flex-col',
      isCollapsed ? 'w-full md:w-20' : 'w-full md:w-64',
    ]"
  >
    <!-- Header & Collapse Toggle -->
    <div class="flex items-center justify-between">
      <h1 v-if="!isCollapsed" class="text-xl font-bold text-sky-400 truncate">Astrolabed UI</h1>
      <button
        type="button"
        class="hidden md:flex items-center justify-center p-1.5 rounded-md text-slate-400 hover:text-white hover:bg-slate-700 transition-colors"
        :title="isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
        @click="handleToggleCollapse"
      >
        <span class="text-xs font-bold">
          <ArrowRightToLine v-if="isCollapsed" />
          <ArrowLeftToLine v-else />
        </span>
      </button>
    </div>

    <!-- API Base URL Input -->
    <div v-if="!isCollapsed" class="mt-2 text-xs text-slate-400">
      <label for="api-url" class="block">API:</label>
      <input
        id="api-url"
        v-model="apiBaseUrl"
        type="text"
        class="bg-slate-900 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs w-full mt-1 focus:outline-none focus:border-sky-500"
      />
    </div>

    <!-- Navigation -->
    <nav class="mt-6 flex md:flex-col space-x-2 md:space-x-0 md:space-y-1 overflow-x-auto">
      <router-link
        v-for="item in items"
        :key="item.to"
        :to="item.to"
        class="px-3 py-2 rounded-md text-sm font-medium hover:bg-slate-700 whitespace-nowrap transition-colors"
        :active-class="item.exact ? undefined : 'bg-sky-600 text-white'"
        :exact-active-class="item.exact ? 'bg-sky-600 text-white' : undefined"
        :title="isCollapsed ? item.label : undefined"
        @click="handleItemClick(item)"
      >
        <span v-if="isCollapsed">
          <component v-if="item.icon" :is="getIconRef(item.icon)" :size="24" :stroke-width="2" />
          <span v-else>{{ item.shortLabel }}</span>
        </span>
        <span v-else>{{ item.label }}</span>
      </router-link>
    </nav>
  </aside>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '../composables/useApi'
import { type NavItem } from '../types/types'
import { ArrowRightToLine, ArrowLeftToLine } from '@lucide/vue'
import * as icons from '@lucide/vue'

interface Props {
  items?: NavItem[]
  defaultCollapsed?: boolean
}

const STORAGE_KEY = 'astrolabed_sidebar_collapsed'

const getIconRef = (name: string) => {
  const icon = (icons as Record<string, any>)[name]

  if (!icon) {
    return null
  }

  return icon
}

const props = withDefaults(defineProps<Props>(), {
  items: () => [
    { label: 'Dashboard', shortLabel: 'DB', to: '/', exact: true },
    { label: 'DHCP Leases', shortLabel: 'DHCP', to: '/dhcp' },
    { label: 'DNS Events', shortLabel: 'DNS', to: '/dns' },
    { label: 'Network Devices', shortLabel: 'NET', to: '/network' },
  ],
  defaultCollapsed: false,
})

const emit = defineEmits<{
  (e: 'toggle-collapse', isCollapsed: boolean): void
  (e: 'item-click', item: NavItem): void
}>()

const { apiBaseUrl } = useApi()

// Rehydrate state from localStorage if available, fallback to props.defaultCollapsed
const getInitialCollapsedState = (): boolean => {
  if (typeof window === 'undefined') {
    return props.defaultCollapsed
  }

  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return saved !== null ? JSON.parse(saved) : props.defaultCollapsed
  } catch {
    return props.defaultCollapsed
  }
}

const isCollapsed = ref(getInitialCollapsedState())

function handleToggleCollapse(): void {
  isCollapsed.value = !isCollapsed.value

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(isCollapsed.value))
  } catch (error) {
    console.warn('Failed to persist sidebar collapse state to localStorage:', error)
  }

  emit('toggle-collapse', isCollapsed.value)
}

function handleItemClick(item: NavItem): void {
  emit('item-click', item)
}
</script>
