<script setup lang="ts">
import { type WebUiOptions } from '../composables/useServerOptions'
const config = defineModel<WebUiOptions>({ required: true })
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.enabled" label="Enable Web UI" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.listenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.listenPort" type="number" class="wt-input" />
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
