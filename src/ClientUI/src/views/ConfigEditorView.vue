<script setup lang="ts">
import { ref } from 'vue'

import type { TabItem } from '../components/types'

const props = defineProps<{
    initialConfig: Record<string, any>
}>()

const emit = defineEmits<{
    (e: 'save', updatedConfig: Record<string, any>): void
    (e: 'cancel'): void
}>()

// Clone configuration to prevent mutating upstream props directly
const configData = ref<Record<string, any>>(JSON.parse(JSON.stringify(props.initialConfig)))
const activeTab = ref<string>('dns')

const tabs: TabItem[] = [
    { id: 'dns', label: 'DNS Services' },
    { id: 'dhcp', label: 'DHCP Config' },
    { id: 'ntp', label: 'NTP Time' },
    { id: 'metrics', label: 'Metrics' },
    { id: 'webui', label: 'Web UI' },
    { id: 'db', label: 'Database' },
    { id: 'scanner', label: 'NetScanner' },
    { id: 'logging', label: 'Logging' },
]

const handleSave = (): void => {
    emit('save', configData.value)
}

const handleCancel = (): void => {
    emit('cancel')
}
</script>

<template>
    <div class="wt-screen">
        <div class="wt-dialog wt-config-editor-dialog">
            <!-- Terminal Dialog Header -->
            <div class="wt-title wt-header-title">
                <span>[ CONFIGURATION OPTIONS EDITOR ]</span>
                <span class="wt-status-indicator">• READY</span>
            </div>

            <!-- Tabbed Configuration Sections -->
            <WhiptailTabs v-model="activeTab" :tabs="tabs">
                <template #dns>
                    <DnsConfigForm v-model="configData.Dns" />
                </template>

                <template #dhcp>
                    <DhcpConfigForm v-model="configData.Dhcp" />
                </template>

                <template #ntp>
                    <NtpConfigForm v-model="configData.Ntp" />
                </template>

                <template #metrics>
                    <MetricsConfigForm v-model="configData.Metrics" />
                </template>

                <template #webui>
                    <WebUiConfigForm v-model="configData.WebUI" />
                </template>

                <template #db>
                    <DbOptionsConfigForm v-model="configData.DbOptions" />
                </template>

                <template #scanner>
                    <NetworkScannerConfigForm v-model="configData.NetworkScanner" />
                </template>

                <template #logging>
                    <LoggingConfigForm v-model="configData.Logging" />
                </template>
            </WhiptailTabs>

            <!-- Dialog Footer Actions -->
            <div class="wt-footer">
                <WhiptailButton variant="cancel" @click="handleCancel">Cancel</WhiptailButton>
                <WhiptailButton variant="ok" @click="handleSave">Save Config</WhiptailButton>
            </div>
        </div>
    </div>
</template>

<style scoped>
.wt-config-editor-dialog {
    max-width: 900px;
    width: 100%;
}
</style>
