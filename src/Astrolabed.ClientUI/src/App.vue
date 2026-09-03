<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import DefaultLayout from './layouts/DefaultLayout.vue'

const route = useRoute()

// Dynamically select layout specified in route meta, or default to DefaultLayout
const layout = computed(() => route.meta.layout || DefaultLayout)
</script>

<template>
  <component :is="layout">
    <router-view />
  </component>
</template>
