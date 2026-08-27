<template>
  <div class="min-h-screen bg-slate-900 text-slate-100 flex flex-col md:flex-row">
    <AppSidebar
      :items="navItems"
      @toggle-collapse="onSidebarCollapse"
      @item-click="onNavItemClick"
    />

    <main class="flex-1 p-6 overflow-y-auto">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { type NavItem } from '../types/types'
 
const navItems: NavItem[] = [
  { label: 'Dashboard', shortLabel: 'DB', to: '/', exact: true },
  { label: 'DHCP Leases', shortLabel: 'DHCP', to: '/dhcp' },
  { label: 'DNS Events', shortLabel: 'DNS', to: '/dns' },
  { label: 'Network Devices', shortLabel: 'NET', to: '/network' }
]

function onSidebarCollapse(isCollapsed: boolean): void {
}

function onNavItemClick(item: NavItem): void {
}
</script>
