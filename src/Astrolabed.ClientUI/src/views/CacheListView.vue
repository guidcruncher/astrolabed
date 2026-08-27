<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">DNS Cache Entries</h2>
      <AppButton variant="danger" @click="handleClear"> Clear DNS Cache </AppButton>
    </div>

    <DataGrid
      v-model:page="currentPage"
      v-model:pageSize="pageSize"
      :data="entries"
      :columns="columns"
      :total-count="totalCount"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
      @row-select="handleRowSelect"
      @loaded="handleGridLoaded"
    >
      <template #cell-value-payload-questionName="{ row, value }">
        <span class="font-medium text-white">
          {{ typeof value === 'string' ? value : row.payload?.questionName || 'N/A' }}
        </span>
      </template>

      <template #cell-value-payload-questionType="{ row, value }">
        <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">
          {{ formatDnsType(typeof value === 'number' ? value : row.payload?.questionType) }}
        </span>
      </template>

      <template #cell-value-expiresAt="{ row, value }">
        <span class="font-mono text-xs">
          {{ formatDate(value ?? row.expiresAt) }}
        </span>
      </template>

      <template #cell-value-isExpired="{ row, value }">
        <span
          class="text-xs px-2 py-0.5 rounded font-medium"
          :class="
            (value ?? row.isExpired)
              ? 'bg-red-900/50 text-red-300'
              : 'bg-green-900/50 text-green-300'
          "
        >
          {{ (value ?? row.isExpired) ? 'Expired' : 'Active' }}
        </span>
      </template>

      <template #empty> No cached DNS entries found. </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { CacheEntryView } from '../types/api'

const { getCacheEntries, clearCache } = useApi()

const entries = ref<CacheEntryView[]>([])
const currentPage = ref<number>(1)
const pageSize = ref<number>(10)
const totalCount = ref<number>(0)

const columns: Column[] = [
  { key: 'value.payload.questionName', label: 'Domain Question' },
  { key: 'value.payload.questionType', label: 'Type' },
  { key: 'value.expiresAt', label: 'Expiration' },
  { key: 'value.isExpired', label: 'Status' },
]

const loadCache = async (): Promise<void> => {
  const data = await getCacheEntries(currentPage.value, pageSize.value)
  entries.value = data?.items || []
  totalCount.value = data?.totalCount || 0
}

const handlePageChange = (newPage: number): void => {
  currentPage.value = newPage
  loadCache()
}

const handlePageSizeChange = (newSize: number): void => {
  pageSize.value = newSize
  currentPage.value = 1
  loadCache()
}

const handleRowSelect = (row: CacheEntryView): void => {
  console.log('Selected Cache Entry:', row)
}

const handleGridLoaded = (): void => {
  // Executed on grid initialization and data updates
}

const handleClear = async (): Promise<void> => {
  if (confirm('Are you sure you want to clear all DNS cache entries?')) {
    await clearCache()
    currentPage.value = 1
    await loadCache()
  }
}

const formatDate = (dateValue?: unknown): string => {
  if (
    !dateValue ||
    (typeof dateValue !== 'string' && typeof dateValue !== 'number' && !(dateValue instanceof Date))
  ) {
    return 'N/A'
  }
  const parsed = new Date(dateValue)
  return isNaN(parsed.getTime()) ? 'N/A' : parsed.toLocaleString()
}

const formatDnsType = (type?: number): string => {
  if (type === undefined || type === null) return 'UNKNOWN'
  const dnsTypeMap: Record<number, string> = {
    1: 'A',
    2: 'NS',
    3: 'MD',
    4: 'MF',
    5: 'CNAME',
    6: 'SOA',
    7: 'MB',
    8: 'MG',
    9: 'MR',
    10: 'NULL',
    11: 'WKS',
    12: 'PTR',
    13: 'HINFO',
    14: 'MINFO',
    15: 'MX',
    16: 'TXT',
    17: 'RP',
    18: 'AFSDB',
    19: 'X25',
    20: 'ISDN',
    21: 'RT',
    22: 'NSAP',
    23: 'NSAP-PTR',
    24: 'SIG',
    25: 'KEY',
    26: 'PX',
    27: 'GPOS',
    28: 'AAAA',
    29: 'LOC',
    30: 'NXT',
    31: 'EID',
    32: 'NIMLOC',
    33: 'SRV',
    34: 'ATMA',
    35: 'NAPTR',
    36: 'KX',
    37: 'CERT',
    38: 'A6',
    39: 'DNAME',
    40: 'SINK',
    41: 'OPT',
    42: 'APL',
    43: 'DS',
    44: 'SSHFP',
    45: 'IPSECKEY',
    46: 'RRSIG',
    47: 'NSEC',
    48: 'DNSKEY',
    49: 'DHCID',
    50: 'NSEC3',
    51: 'NSEC3PARAM',
    52: 'TLSA',
    53: 'SMIMEA',
    55: 'HIP',
    56: 'NINFO',
    57: 'RKEY',
    58: 'TALINK',
    59: 'CDS',
    60: 'CDNSKEY',
    61: 'OPENPGPKEY',
    62: 'CSYNC',
    63: 'ZONEMD',
    64: 'SVCB',
    65: 'HTTPS',
    99: 'SPF',
    100: 'UINFO',
    101: 'UID',
    102: 'GID',
    103: 'UNSPEC',
    104: 'NID',
    105: 'L32',
    106: 'L64',
    107: 'LP',
    108: 'EUI48',
    109: 'EUI64',
    249: 'TKEY',
    250: 'TSIG',
    251: 'IXFR',
    252: 'AXFR',
    253: 'MAILB',
    254: 'MAILA',
    255: 'ANY',
    256: 'URI',
    257: 'CAA',
    258: 'AVC',
    259: 'DOA',
    260: 'AMTRELAY',
    32768: 'TA',
    32769: 'DLV',
  }
  return dnsTypeMap[type] || `TYPE_${type}`
}

onMounted(() => {
  loadCache()
})
</script>
