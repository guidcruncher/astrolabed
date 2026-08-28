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

    <!-- Dig Format Output Terminal -->
    <div
      class="bg-slate-950 border border-slate-800 rounded-lg p-4 font-mono text-xs text-slate-200 overflow-x-auto"
    >
      <div
        class="flex justify-between items-center pb-2 mb-3 border-b border-slate-800 text-slate-500"
      >
        <span>Output Format: dig</span>
        <button
          v-if="digOutput"
          class="hover:text-white transition-colors"
          @click="copyToClipboard"
        >
          {{ copied ? 'Copied' : 'Copy Output' }}
        </button>
      </div>

      <pre v-if="digOutput" class="whitespace-pre text-emerald-400 leading-relaxed">{{
        digOutput
      }}</pre>
      <div v-else-if="loading" class="text-slate-500 py-8 text-center">Executing DNS query...</div>
      <div v-else class="text-slate-500 py-8 text-center">
        {{ hasQueried ? 'No response received.' : 'Enter a domain to execute a DNS query.' }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useApi } from '../composables/useApi'
import type { DnsResourceRecord, DnsWireMessage, DnsType } from '../types/api'

const { queryDns, loading } = useApi()

const domain = ref<string>('')
const selectedType = ref<DnsType>(1)
const hasQueried = ref<boolean>(false)
const copied = ref<boolean>(false)

const wireMessage = ref<DnsWireMessage | null>(null)
const answers = ref<DnsResourceRecord[]>([])

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

const handleQuery = async (): Promise<void> => {
  if (!domain.value.trim()) return

  hasQueried.value = true
  wireMessage.value = await queryDns(domain.value.trim(), selectedType.value)
  answers.value = wireMessage.value?.answers || []
}

const copyToClipboard = async (): Promise<void> => {
  if (!digOutput.value) return
  await navigator.clipboard.writeText(digOutput.value)
  copied.value = true
  setTimeout(() => {
    copied.value = false
  }, 2000)
}

const digOutput = computed<string>(() => {
  if (!wireMessage.value) return ''

  const queryDomain = domain.value.trim()
  const formattedDomain = queryDomain.endsWith('.') ? queryDomain : `${queryDomain}.`
  const recordTypeName = formatDnsType(selectedType.value)
  const status = formatResponseCode(wireMessage.value.responseCode)
  const txId = wireMessage.value.transactionId ?? 0

  const flags: string[] = []
  if (wireMessage.value.authoritativeAnswer) flags.push('aa')
  if (wireMessage.value.truncated) flags.push('tc')
  if (wireMessage.value.recursionDesired) flags.push('rd')
  if (wireMessage.value.recursionAvailable) flags.push('ra')

  const flagStr = flags.length > 0 ? flags.join(' ') : 'none'
  const answerCount = answers.value.length
  const authorityCount = wireMessage.value.authorities?.length ?? 0
  const additionalCount = wireMessage.value.additionals?.length ?? 0

  let output = `; <<>> DiG <<>> ${queryDomain} ${recordTypeName}\n`
  output += `;; global options: +cmd\n`
  output += `;; Got answer:\n`
  output += `;; ->>HEADER<<- opcode: QUERY, status: ${status}, id: ${txId}\n`
  output += `;; flags: ${flagStr}; QUERY: 1, ANSWER: ${answerCount}, AUTHORITY: ${authorityCount}, ADDITIONAL: ${additionalCount}\n\n`

  output += `;; QUESTION SECTION:\n`
  output += `;${formattedDomain.padEnd(24)} IN\t${recordTypeName}\n\n`

  if (answerCount > 0) {
    output += `;; ANSWER SECTION:\n`
    answers.value.forEach((rr) => {
      const rrName = rr.name ? (rr.name.endsWith('.') ? rr.name : `${rr.name}.`) : formattedDomain
      const ttl = rr.ttl ?? 0
      const rrType = formatDnsType(rr.type)
      var rrData = rr.data || rr.parsedIp || 'N/A'
      if (rr.parsedIp && rr.data) {
        rrData = `${rr.parsedIp.padEnd(16)} ${rr.data}`
      }

      output += `${rrName.padEnd(24)} ${ttl}\tIN\t${rrType}\t${rrData}\n`
    })
  }

  return output
})

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
