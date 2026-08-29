<script setup lang="ts">
import { computed, type Component } from 'vue'
import {
  CircleQuestionMark,
  Smartphone,
  Tablet,
  Laptop,
  Monitor,
  Bot,
  Router as RouterIcon,
  Terminal,
  Cpu,
  Gamepad,
  Gamepad2,
  Tv,
} from '@lucide/vue'

interface Props {
  deviceType?: string
  size?: number | string
  color?: string
  strokeWidth?: number | string
}

const props = withDefaults(defineProps<Props>(), {
  deviceType: 'Unknown',
  size: '1em',
  color: 'currentColor',
  strokeWidth: undefined,
})

const iconMap: Record<string, Component> = {
  Unknown: CircleQuestionMark,
  iPhone: Smartphone,
  iPad: Tablet,
  Apple: Laptop,
  PC: Monitor,
  Android: Bot,
  Router: RouterIcon,
  Linux: Terminal,
  IoT: Cpu,
  Nintendo: Gamepad,
  Playstation: Gamepad2,
  XBOX: Gamepad2,
  SmartTV: Tv,
}

const iconComponent = computed<Component>(() => {
  if (!props.deviceType) return CircleQuestionMark

  const targetKey = props.deviceType.trim().toLowerCase()
  const matchedKey = Object.keys(iconMap).find((key) => key.toLowerCase() === targetKey)

  return matchedKey ? iconMap[matchedKey] : CircleQuestionMark
})
</script>

<template>
  <component
    :is="iconComponent"
    :size="size"
    :color="color"
    :stroke-width="strokeWidth"
    class="device-icon"
  />
</template>

<style scoped>
.device-icon {
  width: 1em;
  height: 1em;
  color: inherit;
  stroke-width: inherit;
}
</style>
