<template>
  <div class="min-h-screen bg-slate-900 text-slate-100 flex flex-col md:flex-row">
    <aside class="w-full md:w-64 bg-slate-800 border-b md:border-b-0 md:border-r border-slate-700 p-4">
      <div class="flex items-center justify-between md:block">
        <h1 class="text-xl font-bold text-sky-400">Astrolabed UI</h1>
        <div class="mt-2 text-xs text-slate-400">
          API:
          <input
            v-model="apiBaseUrl"
            type="text"
            class="bg-slate-900 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs w-full mt-1 focus:outline-none focus:border-sky-500"
          />
        </div>
      </div>
      <nav class="mt-6 flex md:flex-col space-x-2 md:space-x-0 md:space-y-1 overflow-x-auto">
        <router-link to="/" class="px-3 py-2 rounded-md text-sm font-medium hover:bg-slate-700" active-class="bg-sky-600 text-white">Dashboard</router-link>
        <router-link to="/dhcp" class="px-3 py-2 rounded-md text-sm font-medium hover:bg-slate-700" active-class="bg-sky-600 text-white">DHCP Leases</router-link>
        <router-link to="/dns" class="px-3 py-2 rounded-md text-sm font-medium hover:bg-slate-700" active-class="bg-sky-600 text-white">DNS Events</router-link>
        <router-link to="/network" class="px-3 py-2 rounded-md text-sm font-medium hover:bg-slate-700" active-class="bg-sky-600 text-white">Network Devices</router-link>
      </nav>
    </aside>

    <main class="flex-1 p-6 overflow-y-auto">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useApi } from '../composables/useApi'

const { apiBaseUrl } = useApi()
</script>
