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

    <div class="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
      <table class="w-full text-left text-sm text-slate-300">
        <thead class="bg-slate-700/50 text-slate-400 uppercase text-xs">
          <tr>
            <th class="p-4">Client Name</th>
            <th class="p-4">IP Address</th>
            <th class="p-4">MAC Address</th>
            <th class="p-4">Status</th>
            <th class="p-4">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="lease in leases" :key="lease.clientId" class="border-t border-slate-700">
            <td class="p-4 font-medium text-white">{{ lease.clientName }}</td>
            <td class="p-4">{{ formatIpAddress(lease.ipAddress) }}</td>
            <td class="p-4 font-mono text-xs">{{ lease.macAddress }}</td>
            <td class="p-4">
              <span
                :class="lease.isActive ? 'bg-emerald-900/60 text-emerald-400' : 'bg-slate-700 text-slate-400'"
                class="px-2 py-1 text-xs rounded-full"
              >
                {{ lease.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td class="p-4">
              <button @click="handleRelease(lease)" class="text-xs text-rose-400 hover:text-rose-300">Release</button>
            </td>
          </tr>
          <tr v-if="leases.length === 0">
            <td colspan="5" class="p-4 text-center text-slate-500">No DHCP leases found.</td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination Footer -->
      <div class="p-4 bg-slate-800 border-t border-slate-700 flex items-center justify-between text-xs text-slate-400">
        <div>
          Showing {{ totalCount === 0 ? 0 : ((currentPage - 1) * pageSize) + 1 }} to {{ Math.min(currentPage * pageSize, totalCount) }} of {{ totalCount }} entries
        </div>
        <div class="flex items-center space-x-2">
          <button
            @click="fetchLeases(currentPage - 1)"
            :disabled="currentPage <= 1"
            class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
          >
            Previous
          </button>
          <span class="px-2 py-1 text-slate-300">Page {{ currentPage }} of {{ totalPages }}</span>
          <button
            @click="fetchLeases(currentPage + 1)"
            :disabled="currentPage >= totalPages"
            class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
          >
            Next
          </button>
        </div>
      </div>
    </div>

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
import { useApi } from '../composables/useApi'
import type { DhcpLease, AllocateOrUpdateDhcpLeaseRequest, IPAddress } from '../types/api'

const { getDhcpLeases, allocateDhcpLease, releaseDhcpLease } = useApi()
const leases = ref<DhcpLease[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalPages = ref<number>(1)
const totalCount = ref<number>(0)

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

const fetchLeases = async (page = 1): Promise<void> => {
  currentPage.value = page
  const data = await getDhcpLeases(currentPage.value, pageSize.value)
  leases.value = data?.items || []
  totalCount.value = data?.totalCount || 0
  totalPages.value = data?.totalPages || Math.ceil(totalCount.value / pageSize.value) || 1
}

const handleAllocate = async (): Promise<void> => {
  await allocateDhcpLease(form.value)
  showModal.value = false
  await fetchLeases(currentPage.value)
}

const handleRelease = async (lease: DhcpLease): Promise<void> => {
  if (confirm(`Release lease for ${lease.clientName}?`)) {
    await releaseDhcpLease({ clientId: lease.clientId, macAddress: lease.macAddress })
    await fetchLeases(currentPage.value)
  }
}

onMounted(() => fetchLeases(1))
</script>
