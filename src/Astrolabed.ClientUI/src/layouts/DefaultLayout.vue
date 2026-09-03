<template>
  <div class="min-h-screen bg-slate-900 text-slate-100 flex flex-col md:flex-row">
    <AppSidebar
      :items="navItems"
      @toggle-collapse="onSidebarCollapse"
      @item-click="onNavItemClick"
    />

    <main class="flex-1 p-6 overflow-y-auto">
      <AppButton @click="handleLogout">Logout</AppButton>
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { type NavItem } from '../types/types'
import { useAuth } from '../composables/useAuth'

const navItems: NavItem[] = [
  { icon: 'Gauge', label: 'Dashboard', shortLabel: 'DB', to: '/app/', exact: true },
  { icon: 'HandHelping', label: 'DHCP Leases', shortLabel: 'DHCP', to: '/app/dhcp' },
  { icon: 'Timer', label: 'DNS Bench', shortLabel: 'BNH', to: '/app/benchmark' },
  { icon: 'Logs', label: 'DNS Events', shortLabel: 'DNS', to: '/app/dns' },
  { icon: 'DatabaseZap', label: 'DNS Cache', shortLabel: 'CAC', to: '/app/cache' },
  { icon: 'Binoculars', label: 'DNS Query', shortLabel: 'QRY', to: '/app/dnsquery' },
  { icon: 'Network', label: 'Network Devices', shortLabel: 'NET', to: '/app/network' },
]

const { user, logout, isAuthenticated } = useAuth()

async function handleLogout() {
  await logout()
  window.location.href = '/login'
}

function onSidebarCollapse(isCollapsed: boolean): void {}

function onNavItemClick(item: NavItem): void {}
</script>
