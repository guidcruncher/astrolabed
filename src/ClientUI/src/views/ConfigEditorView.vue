t
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useServerOptions, type ServerOptions } from '../composables/useServerOptions'
import type { TabItem } from '../components/types'
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const { fetchOptions, updateOptions } = useServerOptions()

const configData = ref<ServerOptions | null>(null)
const loaded = ref<boolean>(false)

const activeTab = ref<string>('dns')
const isModalOpen = ref(false)

const tabs: TabItem[] = [
    { id: 'dns', label: 'DNS Services' },
    { id: 'dhcp', label: 'DHCP Config' },
    { id: 'ntp', label: 'NTP Time' },
    { id: 'metrics', label: 'Metrics' },
    { id: 'webui', label: 'Web UI' },
    { id: 'db', label: 'Database' },
    { id: 'scanner', label: 'NetScanner' },
]

const handleOk = () => {
    isModalOpen.value = false
    router.push('/dashboard')
}

const handleSave = async (): Promise<void> => {
    if (configData.value) {
        await updateOptions(configData.value)
    }

    isModalOpen.value = true
}

const handleCancel = (): void => {
    router.push('/dashboard')
}

onMounted(async () => {
    configData.value = await fetchOptions()
    loaded.value = true
})
</script>

<template>
    <div class="wt-dialog wt-config-editor-dialog" v-if="configData">
        <!-- Terminal Dialog Header -->
        <div class="wt-title wt-header-title">
            <span>[ CONFIGURATION OPTIONS EDITOR ]</span>
            <span class="wt-status-indicator">• READY</span>
        </div>

        <!-- Tabbed Configuration Sections -->
        <WhiptailTabs v-model="activeTab" :tabs="tabs">
            <template #dns>
                <DnsConfigForm v-model="configData.dns" />
            </template>

            <template #dhcp>
                <DhcpConfigForm v-model="configData.dhcp" />
            </template>

            <template #ntp>
                <NtpConfigForm v-model="configData.ntp" />
            </template>

            <template #metrics>
                <MetricsConfigForm v-model="configData.metrics" />
            </template>

            <template #webui>
                <WebUiConfigForm v-model="configData.webUI" />
            </template>

            <template #db>
                <DbOptionsConfigForm v-model="configData.dbOptions" />
            </template>

            <template #scanner>
                <NetworkScannerConfigForm v-model="configData.networkScanner" />
            </template>
        </WhiptailTabs>

        <!-- Dialog Footer Actions -->
        <div class="wt-footer">
            <WhiptailButton variant="cancel" @click="handleCancel">Cancel</WhiptailButton>
            <WhiptailButton variant="ok" @click="handleSave">Save</WhiptailButton>
        </div>
    </div>
    <WhiptailModal
        :is-open="isModalOpen"
        title="Changes saved"
        ok-text="OK"
        show-cancel="false"
        @ok="handleOk"
        @close="handleOk"
    >
        <p>You need to restart the server for changes to take effect.</p>
    </WhiptailModal>
</template>

<style scoped>
.wt-config-editor-dialog {
    max-width: 900px;
    width: 100%;
}
</style>
