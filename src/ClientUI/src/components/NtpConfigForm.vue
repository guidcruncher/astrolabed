<script setup lang="ts">
import WhiptailCheckbox from './WhiptailCheckbox.vue'
import WhiptailButton from './WhiptailButton.vue'

const config = defineModel<Record<string, any>>({ required: true })

const addUpstreamServer = (): void => {
    config.value.Upstream.Servers.push('')
}

const removeUpstreamServer = (index: number): void => {
    config.value.Upstream.Servers.splice(index, 1)
}
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.Enabled" label="Enable NTP Service" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.ListenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Port</label>
                <input v-model.number="config.Port" type="number" class="wt-input" />
            </div>
        </div>

        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <label class="wt-label">Buffer Size</label>
                <input v-model.number="config.BufferSize" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Stratum Level</label>
                <input v-model.number="config.Stratum" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Reference ID</label>
                <input v-model="config.ReferenceId" type="text" class="wt-input" />
            </div>
        </div>

        <!-- Upstream Sync Configuration -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold"
                >Upstream NTP Synchronization</span
            >
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <WhiptailCheckbox
                        v-model="config.Upstream.Enabled"
                        label="Sync with Upstream"
                    />
                </div>
                <div style="flex: 2">
                    <label class="wt-label">Poll Interval (Seconds)</label>
                    <input
                        v-model.number="config.Upstream.PollIntervalSeconds"
                        type="number"
                        class="wt-input"
                    />
                </div>
            </div>

            <div style="margin-top: 12px">
                <div class="wt-box-header" style="margin-bottom: 6px">
                    <label class="wt-label" style="color: #ffff00">Upstream Servers</label>
                    <WhiptailButton @click="addUpstreamServer">+ Add Server</WhiptailButton>
                </div>
                <div
                    v-for="(_, idx) in config.Upstream.Servers"
                    :key="idx"
                    class="wt-form-row"
                    style="margin-bottom: 6px"
                >
                    <input v-model="config.Upstream.Servers[idx]" type="text" class="wt-input" />
                    <WhiptailButton variant="cancel" @click="removeUpstreamServer(Number(idx))"
                        >X</WhiptailButton
                    >
                </div>
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
