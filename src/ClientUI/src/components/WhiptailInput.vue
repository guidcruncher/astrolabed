<script setup lang="ts">
import { ref, onMounted } from 'vue'

const textValue = defineModel<string>({ default: '' })

interface Props {
    id?: string
    type?: 'text' | 'password' | 'email' | 'number'
    placeholder?: string
    disabled?: boolean
    prefix?: string
    suffix?: string
    autofocus?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    type: 'text',
    placeholder: '',
    disabled: false,
    autofocus: false,
})

const emit = defineEmits<{
    (e: 'change', value: string): void
    (e: 'enter'): void
}>()

const inputRef = ref<HTMLInputElement | null>(null)

const handleInput = (event: Event): void => {
    const val = (event.target as HTMLInputElement).value
    emit('change', val)
}

onMounted(() => {
    if (props.autofocus) {
        inputRef.value?.focus()
    }
})
</script>

<template>
    <div class="wt-input-wrapper" :class="{ 'wt-disabled': disabled }">
        <span v-if="prefix" class="wt-input-affix wt-prefix">{{ prefix }}</span>

        <input
            :id="id"
            ref="inputRef"
            v-model="textValue"
            :type="type"
            class="wt-input"
            :placeholder="placeholder"
            :disabled="disabled"
            @input="handleInput"
            @keydown.enter="emit('enter')"
        />

        <span v-if="suffix" class="wt-input-affix wt-suffix">{{ suffix }}</span>
    </div>
</template>

<style scoped>
.wt-input-wrapper {
    display: flex;
    align-items: center;
    width: 100%;
    background-color: #000;
    border: 1px solid #5f5f5f;
}

.wt-input-wrapper:focus-within {
    border-color: #ffff00;
}

.wt-input {
    width: 100%;
    padding: 6px 8px;
    background-color: transparent;
    border: none;
    color: #e0e0e0;
    font-family: inherit;
    font-size: 1rem;
    outline: none;
}

.wt-input-affix {
    padding: 0 8px;
    color: #ffff00;
    font-weight: bold;
    user-select: none;
    white-space: nowrap;
}

.wt-prefix {
    border-right: 1px solid #3a3a3a;
}

.wt-suffix {
    border-left: 1px solid #3a3a3a;
}

.wt-disabled {
    opacity: 0.5;
    pointer-events: none;
}
</style>
