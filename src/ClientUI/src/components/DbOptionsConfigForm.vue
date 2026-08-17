<script setup lang="ts">
import WhiptailCombobox from './WhiptailCombobox.vue'
import type { WhiptailOption } from './types'

const config = defineModel<Record<string, any>>({ required: true })

const providerOptions: WhiptailOption[] = [
    { label: 'SQLite', value: 'sqlite' },
    { label: 'PostgreSQL', value: 'postgresql' },
    { label: 'MySQL', value: 'mysql' },
]
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-group">
            <label class="wt-label">Database Provider</label>
            <WhiptailCombobox v-model="config.DatabaseProvider" :options="providerOptions" />
        </div>
        <div class="wt-form-group">
            <label class="wt-label">Connection String</label>
            <input v-model="config.ConnectionString" type="text" class="wt-input" />
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
