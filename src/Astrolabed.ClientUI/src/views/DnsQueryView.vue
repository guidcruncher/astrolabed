<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold">DNS Query</h2>
    </div>

    <!-- Query Form -->
    <div class="bg-slate-800 p-4 rounded-lg mb-6 flex flex-col md:flex-row gap-4 items-end">
      <div class="flex-1">
        <label class="block text-xs font-medium text-slate-400 mb-1">Domain Name</label>
        <input
          v-model="domain"
          type="text"
          placeholder="example.com"
          class="w-full bg-slate-900 border border-slate-700 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
          @keyup.enter="handleQuery"
        />
      </div>

      <div class="w-full md:w-48">
        <label class="block text-xs font-medium text-slate-400 mb-1">Record Type</label>
        <select
          v-model.number="selectedType"
          class="w-full bg-slate-900 border border-slate-700 rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
        >
          <option
            v-for="(label, typeVal) in commonDnsTypes"
            :key="typeVal"
            :value="Number(typeVal)"
          >
            {{ label }} ({{ typeVal }})
          </option>
        </select>
      </div>

      <AppButton variant="primary" :disabled="loading" @click="handleQuery">
        {{ loading ? 'Querying...' : 'Execute Query' }}
      </AppButton>
    </div>

    <!-- Query Header Info -->
    <div
      v-if="wireMessage"
      class="bg-slate-900/50 p-4 rounded-lg mb-6 border border-slate-800 text-sm grid grid-cols-2 md:grid-cols-4 gap-4"
    >
      <div>
        <span class="text-slate-400 block text-xs">Transaction ID</span>
        <span class="font-mono text-white"
          >0x{{ wireMessage.transactionId?.toString(16).toUpperCase() ?? '0' }}</span
        >
      </div>
      <div>
        <span class="text-slate-400 block text-xs">Response Code</span>
        <span class="font-mono text-white">{{ formatResponseCode(wireMessage.responseCode) }}</span>
      </div>
      <div>
        <span class="text-slate-400 block text-xs">Authoritative</span>
        <span
          class="font-medium"
          :class="wireMessage.authoritativeAnswer ? 'text-green-400' : 'text-slate-400'"
        >
          {{ wireMessage.authoritativeAnswer ? 'Yes' : 'No' }}
        </span>
      </div>
      <div>
        <span class="text-slate-400 block text-xs">Truncated</span>
        <span
          class="font-medium"
          :class="wireMessage.truncated ? 'text-red-400' : 'text-slate-400'"
        >
          {{ wireMessage.truncated ? 'Yes' : 'No' }}
        </span>
      </div>
    </div>

    <!-- Results DataGrid -->
    <DataGrid
      v-model:page="currentPage"
      v-model:pageSize="pageSize"
      :data="paginatedAnswers"
      :columns="columns"
      :total-count="answers.length"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
      @row-select="handleRowSelect"
      @loaded="handleGridLoaded"
    >
      <template #cell-value-name="{ row, value }">
        <span class="font-medium text-white">
          {{ typeof value === 'string' ? value : row.name || 'N/A' }}
        </span>
      </template>

      <template #cell-value-type="{ row, value }">
        <span class="bg-slate-700 text-xs px-2 py-0.5 rounded">
          {{ formatDnsType(typeof value === 'number' ? value : row.type) }}
        </span>
      </template>

      <template #cell-value-ttl="{ row, value }">
        <span class="font-mono text-xs"> {{ value ?? row.ttl ?? 0 }}s </span>
      </template>

      <template #cell-value-data="{ row, value }">
        <span class="font-mono text-xs text-slate-300 break-all">
          {{ typeof value === 'string' ? value : row.data || 'N/A' }}
          <span v-if="row.parsedIp" class="bg-slate-700 text-xs px-2 py-0.5 rounded">
            {{ row.parsedIp }}
          </span>
        </span>
      </template>

      <template #empty>
        {{
          hasQueried ? 'No DNS answer records returned.' : 'Enter a domain to execute a DNS query.'
        }}
      </template>
    </DataGrid>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { type Column } from '../types/types'
import { useApi } from '../composables/useApi'
import type { DnsResourceRecord, DnsWireMessage, DnsType } from '../types/api'

const { queryDns, loading } = useApi()

const domain = ref<string>('')
const selectedType = ref<DnsType>(1)
const hasQueried = ref<boolean>(false)

const wireMessage = ref<DnsWireMessage | null>(null)
const answers = ref<DnsResourceRecord[]>([])

const currentPage = ref<number>(1)
const pageSize = ref<number>(10)

const commonDnsTypes: Record<number, string> = {
  1: 'A',
  28: 'AAAA',
  5: 'CNAME',
  15: 'MX',
  16: 'TXT',
  12: 'PTR',
  33: 'SRV',
  2: 'NS',
  6: 'SOA',
  255: 'ANY',
}

const columns: Column[] = [
  { key: 'value.name', label: 'Name' },
  { key: 'value.type', label: 'Type' },
  { key: 'value.ttl', label: 'TTL' },
  { key: 'value.data', label: 'Data / Value' },
]

const paginatedAnswers = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return answers.value.slice(start, start + pageSize.value)
})

const handleQuery = async (): Promise<void> => {
  if (!domain.value.trim()) return

  hasQueried.value = true
  wireMessage.value = await queryDns(domain.value.trim(), selectedType.value)
  answers.value = wireMessage.value?.answers || []
  currentPage.value = 1
}

const handlePageChange = (newPage: number): void => {
  currentPage.value = newPage
}

const handlePageSizeChange = (newSize: number): void => {
  pageSize.value = newSize
  currentPage.value = 1
}

const handleRowSelect = (row: DnsResourceRecord): void => {
  console.log('Selected DNS Resource Record:', row)
}

const handleGridLoaded = (): void => {
  // Executed on grid initialization and data updates
}

const formatResponseCode = (code?: number): string => {
  if (code === undefined || code === null) return 'UNKNOWN'
  const rcodeMap: Record<number, string> = {
    0: 'NOERROR',
    1: 'FORMERR',
    2: 'SERVFAIL',
    3: 'NXDOMAIN',
    4: 'NOTIMP',
    5: 'REFUSED',
  }
  return rcodeMap[code] || `RCODE_${code}`
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
    249: 'TKEY',
    250: 'TSIG',
    251: 'IXFR',
    252: 'AXFR',
    255: 'ANY',
    257: 'CAA',
  }
  return dnsTypeMap[type] || `TYPE_${type}`
}
</script>
