<script setup lang="ts">
const config = defineModel<Record<string, any>>({ required: true })
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <label class="wt-label">Max Degree Of Parallelism</label>
                <input
                    v-model.number="config.MaxDegreeOfParallelism"
                    type="number"
                    class="wt-input"
                />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Ping Timeout (ms)</label>
                <input v-model.number="config.PingTimeoutMs" type="number" class="wt-input" />
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

