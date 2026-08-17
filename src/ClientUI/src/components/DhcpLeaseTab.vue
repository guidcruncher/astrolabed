<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAstrolabedApi, type DhcpLease } from '../composables/useAstrolabedApi'

const { loading, error, getLeases } = useAstrolabedApi()

const leases = ref<DhcpLease[]>([])
const activeOnly = ref<boolean>(true)
const searchQuery = ref<string>('')

// Column definitions for WhiptailDataGrid matching the JSON properties
const columns = [
    { key: 'clientName', label: 'Client Name', sortable: true },
    { key: 'ip', label: 'IP Address', sortable: true },
    { key: 'mac', label: 'MAC Address', sortable: true },
    { key: 'vendorClassIdentifier', label: 'Vendor Class', sortable: true },
    { key: 'expiresAt', label: 'Expiration (UTC)', sortable: true },
]

// Filtered data based on search input matching JSON fields
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

const fetchLeases = async (): Promise<void> => {
    try {
        const result = (await getLeases(activeOnly.value)) as DhcpLease[]
        leases.value = Array.isArray(result) ? result : []
    } catch {
        leases.value = []
    }
}

const toggleActiveOnly = () => {
    activeOnly.value = !activeOnly.value
    fetchLeases()
}

const formatDate = (isoString: string): string => {
    if (!isoString) return 'N/A'
    const date = new Date(isoString)
    return isNaN(date.getTime()) ? isoString : date.toLocaleString()
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
            :columns="columns"
            :data="filteredLeases"
            :loading="loading"
            empty-text="No DHCP leases found."
            class="wt-leases-grid"
        >
            <!-- Custom Slot: Client Name Column -->
            <template #cell-clientName="{ row }">
                <span class="terminal-name">{{ row.clientName || '' }}</span>
            </template>

            <!-- Custom Slot: IP Column -->
            <template #cell-ip="{ row }">
                <span class="terminal-data">{{ row.ip }}</span>
            </template>

            <!-- Custom Slot: MAC Column -->
            <template #cell-mac="{ row }">
                <span class="terminal-type">{{ row.mac }}</span>
            </template>

            <!-- Custom Slot: Vendor Class Column -->
            <template #cell-vendorClassIdentifier="{ row }">
                <span class="terminal-comment">{{ row.vendorClassIdentifier || '-' }}</span>
            </template>

            <!-- Custom Slot: Expiration Column -->
            <template #cell-expiresAt="{ row }">
                <span class="terminal-data">{{ formatDate(row.expiresAt) }}</span>
            </template>
        </WhiptailDataGrid>

        <!-- Footer Info Bar -->
        <div class="wt-grid-footer">
            <span class="terminal-comment"> Total leases listed: {{ filteredLeases.length }} </span>
        </div>
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

.wt-grid-footer {
    padding: 0.5rem 0;
    font-family: monospace;
}

.mb-4 {
    margin-bottom: 1rem;
}
</style>
