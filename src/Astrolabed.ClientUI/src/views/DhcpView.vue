<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">DHCP Leases</h2>
      <button
        @click="showModal = true"
        class="bg-sky-600 hover:bg-sky-500 text-white px-4 py-2 rounded-md text-sm font-medium"
      >
        Allocate / Renew Lease
      </button>
    </div>

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
        <button 
          @click.stop="handleRelease(row)" 
          class="text-xs text-rose-400 hover:text-rose-300"
        >
          Release
        </button>
      </template>

      <template #empty>
        No DHCP leases found.
      </template>
    </DataGrid>

    <div v-if="showModal" class="fixed inset-0 bg-black/60 flex items-center justify-center p-4">
      <div class="bg-slate-800 rounded-lg p-6 max-w-md w-full border border-slate-700">
        <h3 class="text-lg font-bold mb-4">Allocate DHCP Lease</h3>
        <form @submit.prevent="handleAllocate" class="space-y-4">
          <input v-model="form.clientId" placeholder="Client ID (e.g. DUID)" class="w-full bg-slate-900 border border-slate-700 rounded p-2 text-sm text-white" required />
          <input v-model="form.clientName" placeholder="Client Hostname" class="w-full bg-slate-900 border border-slate-700 rounded p-2 text-sm text-white" required />
          <input v-model="form.macAddress" placeholder="MAC Address" class="w-full bg-slate-900 border border-slate-700 rounded p-2 text-sm text-white" required />
          <input v-model="form.requestedIp" placeholder="Requested IP" class="w-full bg-slate-900 border border-slate-700 rounded p-2 text-sm text-white" required />
          <input v-model.number="form.durationInSeconds" type="number" placeholder="Duration (seconds)" class="w-full bg-slate-900 border border-slate-700 rounded p-2 text-sm text-white" required />
          <div class="flex justify-end space-x-2">
            <button type="button" @click="showModal = false" class="px-4 py-2 bg-slate-700 text-sm rounded">Cancel</button>
            <button type="submit" class="px-4 py-2 bg-sky-600 text-sm rounded">Save</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DhcpLease, AllocateOrUpdateDhcpLeaseRequest, IPAddress } from '../types/api'

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
  { key: 'actions', label: 'Actions' }
]

const showModal = ref<boolean>(false)
const form = ref<AllocateOrUpdateDhcpLeaseRequest>({
  clientId: '',
  clientName: '',
  macAddress: '',
  requestedIp: '',
  durationInSeconds: 86400
})

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

const handleAllocate = async (): Promise<void> => {
  await allocateDhcpLease(form.value)
  showModal.value = false
  await fetchLeases()
}

const handleRelease = async (lease: DhcpLease): Promise<void> => {
  if (confirm(`Release lease for ${lease.clientName}?`)) {
    await releaseDhcpLease({ clientId: lease.clientId, macAddress: lease.macAddress })
    await fetchLeases()
  }
}

onMounted(() => fetchLeases())
</script>
