<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue'
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

const selectOption = (val: string | number): void => {
    selectedValue.value = val
}
</script>

<template>
    <WhiptailDialog :title="title" :ok-text="okText" @ok="emit('ok', selectedValue)">
        <div class="wt-list wt-radiolist">
            <div
                v-for="item in options"
                :key="item.value"
                class="wt-list-item"
                @click="selectOption(item.value)"
            >
                <div
                    class="wt-radio"
                    :class="{ 'wt-radio-checked': selectedValue === item.value }"
                ></div>
                {{ item.label }}
            </div>
        </div>
    </WhiptailDialog>
</template>
