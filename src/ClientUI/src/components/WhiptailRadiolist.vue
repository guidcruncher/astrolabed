<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue'
import WhiptailRadioGroup from './WhiptailRadioGroup.vue'
import type { WhiptailOption } from './types'

const selectedValue = defineModel<string | number | null>({ default: null })

interface Props {
    title?: string
    options: WhiptailOption[]
    okText?: string
}

withDefaults(defineProps<Props>(), {
    title: 'Radiolist',
    okText: 'OK',
})

const emit = defineEmits<{
    (e: 'ok', selected: string | number | null): void
}>()
</script>

<template>
    <WhiptailDialog :title="title" :ok-text="okText" @ok="emit('ok', selectedValue)">
        <WhiptailRadioGroup v-model="selectedValue" :options="options" />
    </WhiptailDialog>
</template>
