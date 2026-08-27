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
        <span class="text-xs font-bold">{{ isCollapsed ? '>' : '<' }}</span>
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
        {{ isCollapsed ? item.shortLabel : item.label }}
      </router-link>
    </nav>
  </aside>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '../composables/useApi'
import { type NavItem } from '../types/types'

interface Props {
  items?: NavItem[]
  defaultCollapsed?: boolean
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
const isCollapsed = ref(props.defaultCollapsed)

function handleToggleCollapse(): void {
  isCollapsed.value = !isCollapsed.value
  emit('toggle-collapse', isCollapsed.value)
}

function handleItemClick(item: NavItem): void {
  emit('item-click', item)
}
</script>
