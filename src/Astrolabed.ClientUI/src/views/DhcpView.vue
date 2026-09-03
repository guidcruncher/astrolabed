<template>
  <div>
    <AppToolbar />

    <DataGrid
      v-model:page="currentPage"
      v-model:pageSize="pageSize"
      :data="leases"
      :columns="columns"
      :total-count="totalCount"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
      @row-select="handleRowSelect"
      @loaded="handleGridLoaded"
    >
      <template #cell-clientName="{ value }">
        <span class="font-medium text-white">{{ value }}</span>
      </template>
      <template #cell-ipAddress="{ value }">
        <span>{{ formatIpAddress(value) }}</span>
      </template>
      <template #cell-macAddress="{ value }">
        <span class="font-mono text-xs">{{ value }}</span>
      </template>
      <template #cell-isActive="{ value }">
        <span
          :class="value ? 'bg-emerald-900/60 text-emerald-400' : 'bg-slate-700 text-slate-400'"
          class="px-2 py-1 text-xs rounded-full"
        >
          {{ value ? 'Active' : 'Inactive' }}
        </span>
      </template>
      <template #cell-actions="{ row }">
        <button @click.stop="handleRelease(row)" class="text-xs text-rose-400 hover:text-rose-300">
          <Trash2 />
        </button>
      </template>
      <template #empty> No DHCP leases found. </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DhcpLease, AllocateOrUpdateDhcpLeaseRequest, IPAddress } from '../types/api'
import { type ModalAction } from '../types/types'
import { Trash2 } from '@lucide/vue'

const { getDhcpLeases, allocateDhcpLease, releaseDhcpLease } = useApi()

const leases = ref<DhcpLease[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'clientName', label: 'Client Name' },
  { key: 'ipAddress', label: 'IP Address' },
  { key: 'macAddress', label: 'MAC Address' },
  { key: 'isActive', label: 'Status' },
  { key: 'actions', label: 'Actions' },
]

const formatIpAddress = (ipAddress?: IPAddress | string): string => {
  if (!ipAddress) return '-'
  if (typeof ipAddress === 'string') return ipAddress
  return ipAddress.address ? String(ipAddress.address) : '-'
}

const fetchLeases = async (): Promise<void> => {
  const data = await getDhcpLeases(currentPage.value, pageSize.value)
  leases.value = data?.items || []
  totalCount.value = data?.totalCount || 0
}

const handlePageChange = (page: number): void => {
  currentPage.value = page
  fetchLeases()
}

const handlePageSizeChange = (size: number): void => {
  pageSize.value = size
  currentPage.value = 1
  fetchLeases()
}

const handleRowSelect = (lease: DhcpLease): void => {
  console.log('Selected DHCP Lease:', lease)
}

const handleGridLoaded = (): void => {
  // Triggered when data source changes or grid mounts
}

const handleRelease = async (lease: DhcpLease): Promise<void> => {
  if (confirm(`Release lease for ${lease.clientName}?`)) {
    await releaseDhcpLease({ clientId: lease.clientId, macAddress: lease.macAddress })
    await fetchLeases()
  }
}

onMounted(() => fetchLeases())
</script>
