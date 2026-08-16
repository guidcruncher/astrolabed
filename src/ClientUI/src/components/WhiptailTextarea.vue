<script setup lang="ts">
import { computed } from 'vue'

const textValue = defineModel<string>({ default: '' })

interface Props {
    id?: string
    rows?: number
    placeholder?: string
    disabled?: boolean
    showCounter?: boolean
    readonly?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    rows: 5,
    placeholder: '',
    disabled: false,
    showCounter: true,
    readonly: false,
})

const emit = defineEmits<{
    (e: 'change', value: string): void
}>()

const lineCount = computed(() => {
    if (!textValue.value) return 1
    return textValue.value.split('\n').length
})

const charCount = computed(() => textValue.value.length)

const handleInput = (event: Event): void => {
    const val = (event.target as HTMLTextAreaElement).value
    emit('change', val)
}
</script>

<template>
    <div class="wt-textarea-wrapper" :class="{ 'wt-disabled': disabled }">
        <textarea
            :id="id"
            v-model="textValue"
            class="wt-textarea"
            :rows="rows"
            :placeholder="placeholder"
            :disabled="disabled"
            :readonly="readonly"
            @input="handleInput"
        ></textarea>

        <div v-if="showCounter" class="wt-textarea-footer">
            <span>Lines: {{ lineCount }}</span>
            <span>Chars: {{ charCount }}</span>
        </div>
    </div>
</template>

<style scoped>
.wt-textarea-wrapper {
    display: flex;
    flex-direction: column;
    width: 100%;
    background-color: #000;
    border: 1px solid #5f5f5f;
}

.wt-textarea-wrapper:focus-within {
    border-color: #ffff00;
}

.wt-textarea {
    width: 100%;
    padding: 8px;
    background-color: transparent;
    border: none;
    color: #e0e0e0;
    font-family: inherit;
    font-size: 0.95rem;
    line-height: 1.4;
    outline: none;
    resize: vertical;
}

.wt-textarea-footer {
    display: flex;
    justify-content: space-between;
    padding: 2px 8px;
    background-color: #1b1b1b;
    border-top: 1px solid #3a3a3a;
    color: #8a8a8a;
    font-size: 0.8rem;
    user-select: none;
}

.wt-disabled {
    opacity: 0.5;
    pointer-events: none;
}
</style>
