import { computed, type MaybeRefOrGetter, type ComputedRef, toValue } from 'vue'

/**
 * Styling configuration containing classes for HTML components and SVG elements.
 */
export interface DnsTypeColorConfig {
  /** HTML text color class */
  text: string
  /** HTML background color class */
  bg: string
  /** HTML border color class */
  border: string
  /** SVG fill class for vector shapes */
  fill: string
  /** SVG stroke class for vector outlines */
  stroke: string
}

/**
 * Static mapping of DNS record types to Tailwind CSS styling configurations for dark mode.
 */
export const DNS_TYPE_DARK_MODE_CLASSES: Record<string, DnsTypeColorConfig> = {
  // --- Core Addressing (Blues / Cyans) ---
  A: {
    text: 'text-blue-300',
    bg: 'bg-blue-500/15',
    border: 'border-blue-500/30',
    fill: 'fill-blue-400/30',
    stroke: 'stroke-blue-400',
  },
  AAAA: {
    text: 'text-cyan-300',
    bg: 'bg-cyan-500/15',
    border: 'border-cyan-500/30',
    fill: 'fill-cyan-400/30',
    stroke: 'stroke-cyan-400',
  },
  PTR: {
    text: 'text-sky-300',
    bg: 'bg-sky-500/15',
    border: 'border-sky-500/30',
    fill: 'fill-sky-400/30',
    stroke: 'stroke-sky-400',
  },

  // --- Canonical Names & Redirection (Teals / Emeralds) ---
  CNAME: {
    text: 'text-teal-300',
    bg: 'bg-teal-500/15',
    border: 'border-teal-500/30',
    fill: 'fill-teal-400/30',
    stroke: 'stroke-teal-400',
  },
  DNAME: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/15',
    border: 'border-emerald-500/30',
    fill: 'fill-emerald-400/30',
    stroke: 'stroke-emerald-400',
  },

  // --- Routing & Mail Services (Purples / Indigos) ---
  MX: {
    text: 'text-purple-300',
    bg: 'bg-purple-500/15',
    border: 'border-purple-500/30',
    fill: 'fill-purple-400/30',
    stroke: 'stroke-purple-400',
  },
  SRV: {
    text: 'text-indigo-300',
    bg: 'bg-indigo-500/15',
    border: 'border-indigo-500/30',
    fill: 'fill-indigo-400/30',
    stroke: 'stroke-indigo-400',
  },
  SVCB: {
    text: 'text-violet-300',
    bg: 'bg-violet-500/15',
    border: 'border-violet-500/30',
    fill: 'fill-violet-400/30',
    stroke: 'stroke-violet-400',
  },
  HTTPS: {
    text: 'text-fuchsia-300',
    bg: 'bg-fuchsia-500/15',
    border: 'border-fuchsia-500/30',
    fill: 'fill-fuchsia-400/30',
    stroke: 'stroke-fuchsia-400',
  },
  NAPTR: {
    text: 'text-indigo-200',
    bg: 'bg-indigo-500/15',
    border: 'border-indigo-400/30',
    fill: 'fill-indigo-300/30',
    stroke: 'stroke-indigo-300',
  },
  KX: {
    text: 'text-violet-200',
    bg: 'bg-violet-500/15',
    border: 'border-violet-400/30',
    fill: 'fill-violet-300/30',
    stroke: 'stroke-violet-300',
  },

  // --- Authority & Delegation (Amber / Yellow) ---
  SOA: {
    text: 'text-amber-300',
    bg: 'bg-amber-500/15',
    border: 'border-amber-500/30',
    fill: 'fill-amber-400/30',
    stroke: 'stroke-amber-400',
  },
  NS: {
    text: 'text-yellow-300',
    bg: 'bg-yellow-500/15',
    border: 'border-yellow-500/30',
    fill: 'fill-yellow-400/30',
    stroke: 'stroke-yellow-400',
  },

  // --- Security & DNSSEC (Greens) ---
  DS: {
    text: 'text-green-300',
    bg: 'bg-green-500/15',
    border: 'border-green-500/30',
    fill: 'fill-green-400/30',
    stroke: 'stroke-green-400',
  },
  DNSKEY: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/15',
    border: 'border-emerald-500/30',
    fill: 'fill-emerald-400/30',
    stroke: 'stroke-emerald-400',
  },
  RRSIG: {
    text: 'text-green-200',
    bg: 'bg-green-500/15',
    border: 'border-green-400/30',
    fill: 'fill-green-300/30',
    stroke: 'stroke-green-300',
  },
  NSEC: {
    text: 'text-lime-300',
    bg: 'bg-lime-500/15',
    border: 'border-lime-500/30',
    fill: 'fill-lime-300/30',
    stroke: 'stroke-lime-300',
  },
  NSEC3: {
    text: 'text-lime-300',
    bg: 'bg-lime-500/15',
    border: 'border-lime-500/30',
    fill: 'fill-lime-300/30',
    stroke: 'stroke-lime-300',
  },
  NSEC3PARAM: {
    text: 'text-lime-300',
    bg: 'bg-lime-500/15',
    border: 'border-lime-400/30',
    fill: 'fill-lime-400/30',
    stroke: 'stroke-lime-400',
  },
  TLSA: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/15',
    border: 'border-emerald-400/30',
    fill: 'fill-emerald-400/30',
    stroke: 'stroke-emerald-400',
  },
  SMIMEA: {
    text: 'text-teal-300',
    bg: 'bg-teal-500/15',
    border: 'border-teal-400/30',
    fill: 'fill-teal-400/30',
    stroke: 'stroke-teal-400',
  },
  CERT: {
    text: 'text-green-300',
    bg: 'bg-green-500/15',
    border: 'border-green-400/30',
    fill: 'fill-green-400/30',
    stroke: 'stroke-green-400',
  },
  SSHFP: {
    text: 'text-emerald-200',
    bg: 'bg-emerald-500/15',
    border: 'border-emerald-400/30',
    fill: 'fill-emerald-300/30',
    stroke: 'stroke-emerald-300',
  },
  IPSECKEY: {
    text: 'text-teal-200',
    bg: 'bg-teal-500/15',
    border: 'border-teal-400/30',
    fill: 'fill-teal-300/30',
    stroke: 'stroke-teal-300',
  },
  OPENPGPKEY: {
    text: 'text-green-200',
    bg: 'bg-green-500/15',
    border: 'border-green-400/30',
    fill: 'fill-green-200/30',
    stroke: 'stroke-green-300',
  },
  CDS: {
    text: 'text-emerald-200',
    bg: 'bg-emerald-500/15',
    border: 'border-emerald-400/30',
    fill: 'fill-emerald-200/30',
    stroke: 'stroke-emerald-300',
  },
  CDNSKEY: {
    text: 'text-green-200',
    bg: 'bg-green-500/15',
    border: 'border-green-400/30',
    fill: 'fill-green-300/30',
    stroke: 'stroke-green-300',
  },
  CSYNC: {
    text: 'text-lime-200',
    bg: 'bg-lime-500/15',
    border: 'border-lime-400/30',
    fill: 'fill-lime-200/30',
    stroke: 'stroke-lime-300',
  },
  TA: {
    text: 'text-green-300',
    bg: 'bg-green-500/15',
    border: 'border-green-400/30',
    fill: 'fill-green-400/30',
    stroke: 'stroke-green-400',
  },
  DLV: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/15',
    border: 'border-emerald-400/30',
    fill: 'fill-emerald-400/30',
    stroke: 'stroke-emerald-400',
  },

  // --- Authentication & Verification (Rose / Pinks) ---
  TXT: {
    text: 'text-rose-300',
    bg: 'bg-rose-500/15',
    border: 'border-rose-500/30',
    fill: 'fill-rose-400/30',
    stroke: 'stroke-rose-400',
  },
  SPF: {
    text: 'text-pink-300',
    bg: 'bg-pink-500/15',
    border: 'border-pink-500/30',
    fill: 'fill-pink-400/30',
    stroke: 'stroke-pink-400',
  },
  CAA: {
    text: 'text-rose-300',
    bg: 'bg-rose-500/15',
    border: 'border-rose-400/30',
    fill: 'fill-rose-300/30',
    stroke: 'stroke-rose-300',
  },
  URI: {
    text: 'text-pink-300',
    bg: 'bg-pink-500/15',
    border: 'border-pink-400/30',
    fill: 'fill-pink-300/30',
    stroke: 'stroke-pink-300',
  },
  AVC: {
    text: 'text-rose-300',
    bg: 'bg-rose-500/15',
    border: 'border-rose-400/30',
    fill: 'fill-rose-400/30',
    stroke: 'stroke-rose-400',
  },
  DOA: {
    text: 'text-pink-300',
    bg: 'bg-pink-500/15',
    border: 'border-pink-400/30',
    fill: 'fill-pink-400/30',
    stroke: 'stroke-pink-400',
  },

  // --- Metadata & Diagnostics (Orange) ---
  LOC: {
    text: 'text-orange-300',
    bg: 'bg-orange-500/15',
    border: 'border-orange-500/30',
    fill: 'fill-orange-400/30',
    stroke: 'stroke-orange-400',
  },
  HINFO: {
    text: 'text-orange-200',
    bg: 'bg-orange-500/15',
    border: 'border-orange-400/30',
    fill: 'fill-orange-300/30',
    stroke: 'stroke-orange-300',
  },
  MINFO: {
    text: 'text-amber-200',
    bg: 'bg-amber-500/15',
    border: 'border-amber-400/30',
    fill: 'fill-amber-300/30',
    stroke: 'stroke-amber-300',
  },
  RP: {
    text: 'text-amber-300',
    bg: 'bg-amber-500/15',
    border: 'border-amber-400/30',
    fill: 'fill-amber-300/30',
    stroke: 'stroke-amber-300',
  },
  ZONEMD: {
    text: 'text-orange-300',
    bg: 'bg-orange-500/15',
    border: 'border-orange-400/30',
    fill: 'fill-orange-300/30',
    stroke: 'stroke-orange-300',
  },

  // --- Security Keys & Sessions (Yellows) ---
  TKEY: {
    text: 'text-yellow-200',
    bg: 'bg-yellow-500/15',
    border: 'border-yellow-400/30',
    fill: 'fill-yellow-300/30',
    stroke: 'stroke-yellow-300',
  },
  TSIG: {
    text: 'text-yellow-300',
    bg: 'bg-yellow-500/15',
    border: 'border-yellow-500/30',
    fill: 'fill-yellow-400/30',
    stroke: 'stroke-yellow-400',
  },
  SIG: {
    text: 'text-amber-200',
    bg: 'bg-amber-500/15',
    border: 'border-amber-400/30',
    fill: 'fill-amber-200/30',
    stroke: 'stroke-amber-300',
  },
  KEY: {
    text: 'text-yellow-200',
    bg: 'bg-yellow-500/15',
    border: 'border-yellow-400/30',
    fill: 'fill-yellow-200/30',
    stroke: 'stroke-yellow-300',
  },

  // --- Zone Transfers & Special Commands (Red / Crimson) ---
  AXFR: {
    text: 'text-red-300',
    bg: 'bg-red-500/15',
    border: 'border-red-500/30',
    fill: 'fill-red-400/30',
    stroke: 'stroke-red-400',
  },
  IXFR: {
    text: 'text-red-300',
    bg: 'bg-red-500/15',
    border: 'border-red-400/30',
    fill: 'fill-red-300/30',
    stroke: 'stroke-red-300',
  },
  OPT: {
    text: 'text-rose-200',
    bg: 'bg-rose-500/15',
    border: 'border-rose-400/30',
    fill: 'fill-rose-200/30',
    stroke: 'stroke-rose-300',
  },
  ANY: {
    text: 'text-red-300',
    bg: 'bg-red-500/20',
    border: 'border-red-500/40',
    fill: 'fill-red-400/30',
    stroke: 'stroke-red-400',
  },

  // --- Obsolete, Experimental & Rare Types (Muted Gray / Slate) ---
  MD: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  MF: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  MB: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  MG: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  MR: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  NULL: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-500/10',
    border: 'border-zinc-600/30',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  WKS: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  AFSDB: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  X25: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  ISDN: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  RT: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  NSAP: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  'NSAP-PTR': {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  PX: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  GPOS: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  NXT: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  EID: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  NIMLOC: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  ATMA: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  A6: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  SINK: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  APL: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  DHCID: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  HIP: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  NINFO: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  RKEY: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  TALINK: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  UINFO: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  UID: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  GID: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  UNSPEC: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  NID: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  L32: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  L64: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  LP: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  EUI48: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  EUI64: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  MAILB: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  MAILA: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },
  AMTRELAY: {
    text: 'text-zinc-300',
    bg: 'bg-zinc-500/15',
    border: 'border-zinc-500/30',
    fill: 'fill-zinc-400/30',
    stroke: 'stroke-zinc-400',
  },

  // --- Fallback State ---
  UNKNOWN: {
    text: 'text-slate-300',
    bg: 'bg-slate-500/15',
    border: 'border-slate-500/30',
    fill: 'fill-slate-400/30',
    stroke: 'stroke-slate-400',
  },
}

/**
 * Pure function to get the DnsTypeColorConfig record directly for a given DNS type string.
 */
function getDnsTypeColorConfig(type?: string | null): DnsTypeColorConfig {
  if (!type || typeof type !== 'string') {
    return DNS_TYPE_DARK_MODE_CLASSES['UNKNOWN']
  }
  const key = type.trim().toUpperCase()
  return DNS_TYPE_DARK_MODE_CLASSES[key] ?? DNS_TYPE_DARK_MODE_CLASSES['UNKNOWN']
}

export function useDnsTypeColor() {
  return {
    getDnsTypeColorConfig,
  }
}
