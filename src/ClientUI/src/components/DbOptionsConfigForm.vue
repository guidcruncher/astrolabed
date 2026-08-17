<script setup lang="ts">
import type { WhiptailOption } from './types'
import { type DbOptions } from '../composables/useServerOptions'

const config = defineModel<DbOptions>({ required: true })

const providerOptions: WhiptailOption[] = [
    { label: 'SQLite', value: 'sqlite' },
    { label: 'PostgreSQL', value: 'postgres' },
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
