<script setup>
import logo from '../assets/logo.svg'
</script>

<template>
  <div
    class="relative min-h-screen bg-gradient-to-br from-[#0b0f19] via-[#111827] to-[#070a10] text-slate-100"
  >
    <div class="fixed inset-0 pointer-events-none flex items-center justify-center z-0">
      <img :src="logo" alt="Logo" class="w-1/2 h-auto opacity-20" />
    </div>

    <main class="relative z-10 container mx-auto px-4 py-12">
      <router-view />
    </main>
  </div>
</template>
