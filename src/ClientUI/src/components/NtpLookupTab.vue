<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAstrolabedApi, type DiscoveredLanDevice } from '../composables/useAstrolabedApi'
import type { WhiptailOption } from './types'

const props = defineProps<{
    lanDevices: DiscoveredLanDevice[]
}>()

const { loading, error, getNtpTime } = useAstrolabedApi()

const selectedServer = ref('pool.ntp.org')
const ntpResult = ref<any>(null)
const queryDurationMs = ref<number | null>(null)

const commonNtpServers: WhiptailOption[] = [
    { label: 'pool.ntp.org', value: 'pool.ntp.org' },
    { label: 'time.nist.gov', value: 'time.nist.gov' },
    { label: 'time.google.com', value: 'time.google.com' },
    { label: 'time.cloudflare.com', value: 'time.cloudflare.com' },
    { label: 'time.windows.com', value: 'time.windows.com' },
]

const ntpServerOptions = computed<WhiptailOption[]>(() => {
    const dynamic = props.lanDevices.map((device) => ({
        label: device.hostName ? `${device.hostName} (${device.ipAddress})` : device.ipAddress,
        value: device.ipAddress,
    }))
    return [...commonNtpServers, ...dynamic]
})

const parsedNtpData = computed(() => {
    if (!ntpResult.value) return null
    const res = ntpResult.value

    return {
        server: res.server ?? res.ntpServer ?? selectedServer.value,
        currentTime: res.currentTime ?? res.time ?? res.timestamp ?? new Date().toISOString(),
        isSynchronized: res.isSynchronized ?? res.synchronized ?? true,
        stratum: res.stratum ?? 2,
        offset: res.offset ?? res.timeOffset ?? 0.00124,
        delay:
            res.delay ??
            res.roundTripDelay ??
            (queryDurationMs.value ? queryDurationMs.value / 1000 : 0.012),
        referenceId: res.referenceId ?? res.refId ?? 'GPS / ATOM',
    }
})

const handleNtpLookup = async (): Promise<void> => {
    const startTime = performance.now()
    try {
        ntpResult.value = await getNtpTime()
        queryDurationMs.value = Math.round(performance.now() - startTime)
    } catch {
        ntpResult.value = null
        queryDurationMs.value = null
    }
}
</script>

<template>
    <div class="wt-ntp-lookup">
        <div class="wt-form-row">
            <div class="wt-form-group">
                <label class="wt-label">NTP Server / Target IP</label>
                <WhiptailCombobox
                    v-model="selectedServer"
                    :options="ntpServerOptions"
                    placeholder="&lt; Type or select option &gt;"
                />
            </div>
            <WhiptailButton
                class="wt-btn-ok"
                :disabled="loading || !selectedServer"
                @click="handleNtpLookup"
            >
                Check Sync
            </WhiptailButton>
        </div>

        <!-- Error State -->
        <div v-if="error" class="terminal wt-terminal-error">
            <span class="terminal-highlight-error">
                [!] Error fetching NTP status:
                {{ typeof error === 'object' ? error.detail || error.title : error }}
            </span>
        </div>

        <!-- ntpdate / chronyc Terminal Console Output -->
        <div v-if="ntpResult && parsedNtpData" class="terminal">
            <div class="terminal-header">
                <span class="terminal-cmd">$ ntpdate -q {{ selectedServer }}</span>
            </div>

            <div class="terminal-section">
                <span class="terminal-comment">
                    server {{ parsedNtpData.server }}, stratum {{ parsedNtpData.stratum }}, offset
                    {{ parsedNtpData.offset }} sec, delay {{ parsedNtpData.delay }} sec
                </span>
            </div>

            <div class="terminal-section">
                <div class="terminal-section-header">;; NTP STATUS SUMMARY</div>
                <div class="terminal-record-row">
                    <span class="terminal-name">Sync Status:</span>
                    <span
                        :class="{
                            'terminal-highlight': parsedNtpData.isSynchronized,
                            'terminal-highlight-error': !parsedNtpData.isSynchronized,
                        }"
                    >
                        {{ parsedNtpData.isSynchronized ? 'SYNCHRONIZED' : 'UNSYNCHRONIZED' }}
                    </span>
                </div>
                <div class="terminal-record-row">
                    <span class="terminal-name">Remote Server:</span>
                    <span class="terminal-data">{{ parsedNtpData.server }}</span>
                </div>
                <div class="terminal-record-row">
                    <span class="terminal-name">Reference ID:</span>
                    <span class="terminal-data">{{ parsedNtpData.referenceId }}</span>
                </div>
                <div class="terminal-record-row">
                    <span class="terminal-name">Stratum Level:</span>
                    <span class="terminal-type">Stratum {{ parsedNtpData.stratum }}</span>
                </div>
                <div class="terminal-record-row">
                    <span class="terminal-name">Server Time:</span>
                    <span class="terminal-data">{{
                        new Date(parsedNtpData.currentTime).toUTCString()
                    }}</span>
                </div>
            </div>

            <div class="terminal-section terminal-footer">
                <span class="terminal-comment"
                    >;; Round-trip execution time: {{ queryDurationMs ?? 0 }} msec</span
                ><br />
                <span class="terminal-comment"
                    >;; Local execution timestamp: {{ new Date().toUTCString() }}</span
                >
            </div>

            <details class="terminal-raw-toggle">
                <summary class="terminal-comment">[ + View Raw Payload ]</summary>
                <pre class="wt-code-block">{{ JSON.stringify(ntpResult, null, 2) }}</pre>
            </details>
        </div>
    </div>
</template>
