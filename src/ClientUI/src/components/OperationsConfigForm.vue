<script setup lang="ts">
defineProps<{
    metrics: any
    webUi: any
    db: any
    scanner: any
    logging: any
}>()
</script>

<template>
    <div class="form-section">
        <h3>Metrics & Telemetry</h3>
        <div class="grid-2">
            <div class="field checkbox">
                <label><input v-model="metrics.Enabled" type="checkbox" /> Enable Metrics</label>
            </div>
            <div class="field">
                <label>Storage Engine</label>
                <input v-model="metrics.StorageEngine" type="text" class="input" />
            </div>
            <div class="field">
                <label>Location Path</label>
                <input v-model="metrics.Location" type="text" class="input" />
            </div>
            <div class="field">
                <label>Listen Port</label>
                <input v-model.number="metrics.ListenPort" type="number" class="input" />
            </div>
        </div>

        <h3>Web Interface & Database</h3>
        <div class="grid-2">
            <div class="field">
                <label>Web UI Port</label>
                <input v-model.number="webUi.ListenPort" type="number" class="input" />
            </div>
            <div class="field">
                <label>Database Provider</label>
                <input v-model="db.DatabaseProvider" type="text" class="input" />
            </div>
            <div class="field grid-span-2">
                <label>Connection String</label>
                <input v-model="db.ConnectionString" type="text" class="input" />
            </div>
        </div>

        <h3>Scanner & Logging</h3>
        <div class="grid-2">
            <div class="field">
                <label>Max Concurrency</label>
                <input
                    v-model.number="scanner.MaxDegreeOfParallelism"
                    type="number"
                    class="input"
                />
            </div>
            <div class="field">
                <label>Logging Level</label>
                <select v-model="logging.Level" class="input">
                    <option value="Trace">Trace</option>
                    <option value="Debug">Debug</option>
                    <option value="Information">Information</option>
                    <option value="Warning">Warning</option>
                    <option value="Error">Error</option>
                </select>
            </div>
        </div>
    </div>
</template>

<style scoped>
.form-section {
    display: flex;
    flex-direction: column;
    gap: 16px;
}
.grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}
.grid-span-2 {
    grid-column: span 2;
}
.field {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.field label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #475569;
}
.input {
    padding: 8px;
    border: 1px solid #cbd5e1;
    border-radius: 4px;
}
</style>
