<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useAstrolabedApi, type DhcpLease } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'

const { loading, error, getLeases } = useAstrolabedApi()

const leasesPaged = ref<PagedResult<DhcpLease>>({
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
})

const activeOnly = ref<boolean>(true)
const searchQuery = ref<string>('')

const pageNumber = ref<number>(1)
const pageSize = ref<number>(10)

const leaseColumns: ColumnDef<DhcpLease>[] = [
    {
        key: 'clientName',
        header: 'Client Name',
        formatter: (row: DhcpLease) => row.clientName || '< Unknown >',
    },
    { key: 'ip', header: 'IP Address' },
    { key: 'mac', header: 'MAC Address' },
    {
        key: 'vendorClassIdentifier',
        header: 'Vendor Class',
        formatter: (row: DhcpLease) => row.vendorClassIdentifier || '-',
    },
    {
        key: 'expiresAt',
        header: 'Expiration (UTC)',
        formatter: (row: DhcpLease) =>
            row.expiresAt ? new Date(row.expiresAt).toLocaleString() : 'N/A',
    },
]

const fetchLeases = async (): Promise<void> => {
    try {
        const result = (await getLeases(activeOnly.value, {
            pageNumber: pageNumber.value,
            pageSize: pageSize.value,
        })) as PagedResult<DhcpLease>

        leasesPaged.value = result || {
            items: [],
            pageNumber: pageNumber.value,
            pageSize: pageSize.value,
            totalCount: 0,
        }
    } catch {
        leasesPaged.value = {
            items: [],
            pageNumber: pageNumber.value,
            pageSize: pageSize.value,
            totalCount: 0,
        }
    }
}

const toggleActiveOnly = (): void => {
    activeOnly.value = !activeOnly.value
    pageNumber.value = 1
    fetchLeases()
}

const handlePageChange = (page: number): void => {
    pageNumber.value = page
    fetchLeases()
}

const handlePageSizeChange = (size: number): void => {
    pageSize.value = size
    pageNumber.value = 1
    fetchLeases()
}

onMounted(() => {
    fetchLeases()
})
</script>

<template>
    <div class="wt-leases-container">
        <!-- Control Toolbar -->
        <div class="wt-toolbar">
            <div class="wt-toolbar-actions">
                <WhiptailButton
                    :class="activeOnly ? 'wt-btn-active' : 'wt-btn-secondary'"
                    @click="toggleActiveOnly"
                >
                    {{ activeOnly ? '[X] Active Only' : '[ ] Show All' }}
                </WhiptailButton>

                <WhiptailButton class="wt-btn-ok" :disabled="loading" @click="fetchLeases">
                    {{ loading ? 'Refreshing...' : 'Refresh' }}
                </WhiptailButton>
            </div>
        </div>

        <!-- Error Banner -->
        <div v-if="error" class="terminal wt-terminal-error mb-4">
            <span class="terminal-highlight-error">
                [!] Error loading DHCP leases:
                {{ typeof error === 'object' ? error?.detail || error?.title : error }}
            </span>
        </div>

        <!-- Data Grid Component -->
        <WhiptailDataGrid
            :columns="leaseColumns"
            :data="leasesPaged"
            :loading="loading"
            @page-change="handlePageChange"
            @page-size-change="handlePageSizeChange"
        />
    </div>
</template>

<style scoped>
.wt-leases-container {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.wt-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    flex-wrap: wrap;
}

.wt-toolbar-actions {
    display: flex;
    gap: 0.5rem;
}

.mb-4 {
    margin-bottom: 1rem;
}
</style>
