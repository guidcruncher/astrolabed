<script setup lang="ts">
import WhiptailCombobox from './WhiptailCombobox.vue'
import type { WhiptailOption } from './types'

const config = defineModel<Record<string, any>>({ required: true })

const levelOptions: WhiptailOption[] = [
    { label: 'Trace', value: 'Trace' },
    { label: 'Debug', value: 'Debug' },
    { label: 'Information', value: 'Information' },
    { label: 'Warning', value: 'Warning' },
    { label: 'Error', value: 'Error' },
    { label: 'Critical', value: 'Critical' },
]
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-group">
            <label class="wt-label">Logging Level</label>
            <WhiptailCombobox v-model="config.Level" :options="levelOptions" />
        </div>
    </div>
</template>

<style scoped>
.wt-section-body {
    display: flex;
    flex-direction: column;
    gap: 12px;
}
</style>
