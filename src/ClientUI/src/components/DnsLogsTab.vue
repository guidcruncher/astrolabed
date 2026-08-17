<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAstrolabedApi, type DnsResponseEvent } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'
import { useDnsUtils } from '../composables/useDnsUtils'

const { loading, getDnsEvents } = useAstrolabedApi()
const { getDnsStatusLabel } = useDnsUtils()

const logPageNumber = ref<number>(1)
const logPageSize = ref<number>(10)

const pagedResult = ref<PagedResult<DnsResponseEvent>>({
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
})

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

const fetchLogs = async (): Promise<void> => {
    try {
        const result = await getDnsEvents({
            pageNumber: logPageNumber.value,
            pageSize: logPageSize.value,
        })
        pagedResult.value = result ?? {
            items: [],
            pageNumber: logPageNumber.value,
            pageSize: logPageSize.value,
            totalCount: 0,
        }
    } catch {
        pagedResult.value = {
            items: [],
            pageNumber: logPageNumber.value,
            pageSize: logPageSize.value,
            totalCount: 0,
        }
    }
}

const handlePageChange = (page: number): void => {
    logPageNumber.value = page
    fetchLogs()
}

const handlePageSizeChange = (size: number): void => {
    logPageSize.value = size
    logPageNumber.value = 1
    fetchLogs()
}

onMounted(() => {
    fetchLogs()
})
</script>

<template>
    <div class="wt-logs-tab">
        <div class="wt-logs-header">
            <WhiptailButton :disabled="loading" @click="fetchLogs">
                {{ loading ? 'Refreshing...' : 'Refresh Logs' }}
            </WhiptailButton>
        </div>
        <WhiptailDataGrid
            :columns="dnsLogColumns"
            :data="pagedResult"
            :loading="loading"
            @page-change="handlePageChange"
            @page-size-change="handlePageSizeChange"
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
