<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue'
import WhiptailCheckboxGroup from './WhiptailCheckboxGroup.vue'
import type { WhiptailOption } from './types'

const selectedValues = defineModel<Array<string | number>>({ default: () => [] })

interface Props {
    title?: string
    options: WhiptailOption[]
    okText?: string
}

withDefaults(defineProps<Props>(), {
    title: 'Checklist',
    okText: 'OK',
})

const emit = defineEmits<{
    (e: 'ok', selected: Array<string | number>): void
}>()
</script>

<template>
    <WhiptailDialog :title="title" :ok-text="okText" @ok="emit('ok', selectedValues)">
        <WhiptailCheckboxGroup v-model="selectedValues" :options="options" />
    </WhiptailDialog>
</template>
