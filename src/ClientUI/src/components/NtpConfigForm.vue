<script setup lang="ts">
import { type NtpOptions } from '../composables/useServerOptions'
const config = defineModel<NtpOptions>({ required: true })

const addUpstreamServer = (): void => {
    config.value.upstream.servers.push('')
}

const removeUpstreamServer = (index: number): void => {
    config.value.upstream.servers.splice(index, 1)
}
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.enabled" label="Enable NTP Service" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.listenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Port</label>
                <input v-model.number="config.port" type="number" class="wt-input" />
            </div>
        </div>

        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <label class="wt-label">Buffer Size</label>
                <input v-model.number="config.bufferSize" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Stratum Level</label>
                <input v-model.number="config.stratum" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Reference ID</label>
                <input v-model="config.referenceId" type="text" class="wt-input" />
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
                        v-model="config.upstream.enabled"
                        label="Sync with Upstream"
                    />
                </div>
                <div style="flex: 2">
                    <label class="wt-label">Poll Interval (Seconds)</label>
                    <input
                        v-model.number="config.upstream.pollIntervalSeconds"
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
                    v-for="(_, idx) in config.upstream.servers"
                    :key="idx"
                    class="wt-form-row"
                    style="margin-bottom: 6px"
                >
                    <input v-model="config.upstream.servers[idx]" type="text" class="wt-input" />
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
