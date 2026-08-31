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
 * Static mapping of DNS record types to Tailwind CSS styling configurations for dark mode (Brightened).
 */
export const DNS_TYPE_DARK_MODE_CLASSES: Record<string, DnsTypeColorConfig> = {
  // --- Core Addressing (Blues / Cyans) ---
  A: {
    text: 'text-blue-300',
    bg: 'bg-blue-500/35',
    border: 'border-blue-300/60',
    fill: 'fill-blue-300/50',
    stroke: 'stroke-blue-200',
  },
  AAAA: {
    text: 'text-cyan-300',
    bg: 'bg-cyan-500/35',
    border: 'border-cyan-300/60',
    fill: 'fill-cyan-300/50',
    stroke: 'stroke-cyan-200',
  },
  PTR: {
    text: 'text-sky-300',
    bg: 'bg-sky-500/35',
    border: 'border-sky-300/60',
    fill: 'fill-sky-300/50',
    stroke: 'stroke-sky-200',
  },

  // --- Canonical Names & Redirection (Teals / Emeralds) ---
  CNAME: {
    text: 'text-teal-300',
    bg: 'bg-teal-500/35',
    border: 'border-teal-300/60',
    fill: 'fill-teal-300/50',
    stroke: 'stroke-teal-200',
  },
  DNAME: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/35',
    border: 'border-emerald-300/60',
    fill: 'fill-emerald-300/50',
    stroke: 'stroke-emerald-200',
  },

  // --- Routing & Mail Services (Purples / Indigos) ---
  MX: {
    text: 'text-purple-300',
    bg: 'bg-purple-500/35',
    border: 'border-purple-300/60',
    fill: 'fill-purple-300/50',
    stroke: 'stroke-purple-200',
  },
  SRV: {
    text: 'text-indigo-300',
    bg: 'bg-indigo-500/35',
    border: 'border-indigo-300/60',
    fill: 'fill-indigo-300/50',
    stroke: 'stroke-indigo-200',
  },
  SVCB: {
    text: 'text-violet-300',
    bg: 'bg-violet-500/35',
    border: 'border-violet-300/60',
    fill: 'fill-violet-300/50',
    stroke: 'stroke-violet-200',
  },
  HTTPS: {
    text: 'text-fuchsia-300',
    bg: 'bg-fuchsia-500/35',
    border: 'border-fuchsia-300/60',
    fill: 'fill-fuchsia-300/50',
    stroke: 'stroke-fuchsia-200',
  },
  NAPTR: {
    text: 'text-indigo-200',
    bg: 'bg-indigo-500/35',
    border: 'border-indigo-300/60',
    fill: 'fill-indigo-200/50',
    stroke: 'stroke-indigo-200',
  },
  KX: {
    text: 'text-violet-200',
    bg: 'bg-violet-500/35',
    border: 'border-violet-300/60',
    fill: 'fill-violet-200/50',
    stroke: 'stroke-violet-200',
  },

  // --- Authority & Delegation (Amber / Yellow) ---
  SOA: {
    text: 'text-amber-300',
    bg: 'bg-amber-500/35',
    border: 'border-amber-300/60',
    fill: 'fill-amber-300/50',
    stroke: 'stroke-amber-200',
  },
  NS: {
    text: 'text-yellow-300',
    bg: 'bg-yellow-500/35',
    border: 'border-yellow-300/60',
    fill: 'fill-yellow-300/50',
    stroke: 'stroke-yellow-200',
  },

  // --- Security & DNSSEC (Greens) ---
  DS: {
    text: 'text-green-300',
    bg: 'bg-green-500/35',
    border: 'border-green-300/60',
    fill: 'fill-green-300/50',
    stroke: 'stroke-green-200',
  },
  DNSKEY: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/35',
    border: 'border-emerald-300/60',
    fill: 'fill-emerald-300/50',
    stroke: 'stroke-emerald-200',
  },
  RRSIG: {
    text: 'text-green-200',
    bg: 'bg-green-500/35',
    border: 'border-green-300/60',
    fill: 'fill-green-200/50',
    stroke: 'stroke-green-200',
  },
  NSEC: {
    text: 'text-lime-300',
    bg: 'bg-lime-500/35',
    border: 'border-lime-300/60',
    fill: 'fill-lime-200/50',
    stroke: 'stroke-lime-200',
  },
  NSEC3: {
    text: 'text-lime-300',
    bg: 'bg-lime-500/35',
    border: 'border-lime-300/60',
    fill: 'fill-lime-200/50',
    stroke: 'stroke-lime-200',
  },
  NSEC3PARAM: {
    text: 'text-lime-300',
    bg: 'bg-lime-500/35',
    border: 'border-lime-300/60',
    fill: 'fill-lime-300/50',
    stroke: 'stroke-lime-200',
  },
  TLSA: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/35',
    border: 'border-emerald-300/60',
    fill: 'fill-emerald-300/50',
    stroke: 'stroke-emerald-200',
  },
  SMIMEA: {
    text: 'text-teal-300',
    bg: 'bg-teal-500/35',
    border: 'border-teal-300/60',
    fill: 'fill-teal-300/50',
    stroke: 'stroke-teal-200',
  },
  CERT: {
    text: 'text-green-300',
    bg: 'bg-green-500/35',
    border: 'border-green-300/60',
    fill: 'fill-green-300/50',
    stroke: 'stroke-green-200',
  },
  SSHFP: {
    text: 'text-emerald-200',
    bg: 'bg-emerald-500/35',
    border: 'border-emerald-300/60',
    fill: 'fill-emerald-200/50',
    stroke: 'stroke-emerald-200',
  },
  IPSECKEY: {
    text: 'text-teal-200',
    bg: 'bg-teal-500/35',
    border: 'border-teal-300/60',
    fill: 'fill-teal-200/50',
    stroke: 'stroke-teal-200',
  },
  OPENPGPKEY: {
    text: 'text-green-200',
    bg: 'bg-green-500/35',
    border: 'border-green-300/60',
    fill: 'fill-green-200/50',
    stroke: 'stroke-green-200',
  },
  CDS: {
    text: 'text-emerald-200',
    bg: 'bg-emerald-500/35',
    border: 'border-emerald-300/60',
    fill: 'fill-emerald-200/50',
    stroke: 'stroke-emerald-200',
  },
  CDNSKEY: {
    text: 'text-green-200',
    bg: 'bg-green-500/35',
    border: 'border-green-300/60',
    fill: 'fill-green-200/50',
    stroke: 'stroke-green-200',
  },
  CSYNC: {
    text: 'text-lime-200',
    bg: 'bg-lime-500/35',
    border: 'border-lime-300/60',
    fill: 'fill-lime-200/50',
    stroke: 'stroke-lime-200',
  },
  TA: {
    text: 'text-green-300',
    bg: 'bg-green-500/35',
    border: 'border-green-300/60',
    fill: 'fill-green-300/50',
    stroke: 'stroke-green-200',
  },
  DLV: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/35',
    border: 'border-emerald-300/60',
    fill: 'fill-emerald-300/50',
    stroke: 'stroke-emerald-200',
  },

  // --- Authentication & Verification (Rose / Pinks) ---
  TXT: {
    text: 'text-rose-300',
    bg: 'bg-rose-500/35',
    border: 'border-rose-300/60',
    fill: 'fill-rose-300/50',
    stroke: 'stroke-rose-200',
  },
  SPF: {
    text: 'text-pink-300',
    bg: 'bg-pink-500/35',
    border: 'border-pink-300/60',
    fill: 'fill-pink-300/50',
    stroke: 'stroke-pink-200',
  },
  CAA: {
    text: 'text-rose-300',
    bg: 'bg-rose-500/35',
    border: 'border-rose-300/60',
    fill: 'fill-rose-200/50',
    stroke: 'stroke-rose-200',
  },
  URI: {
    text: 'text-pink-300',
    bg: 'bg-pink-500/35',
    border: 'border-pink-300/60',
    fill: 'fill-pink-200/50',
    stroke: 'stroke-pink-200',
  },
  AVC: {
    text: 'text-rose-300',
    bg: 'bg-rose-500/35',
    border: 'border-rose-300/60',
    fill: 'fill-rose-300/50',
    stroke: 'stroke-rose-200',
  },
  DOA: {
    text: 'text-pink-300',
    bg: 'bg-pink-500/35',
    border: 'border-pink-300/60',
    fill: 'fill-pink-300/50',
    stroke: 'stroke-pink-200',
  },

  // --- Metadata & Diagnostics (Orange) ---
  LOC: {
    text: 'text-orange-300',
    bg: 'bg-orange-500/35',
    border: 'border-orange-300/60',
    fill: 'fill-orange-300/50',
    stroke: 'stroke-orange-200',
  },
  HINFO: {
    text: 'text-orange-200',
    bg: 'bg-orange-500/35',
    border: 'border-orange-300/60',
    fill: 'fill-orange-200/50',
    stroke: 'stroke-orange-200',
  },
  MINFO: {
    text: 'text-amber-200',
    bg: 'bg-amber-500/35',
    border: 'border-amber-300/60',
    fill: 'fill-amber-200/50',
    stroke: 'stroke-amber-200',
  },
  RP: {
    text: 'text-amber-300',
    bg: 'bg-amber-500/35',
    border: 'border-amber-300/60',
    fill: 'fill-amber-200/50',
    stroke: 'stroke-amber-200',
  },
  ZONEMD: {
    text: 'text-orange-300',
    bg: 'bg-orange-500/35',
    border: 'border-orange-300/60',
    fill: 'fill-orange-200/50',
    stroke: 'stroke-orange-200',
  },

  // --- Security Keys & Sessions (Yellows) ---
  TKEY: {
    text: 'text-yellow-200',
    bg: 'bg-yellow-500/35',
    border: 'border-yellow-300/60',
    fill: 'fill-yellow-200/50',
    stroke: 'stroke-yellow-200',
  },
  TSIG: {
    text: 'text-yellow-300',
    bg: 'bg-yellow-500/35',
    border: 'border-yellow-300/60',
    fill: 'fill-yellow-300/50',
    stroke: 'stroke-yellow-200',
  },
  SIG: {
    text: 'text-amber-200',
    bg: 'bg-amber-500/35',
    border: 'border-amber-300/60',
    fill: 'fill-amber-200/50',
    stroke: 'stroke-amber-200',
  },
  KEY: {
    text: 'text-yellow-200',
    bg: 'bg-yellow-500/35',
    border: 'border-yellow-300/60',
    fill: 'fill-yellow-200/50',
    stroke: 'stroke-yellow-200',
  },

  // --- Zone Transfers & Special Commands (Red / Crimson) ---
  AXFR: {
    text: 'text-red-300',
    bg: 'bg-red-500/35',
    border: 'border-red-300/60',
    fill: 'fill-red-300/50',
    stroke: 'stroke-red-200',
  },
  IXFR: {
    text: 'text-red-300',
    bg: 'bg-red-500/35',
    border: 'border-red-300/60',
    fill: 'fill-red-200/50',
    stroke: 'stroke-red-200',
  },
  OPT: {
    text: 'text-rose-200',
    bg: 'bg-rose-500/35',
    border: 'border-rose-300/60',
    fill: 'fill-rose-200/50',
    stroke: 'stroke-rose-200',
  },
  ANY: {
    text: 'text-red-300',
    bg: 'bg-red-500/40',
    border: 'border-red-300/70',
    fill: 'fill-red-300/50',
    stroke: 'stroke-red-200',
  },

  // --- Obsolete, Experimental & Rare Types (Muted Gray / Slate) ---
  MD: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  MF: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  MB: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  MG: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  MR: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  NULL: {
    text: 'text-zinc-200',
    bg: 'bg-zinc-500/30',
    border: 'border-zinc-400/50',
    fill: 'fill-zinc-300/40',
    stroke: 'stroke-zinc-300',
  },
  WKS: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  AFSDB: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  X25: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  ISDN: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  RT: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  NSAP: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  'NSAP-PTR': {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  PX: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  GPOS: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  NXT: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  EID: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  NIMLOC: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  ATMA: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  A6: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  SINK: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  APL: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  DHCID: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  HIP: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  NINFO: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  RKEY: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  TALINK: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  UINFO: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  UID: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  GID: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  UNSPEC: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  NID: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  L32: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  L64: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  LP: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  EUI48: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  EUI64: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  MAILB: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  MAILA: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },
  AMTRELAY: {
    text: 'text-zinc-100',
    bg: 'bg-zinc-500/35',
    border: 'border-zinc-300/60',
    fill: 'fill-zinc-300/50',
    stroke: 'stroke-zinc-200',
  },

  // --- Fallback State ---
  UNKNOWN: {
    text: 'text-slate-100',
    bg: 'bg-slate-500/35',
    border: 'border-slate-300/60',
    fill: 'fill-slate-300/50',
    stroke: 'stroke-slate-200',
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
