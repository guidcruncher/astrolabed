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
  { icon: 'Gauge', label: 'Dashboard', shortLabel: 'DB', to: '/', exact: true },
  { icon: 'HandHelping', label: 'DHCP Leases', shortLabel: 'DHCP', to: '/dhcp' },
  { icon: 'Timer', label: 'DNS Bench', shortLabel: 'BNH', to: '/benchmark' },
  { icon: 'Logs', label: 'DNS Events', shortLabel: 'DNS', to: '/dns' },
  { icon: 'DatabaseZap', label: 'DNS Cache', shortLabel: 'CAC', to: '/cache' },
  { icon: 'Binoculars', label: 'DNS Query', shortLabel: 'QRY', to: '/dnsquery' },
  { icon: 'Network', label: 'Network Devices', shortLabel: 'NET', to: '/network' },
]

function onSidebarCollapse(isCollapsed: boolean): void {}

function onNavItemClick(item: NavItem): void {}
</script>
