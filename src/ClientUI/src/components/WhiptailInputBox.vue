<script setup lang="ts">
import WhiptailDialog from './WhiptailDialog.vue'

const modelValue = defineModel<string>({ default: '' })

interface Props {
    title?: string
    label?: string
    placeholder?: string
    okText?: string
    cancelText?: string
}

withDefaults(defineProps<Props>(), {
    title: 'Input Box',
    label: '',
    placeholder: '',
    okText: 'Submit',
    cancelText: 'Cancel',
})

const emit = defineEmits<{
    (e: 'submit', value: string): void
    (e: 'cancel'): void
}>()

const handleSubmit = (): void => {
    emit('submit', modelValue.value)
}
</script>

<template>
    <WhiptailDialog
        :title="title"
        :ok-text="okText"
        :cancel-text="cancelText"
        :show-cancel="true"
        @ok="handleSubmit"
        @cancel="emit('cancel')"
    >
        <label v-if="label">{{ label }}</label>
        <input
            v-model="modelValue"
            type="text"
            class="wt-input"
            :placeholder="placeholder"
            @keyup.enter="handleSubmit"
        />
    </WhiptailDialog>
</template>
