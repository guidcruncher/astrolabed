<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAstrolabedApi, type DhcpLease } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'

const { loading, error, getLeases } = useAstrolabedApi()

const leases = ref<DhcpLease[]>([])
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

const filteredLeases = computed(() => {
    if (!searchQuery.value.trim()) return leases.value

    const query = searchQuery.value.toLowerCase()
    return leases.value.filter(
        (lease) =>
            lease.clientName?.toLowerCase().includes(query) ||
            lease.ip.toLowerCase().includes(query) ||
            lease.mac.toLowerCase().includes(query) ||
            lease.vendorClassIdentifier?.toLowerCase().includes(query),
    )
})

const leasesPaged = computed<PagedResult<DhcpLease>>(() => {
    const totalCount = filteredLeases.value.length
    const startIndex = (pageNumber.value - 1) * pageSize.value
    const endIndex = startIndex + pageSize.value
    const items = filteredLeases.value.slice(startIndex, endIndex)

    return {
        items,
        pageNumber: pageNumber.value,
        pageSize: pageSize.value,
        totalCount,
    }
})

const fetchLeases = async (): Promise<void> => {
    try {
        const result = (await getLeases(activeOnly.value)) as DhcpLease[]
        leases.value = Array.isArray(result) ? result : []
        pageNumber.value = 1
    } catch {
        leases.value = []
    }
}

const toggleActiveOnly = (): void => {
    activeOnly.value = !activeOnly.value
    fetchLeases()
}

const handlePageChange = (page: number): void => {
    pageNumber.value = page
}

const handlePageSizeChange = (size: number): void => {
    pageSize.value = size
    pageNumber.value = 1
}

onMounted(() => {
    fetchLeases()
})
</script>

<template>
    <div class="wt-leases-container">
        <!-- Control Toolbar -->
        <div class="wt-toolbar">
            <div class="wt-form-group">
                <input
                    v-model="searchQuery"
                    type="text"
                    class="wt-input wt-search-input"
                    placeholder="Search client, IP, MAC, or vendor..."
                    @input="pageNumber = 1"
                />
            </div>

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

.wt-search-input {
    min-width: 280px;
}

.mb-4 {
    margin-bottom: 1rem;
}
</style>
