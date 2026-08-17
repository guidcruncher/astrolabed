<script setup lang="ts">
import WhiptailCheckbox from './WhiptailCheckbox.vue'

const config = defineModel<Record<string, any>>({ required: true })
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.Enabled" label="Enable Web UI" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.ListenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.ListenPort" type="number" class="wt-input" />
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
