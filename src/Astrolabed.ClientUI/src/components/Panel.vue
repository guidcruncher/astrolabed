<template>
  <div class="bg-slate-800 p-6 rounded-lg border border-slate-700">
    <h3 class="text-sm font-semibold text-slate-400 uppercase">{{ title }}</h3>
    <slot />
  </div>
</template>

<script setup lang="ts">
interface Props {
  title: string
}
const props = withDefaults(defineProps<Props>(), {
  title: '',
})
</script>
