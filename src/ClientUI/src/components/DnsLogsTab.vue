<script setup lang="ts">
import { type DnsResponseEvent } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'
import { useDnsUtils } from '../composables/useDnsUtils'

const { getDnsStatusLabel } = useDnsUtils()

defineProps<{
    events: PagedResult<DnsResponseEvent>
    loading: boolean
}>()

const emit = defineEmits<{
    (e: 'refresh'): void
    (e: 'page-change', page: number): void
    (e: 'page-size-change', size: number): void
}>()

const dnsLogColumns: ColumnDef<DnsResponseEvent>[] = [
    {
        key: 'timestamp',
        header: 'Timestamp',
        formatter: (row: DnsResponseEvent) =>
            row.timestamp ? new Date(row.timestamp).toLocaleTimeString() : '-',
    },
    { key: 'clientIp', header: 'Client' },
    { key: 'queryName', header: 'Query Name' },
    {
        key: 'queryType',
        header: 'Type',
        formatter: (row: DnsResponseEvent) => `[${row.queryType}]`,
    },
    {
        key: 'status',
        header: 'Status',
        formatter: (row: DnsResponseEvent) => getDnsStatusLabel(row.status),
    },
    {
        key: 'responseIp',
        header: 'Resolved IP',
        formatter: (row: DnsResponseEvent) => row.responseIp || '-',
    },
]
</script>

<template>
    <div class="wt-logs-tab">
        <div class="wt-logs-header">
            <WhiptailButton @click="emit('refresh')"> Refresh Logs </WhiptailButton>
        </div>
        <WhiptailDataGrid
            :columns="dnsLogColumns"
            :data="events"
            :loading="loading"
            @page-change="(p: any) => emit('page-change', p)"
            @page-size-change="(s: any) => emit('page-size-change', s)"
        >
            <template #cell-clientIp="{ row }">
                {{ row.clientName ? `${row.clientName} (${row.clientIp})` : row.clientIp }}
            </template>
            <template #cell-status="{ row }">
                <span
                    :class="
                        getDnsStatusLabel(row.status) === 'NOERROR' ? 'wt-text-ok' : 'wt-text-err'
                    "
                >
                    {{ getDnsStatusLabel(row.status) }}
                </span>
            </template>
        </WhiptailDataGrid>
    </div>
</template>

<style scoped>
.wt-logs-header {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 12px;
}
</style>
