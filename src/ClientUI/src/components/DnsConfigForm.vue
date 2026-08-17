<script setup lang="ts">
import { computed } from 'vue'
import { type DnsOptions } from '../composables/useServerOptions'
import type { ColumnDef, PagedResult, WhiptailOption } from './types'

const config = defineModel<DnsOptions>({ required: true })

const blockModeOptions: WhiptailOption[] = [
    { label: 'NXDOMAIN', value: 'NXDOMAIN' },
    { label: 'NODATA', value: 'NODATA' },
    { label: 'REFUSED', value: 'REFUSED' },
    { label: 'SERVFAIL', value: 'SERVFAIL' },
    { label: 'STATIC_IP', value: 'STATIC_IP' },
]

// Column definitions for DataGrids
const defaultResolverCols: ColumnDef[] = [
    { key: 'name', header: 'Resolver Name' },
    { key: 'address', header: 'IP Address', width: '160px' },
    { key: 'port', header: 'Port', width: '100px', align: 'center' },
]

const resolverCols: ColumnDef[] = [
    { key: 'name', header: 'Name' },
    { key: 'address', header: 'Address', width: '140px' },
    { key: 'port', header: 'Port', width: '80px', align: 'center' },
    { key: 'rule', header: 'Rule Regex' },
    { key: 'block', header: 'Blocked', width: '90px', align: 'center' },
]

// PagedResult adapters for WhiptailDataGrid
const defaultResolversPaged = computed<PagedResult<any>>(() => ({
    items: config.value.defaultResolvers || [],
    totalCount: config.value.defaultResolvers?.length || 0,
    pageNumber: 1,
    pageSize: 10,
}))

const resolversPaged = computed<PagedResult<any>>(() => ({
    items: config.value.resolvers || [],
    totalCount: config.value.resolvers?.length || 0,
    pageNumber: 1,
    pageSize: 10,
}))

const addDefaultResolver = (): void => {
    config.value.defaultResolvers.push({ name: 'New Resolver', address: '0.0.0.0', port: 53 })
}

const addResolver = (): void => {
    config.value.resolvers.push({
        name: 'New Rule',
        address: '127.0.0.1',
        port: 53,
        rule: '.*',
        block: false,
    })
}

const addHostsFile = (): void => {
    config.value.hostsFiles.push('file://')
}

const removeHostsFile = (index: number): void => {
    config.value.hostsFiles.splice(index, 1)
}
</script>

<template>
    <div class="wt-section-body">
        <!-- Listen & Basic Setup -->
        <div class="wt-form-row wt-form-group">
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.listen.address" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.listen.port" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Timeout (ms)</label>
                <input v-model.number="config.upstreamTimeoutMs" type="number" class="wt-input" />
            </div>
        </div>

        <!-- Caching Settings -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold">Cache Engine</span>
            <div class="wt-form-row wt-mt" style="margin-top: 8px">
                <div style="flex: 1">
                    <WhiptailCheckbox v-model="config.caching.enabled" label="Enable Cache" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">TTL (Seconds)</label>
                    <input
                        v-model.number="config.caching.ttlSeconds"
                        type="number"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Max Entries</label>
                    <input
                        v-model.number="config.caching.maxEntries"
                        type="number"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Cleanup (Mins)</label>
                    <input
                        v-model.number="config.caching.cleanupIntervalMinutes"
                        type="number"
                        class="wt-input"
                    />
                </div>
            </div>
        </div>

        <!-- Block Response Configuration -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold"
                >Block Response Behavior</span
            >
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <label class="wt-label">Mode</label>
                    <WhiptailSelect
                        v-model="config.blockResponse.mode"
                        :options="blockModeOptions"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Static IP</label>
                    <input v-model="config.blockResponse.staticIp" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">TTL</label>
                    <input
                        v-model.number="config.blockResponse.ttl"
                        type="number"
                        class="wt-input"
                    />
                </div>
            </div>
        </div>

        <!-- Conditional Forwarding -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold"
                >Conditional Forwarding</span
            >
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <WhiptailCheckbox
                        v-model="config.conditionalForwarding.enabled"
                        label="Enabled"
                    />
                </div>
                <div style="flex: 1">
                    <WhiptailCheckbox
                        v-model="config.conditionalForwarding.forwardNonFqdn"
                        label="Forward Non-FQDN"
                    />
                </div>
            </div>
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 2">
                    <label class="wt-label">DHCP Server IP</label>
                    <input
                        v-model="config.conditionalForwarding.dhcpServerIp"
                        type="text"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">DHCP Port</label>
                    <input
                        v-model.number="config.conditionalForwarding.dhcpServerPort"
                        type="number"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 2">
                    <label class="wt-label">Local Domain</label>
                    <input
                        v-model="config.conditionalForwarding.localDomain"
                        type="text"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 2">
                    <label class="wt-label">Subnet CIDR</label>
                    <input
                        v-model="config.conditionalForwarding.localSubnetCidr"
                        type="text"
                        class="wt-input"
                    />
                </div>
            </div>
        </div>

        <!-- Default Resolvers Grid -->
        <div class="wt-form-group">
            <div class="wt-box-header" style="margin-bottom: 6px">
                <label class="wt-label" style="color: #ffff00">Default Resolvers</label>
                <WhiptailButton @click="addDefaultResolver">+ Add Resolver</WhiptailButton>
            </div>
            <WhiptailDataGrid :data="defaultResolversPaged" :columns="defaultResolverCols" />
        </div>

        <!-- Custom Rule Resolvers Grid -->
        <div class="wt-form-group">
            <div class="wt-box-header" style="margin-bottom: 6px">
                <label class="wt-label" style="color: #ffff00">Rule Resolvers</label>
                <WhiptailButton @click="addResolver">+ Add Rule</WhiptailButton>
            </div>
            <WhiptailDataGrid :data="resolversPaged" :columns="resolverCols">
                <template #cell-Block="{ value }">
                    <span :class="value ? 'wt-text-err' : 'wt-text-ok'">
                        {{ value ? '[BLOCKED]' : '[ALLOW]' }}
                    </span>
                </template>
            </WhiptailDataGrid>
        </div>

        <!-- Hosts Files -->
        <div class="wt-form-group">
            <div class="wt-box-header" style="margin-bottom: 6px">
                <label class="wt-label" style="color: #ffff00">Hosts File Sources</label>
                <WhiptailButton @click="addHostsFile">+ Add Source</WhiptailButton>
            </div>
            <div
                v-for="(_, idx) in config.hostsFiles"
                :key="idx"
                class="wt-form-row"
                style="margin-bottom: 6px"
            >
                <input v-model="config.hostsFiles[idx]" type="text" class="wt-input" />
                <WhiptailButton variant="cancel" @click="removeHostsFile(Number(idx))"
                    >X</WhiptailButton
                >
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
