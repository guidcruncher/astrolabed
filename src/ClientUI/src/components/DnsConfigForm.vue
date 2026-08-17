<script setup lang="ts">
import { computed } from 'vue'
import WhiptailCheckbox from './WhiptailCheckbox.vue'
import WhiptailCombobox from './WhiptailCombobox.vue'
import WhiptailDataGrid from './WhiptailDataGrid.vue'
import WhiptailButton from './WhiptailButton.vue'
import type { ColumnDef, PagedResult, WhiptailOption } from './types'

const config = defineModel<Record<string, any>>({ required: true })

const blockModeOptions: WhiptailOption[] = [
    { label: 'NXDOMAIN', value: 'NXDOMAIN' },
    { label: 'NODATA', value: 'NODATA' },
    { label: 'REFUSED', value: 'REFUSED' },
    { label: 'STATIC_IP', value: 'STATIC_IP' },
]

// Column definitions for DataGrids
const defaultResolverCols: ColumnDef[] = [
    { key: 'Name', header: 'Resolver Name' },
    { key: 'Address', header: 'IP Address', width: '160px' },
    { key: 'Port', header: 'Port', width: '100px', align: 'center' },
]

const resolverCols: ColumnDef[] = [
    { key: 'Name', header: 'Name' },
    { key: 'Address', header: 'Address', width: '140px' },
    { key: 'Port', header: 'Port', width: '80px', align: 'center' },
    { key: 'Rule', header: 'Rule Regex' },
    { key: 'Block', header: 'Blocked', width: '90px', align: 'center' },
]

// PagedResult adapters for WhiptailDataGrid
const defaultResolversPaged = computed<PagedResult<any>>(() => ({
    items: config.value.DefaultResolvers || [],
    totalCount: config.value.DefaultResolvers?.length || 0,
    pageNumber: 1,
    pageSize: 10,
}))

const resolversPaged = computed<PagedResult<any>>(() => ({
    items: config.value.Resolvers || [],
    totalCount: config.value.Resolvers?.length || 0,
    pageNumber: 1,
    pageSize: 10,
}))

const addDefaultResolver = (): void => {
    config.value.DefaultResolvers.push({ Name: 'New Resolver', Address: '0.0.0.0', Port: 53 })
}

const addResolver = (): void => {
    config.value.Resolvers.push({
        Name: 'New Rule',
        Address: '127.0.0.1',
        Port: 53,
        Rule: '.*',
        Block: false,
    })
}

const addHostsFile = (): void => {
    config.value.HostsFiles.push('file://')
}

const removeHostsFile = (index: number): void => {
    config.value.HostsFiles.splice(index, 1)
}
</script>

<template>
    <div class="wt-section-body">
        <!-- Listen & Basic Setup -->
        <div class="wt-form-row wt-form-group">
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.Listen.Address" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.Listen.Port" type="number" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Timeout (ms)</label>
                <input v-model.number="config.UpstreamTimeoutMs" type="number" class="wt-input" />
            </div>
        </div>

        <!-- Caching Settings -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold">Cache Engine</span>
            <div class="wt-form-row wt-mt" style="margin-top: 8px">
                <div style="flex: 1">
                    <WhiptailCheckbox v-model="config.Caching.Enabled" label="Enable Cache" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">TTL (Seconds)</label>
                    <input
                        v-model.number="config.Caching.TtlSeconds"
                        type="number"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Max Entries</label>
                    <input
                        v-model.number="config.Caching.MaxEntries"
                        type="number"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Cleanup (Mins)</label>
                    <input
                        v-model.number="config.Caching.CleanupIntervalMinutes"
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
                    <WhiptailCombobox
                        v-model="config.BlockResponse.Mode"
                        :options="blockModeOptions"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Static IP</label>
                    <input v-model="config.BlockResponse.StaticIp" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">TTL</label>
                    <input
                        v-model.number="config.BlockResponse.Ttl"
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
                        v-model="config.ConditionalForwarding.Enabled"
                        label="Enabled"
                    />
                </div>
                <div style="flex: 1">
                    <WhiptailCheckbox
                        v-model="config.ConditionalForwarding.ForwardNonFqdn"
                        label="Forward Non-FQDN"
                    />
                </div>
            </div>
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 2">
                    <label class="wt-label">DHCP Server IP</label>
                    <input
                        v-model="config.ConditionalForwarding.DhcpServerIp"
                        type="text"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">DHCP Port</label>
                    <input
                        v-model.number="config.ConditionalForwarding.DhcpServerPort"
                        type="number"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 2">
                    <label class="wt-label">Local Domain</label>
                    <input
                        v-model="config.ConditionalForwarding.LocalDomain"
                        type="text"
                        class="wt-input"
                    />
                </div>
                <div style="flex: 2">
                    <label class="wt-label">Subnet CIDR</label>
                    <input
                        v-model="config.ConditionalForwarding.LocalSubnetCidr"
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
                v-for="(_, idx) in config.HostsFiles"
                :key="idx"
                class="wt-form-row"
                style="margin-bottom: 6px"
            >
                <input v-model="config.HostsFiles[idx]" type="text" class="wt-input" />
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
