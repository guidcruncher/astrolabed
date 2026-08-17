<script setup lang="ts">
import WhiptailCheckbox from './WhiptailCheckbox.vue'
import WhiptailCombobox from './WhiptailCombobox.vue'
import type { WhiptailOption } from './types'

const config = defineModel<Record<string, any>>({ required: true })

const engineOptions: WhiptailOption[] = [
    { label: 'Prometheus', value: 'prometheus' },
    { label: 'InfluxDB', value: 'influxdb' },
    { label: 'StatsD', value: 'statsd' },
]
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.Enabled" label="Enable Metrics" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Storage Engine</label>
                <WhiptailCombobox v-model="config.StorageEngine" :options="engineOptions" />
            </div>
        </div>

        <div class="wt-form-row wt-form-group">
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.ListenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.ListenPort" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Metrics Endpoint Location</label>
                <input v-model="config.Location" type="text" class="wt-input" />
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
