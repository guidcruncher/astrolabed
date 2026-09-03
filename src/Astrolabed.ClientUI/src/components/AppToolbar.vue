<template>
  <div
    class="w-full flex flex-col sm:flex-row items-center justify-between gap-4 p-3 bg-gray-800 border border-gray-700 rounded-lg shadow-sm text-gray-200 mb-[5px]"
  >
    <div class="flex items-center gap-2 w-full sm:w-auto overflow-x-auto">
      {{ pageTitle }}
    </div>
    <!-- Center/Right Group: Search & Utilities -->
    <div class="flex items-center gap-3 w-full sm:w-auto">
      <slot />
      <AppButton @click="handleLogout"><LogOut /></AppButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { LogOut } from '@lucide/vue'
import { useAuth } from '../composables/useAuth'
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const pageTitle = computed(() => route.meta.pageTitle || 'System Overview')
const { user, logout, isAuthenticated } = useAuth()

async function handleLogout() {
  await logout()
  window.location.href = '/login'
}
</script>
