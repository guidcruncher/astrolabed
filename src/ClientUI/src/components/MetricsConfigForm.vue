<script setup lang="ts">
import type { WhiptailOption } from './types'
import { type MetricsOptions } from '../composables/useServerOptions'
const config = defineModel<MetricsOptions>({ required: true })

const engineOptions: WhiptailOption[] = [{ label: 'Prometheus', value: 'prometheus' }]
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.enabled" label="Enable Metrics" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Storage Engine</label>
                <WhiptailSelect v-model="config.storageEngine" :options="engineOptions" />
            </div>
        </div>

        <div class="wt-form-row wt-form-group">
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.listenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.listenPort" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Metrics Endpoint Location</label>
                <input v-model="config.location" type="text" class="wt-input" />
            </div>
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
