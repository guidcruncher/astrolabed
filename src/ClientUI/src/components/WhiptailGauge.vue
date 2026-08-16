<script setup lang="ts">
import { ref, onUnmounted } from 'vue'
import WhiptailDialog from './WhiptailDialog.vue'

interface Props {
    title?: string
    btnText?: string
}

withDefaults(defineProps<Props>(), {
    title: 'Progress Gauge',
    btnText: 'Start',
})

const emit = defineEmits<{
    (e: 'complete'): void
}>()

const progress = ref<number>(0)
let timer: ReturnType<typeof setInterval> | null = null

const startProgress = (): void => {
    if (timer) clearInterval(timer)
    progress.value = 0

    timer = setInterval(() => {
        progress.value += 2
        if (progress.value >= 100) {
            progress.value = 100
            if (timer) clearInterval(timer)
            timer = null
            emit('complete')
        }
    }, 50)
}

onUnmounted(() => {
    if (timer) clearInterval(timer)
})
</script>

<template>
    <WhiptailDialog :title="title" :ok-text="btnText" @ok="startProgress">
        <div class="wt-gauge">
            <div class="wt-gauge-fill" :style="{ width: progress + '%' }"></div>
        </div>
    </WhiptailDialog>
</template>
