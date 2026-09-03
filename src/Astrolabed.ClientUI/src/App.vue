<!-- App.vue -->
<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuth } from './composables/useAuth'
import LandingLayout from './layouts/LandingLayout.vue'

const route = useRoute()
const { fetchCurrentUser, isInitialized } = useAuth()

// Fetch current user once when the app mounts if not already initialized
onMounted(async () => {
  if (!isInitialized.value) {
    await fetchCurrentUser()
  }
})

// Only determine layout once route metadata is available and auth is ready
const layout = computed(() => route.meta.layout || LandingLayout)
</script>

<template>
  <!-- 1. Full-screen loader shown during initial auth resolution (prevents layout flash) -->
  <div v-if="!isInitialized" class="min-h-screen bg-slate-950 flex items-center justify-center">
    <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-slate-400"></div>
  </div>

  <!-- 2. Render actual layout only AFTER initial auth status is resolved -->
  <component :is="layout" v-else>
    <router-view />
  </component>
</template>
