<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAstrolabedApi, type DiscoveredLanDeviceDto } from '../composables/useAstrolabedApi'
import type { WhiptailOption } from './types'
import { useDnsUtils } from '../composables/useDnsUtils'

const { getDnsStatusLabel } = useDnsUtils()

const props = defineProps<{
    lanDevices: DiscoveredLanDeviceDto[]
}>()

const { loading, queryDns } = useAstrolabedApi()

const selectedDomain = ref('')
const selectedRecordType = ref('A')
const queryResult = ref<any>(null)
const queryDurationMs = ref<number | null>(null)

const commonDomains: WhiptailOption[] = [
    { label: 'google.com', value: 'google.com' },
    { label: 'cloudflare.com', value: 'cloudflare.com' },
    { label: 'github.com', value: 'github.com' },
    { label: 'microsoft.com', value: 'microsoft.com' },
    { label: 'astrolabed.local', value: 'astrolabed.local' },
]

const deviceComboboxOptions = computed<WhiptailOption[]>(() => {
    const dynamic = props.lanDevices.map((device) => ({
        label: device.hostName ? `${device.hostName} (${device.ipAddress})` : device.ipAddress,
        value: device.ipAddress,
    }))
    return [...commonDomains, ...dynamic]
})

const parsedAnswers = computed(() => {
    if (!queryResult.value) return []
    const res = queryResult.value

    if (Array.isArray(res.answers)) return res.answers
    if (Array.isArray(res.Answer)) return res.Answer
    if (Array.isArray(res)) return res
    if (typeof res === 'object' && res.data) {
        return Array.isArray(res.data) ? res.data : [res.data]
    }

    return []
})

const handleDnsLookup = async (): Promise<void> => {
    if (!selectedDomain.value) return
    const startTime = performance.now()
    try {
        queryResult.value = await queryDns(selectedDomain.value, selectedRecordType.value)
        queryDurationMs.value = Math.round(performance.now() - startTime)
    } catch {
        queryResult.value = null
        queryDurationMs.value = null
    }
}
</script>

<template>
    <div class="wt-dns-lookup">
        <div class="wt-form-group">
            <label class="wt-label">Target Domain / IP</label>
            <WhiptailCombobox
                v-model="selectedDomain"
                :options="deviceComboboxOptions"
                placeholder="&lt; Type or select option &gt;"
            />
        </div>

        <div class="wt-form-row">
            <div class="wt-form-group">
                <label class="wt-label">Record Type</label>
                <select v-model="selectedRecordType" class="wt-input wt-select">
                    <option value="A">A (IPv4)</option>
                    <option value="AAAA">AAAA (IPv6)</option>
                    <option value="CNAME">CNAME</option>
                    <option value="MX">MX</option>
                    <option value="PTR">PTR</option>
                    <option value="TXT">TXT</option>
                </select>
            </div>
            <WhiptailButton
                class="wt-btn-ok"
                :disabled="loading || !selectedDomain"
                @click="handleDnsLookup"
            >
                Query
            </WhiptailButton>
        </div>

        <!-- DIG Output Console -->
        <div v-if="queryResult" class="terminal">
            <div class="terminal-header">
                <span class="terminal-cmd"
                    >$ dig {{ selectedDomain }} {{ selectedRecordType }}</span
                >
            </div>

            <div class="terminal-section">
                <span class="terminal-comment">
                    ; &lt;&lt;&gt;&gt; DiG 9.18.12 &lt;&lt;&gt;&gt; {{ selectedDomain }}
                    {{ selectedRecordType }} </span
                ><br />
                <span class="terminal-comment">;; global options: +cmd</span><br />
                <span class="terminal-comment">;; Got answer:</span><br />
                <span class="terminal-comment">
                    ;; -&gt;&gt;HEADER&lt;&lt;- opcode: QUERY, status:
                    <span class="terminal-highlight">
                        {{ getDnsStatusLabel(queryResult.responseCode ?? queryResult.status) }}
                    </span>
                    , id: {{ Math.floor(Math.random() * 60000) }}
                </span>
            </div>

            <div class="terminal-section">
                <div class="terminal-section-header">;; QUESTION SECTION:</div>
                <div class="terminal-record-row">
                    <span class="terminal-name">
                        ;{{ selectedDomain.endsWith('.') ? selectedDomain : selectedDomain + '.' }}
                    </span>
                    <span class="terminal-class">IN</span>
                    <span class="terminal-type">{{ selectedRecordType }}</span>
                </div>
            </div>

            <div class="terminal-section">
                <div class="terminal-section-header">;; ANSWER SECTION:</div>
                <template v-if="parsedAnswers.length > 0">
                    <div v-for="(ans, idx) in parsedAnswers" :key="idx" class="terminal-record-row">
                        <span class="terminal-name">
                            {{
                                ans.name ||
                                ans.domain ||
                                (selectedDomain.endsWith('.')
                                    ? selectedDomain
                                    : selectedDomain + '.')
                            }}
                        </span>
                        <span class="terminal-ttl">{{ ans.ttl ?? ans.TTL ?? 300 }}</span>
                        <span class="terminal-class">IN</span>
                        <span class="terminal-type">{{
                            ans.type || ans.typeStr || selectedRecordType
                        }}</span>
                        <span class="terminal-data">{{
                            ans.data || ans.value || ans.address || ans
                        }}</span>
                    </div>
                </template>
                <div v-else class="terminal-comment">
                    ;; (No records returned or custom raw data format)
                </div>
            </div>

            <div class="terminal-section terminal-footer">
                <span class="terminal-comment">;; Query time: {{ queryDurationMs ?? 12 }} msec</span
                ><br />
                <span class="terminal-comment">;; SERVER: 127.0.0.1#53(astrolabed-dns)</span><br />
                <span class="terminal-comment">;; WHEN: {{ new Date().toUTCString() }}</span>
            </div>

            <details class="terminal-raw-toggle">
                <summary class="terminal-comment">[ + View Raw Payload ]</summary>
                <pre class="wt-code-block">{{ JSON.stringify(queryResult, null, 2) }}</pre>
            </details>
        </div>
    </div>
</template>
