<script setup lang="ts">
import { ref, computed } from 'vue'
import type { NtpResponse, NtpHeader } from '../composables/useAstrolabedApi'
import { useAstrolabedApi } from '../composables/useAstrolabedApi'

const { loading, error, getNtpTime } = useAstrolabedApi()

const ntpResult = ref<NtpResponse | null>(null)
const queryDurationMs = ref<number | null>(null)

const parsedNtpData = computed(() => {
    if (!ntpResult.value) return null
    const res = ntpResult.value

    return {
        server: res.server || 'Unknown Server',
        currentTime: res.networkTimeUtc || res.systemTimeUtc || new Date().toISOString(),
        isSynchronized: res.success && !res.errorMessage,
        stratum: res.header?.stratum ?? 0,
        offset: res.offset || '00:00:00',
        delay: res.delay || '00:00:00',
        referenceId: res.header?.referenceId || 'N/A',
        errorMessage: res.errorMessage,
    }
})

const handleNtpLookup = async (): Promise<void> => {
    const startTime = performance.now()
    try {
        ntpResult.value = (await getNtpTime()) as NtpResponse
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
            <WhiptailButton class="wt-btn-ok" :disabled="loading" @click="handleNtpLookup">
                {{ loading ? 'Checking...' : 'Check Sync' }}
            </WhiptailButton>
        </div>

        <!-- Error State -->
        <div v-if="error || parsedNtpData?.errorMessage" class="terminal wt-terminal-error">
            <span class="terminal-highlight-error">
                [!] Error fetching NTP status:
                {{
                    parsedNtpData?.errorMessage ||
                    (typeof error === 'object' ? error?.detail || error?.title : error)
                }}
            </span>
        </div>

        <!-- ntpdate / chronyc Terminal Console Output -->
        <div v-if="ntpResult && parsedNtpData" class="terminal">
            <div class="terminal-header">
                <span class="terminal-cmd">$ ntpdate -q {{ parsedNtpData.server }}</span>
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
                    <span class="terminal-name">Network Time (UTC):</span>
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
