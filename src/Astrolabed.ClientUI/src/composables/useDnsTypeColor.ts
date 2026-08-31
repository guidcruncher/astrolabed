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
    bg: 'bg-blue-950/60',
    border: 'border-blue-800/50',
    fill: 'fill-blue-400/20',
    stroke: 'stroke-blue-400',
  },
  AAAA: {
    text: 'text-cyan-300',
    bg: 'bg-cyan-950/60',
    border: 'border-cyan-800/50',
    fill: 'fill-cyan-400/20',
    stroke: 'stroke-cyan-400',
  },
  PTR: {
    text: 'text-sky-300',
    bg: 'bg-sky-950/60',
    border: 'border-sky-800/50',
    fill: 'fill-sky-400/20',
    stroke: 'stroke-sky-400',
  },

  // --- Canonical Names & Redirection (Teals / Emeralds) ---
  CNAME: {
    text: 'text-teal-300',
    bg: 'bg-teal-950/60',
    border: 'border-teal-800/50',
    fill: 'fill-teal-400/20',
    stroke: 'stroke-teal-400',
  },
  DNAME: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-950/60',
    border: 'border-emerald-800/50',
    fill: 'fill-emerald-400/20',
    stroke: 'stroke-emerald-400',
  },

  // --- Routing & Mail Services (Purples / Indigos) ---
  MX: {
    text: 'text-purple-300',
    bg: 'bg-purple-950/60',
    border: 'border-purple-800/50',
    fill: 'fill-purple-400/20',
    stroke: 'stroke-purple-400',
  },
  SRV: {
    text: 'text-indigo-300',
    bg: 'bg-indigo-950/60',
    border: 'border-indigo-800/50',
    fill: 'fill-indigo-400/20',
    stroke: 'stroke-indigo-400',
  },
  SVCB: {
    text: 'text-violet-300',
    bg: 'bg-violet-950/60',
    border: 'border-violet-800/50',
    fill: 'fill-violet-400/20',
    stroke: 'stroke-violet-400',
  },
  HTTPS: {
    text: 'text-fuchsia-300',
    bg: 'bg-fuchsia-950/60',
    border: 'border-fuchsia-800/50',
    fill: 'fill-fuchsia-400/20',
    stroke: 'stroke-fuchsia-400',
  },
  NAPTR: {
    text: 'text-indigo-200',
    bg: 'bg-indigo-950/40',
    border: 'border-indigo-900/50',
    fill: 'fill-indigo-300/20',
    stroke: 'stroke-indigo-300',
  },
  KX: {
    text: 'text-violet-200',
    bg: 'bg-violet-950/40',
    border: 'border-violet-900/50',
    fill: 'fill-violet-300/20',
    stroke: 'stroke-violet-300',
  },

  // --- Authority & Delegation (Amber / Yellow) ---
  SOA: {
    text: 'text-amber-300',
    bg: 'bg-amber-950/60',
    border: 'border-amber-800/50',
    fill: 'fill-amber-400/20',
    stroke: 'stroke-amber-400',
  },
  NS: {
    text: 'text-yellow-300',
    bg: 'bg-yellow-950/60',
    border: 'border-yellow-800/50',
    fill: 'fill-yellow-400/20',
    stroke: 'stroke-yellow-400',
  },

  // --- Security & DNSSEC (Greens) ---
  DS: {
    text: 'text-green-300',
    bg: 'bg-green-950/60',
    border: 'border-green-800/50',
    fill: 'fill-green-400/20',
    stroke: 'stroke-green-400',
  },
  DNSKEY: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-950/60',
    border: 'border-emerald-800/50',
    fill: 'fill-emerald-400/20',
    stroke: 'stroke-emerald-400',
  },
  RRSIG: {
    text: 'text-green-300',
    bg: 'bg-green-950/50',
    border: 'border-green-900/50',
    fill: 'fill-green-300/20',
    stroke: 'stroke-green-300',
  },
  NSEC: {
    text: 'text-lime-300',
    bg: 'bg-lime-950/50',
    border: 'border-lime-900/50',
    fill: 'fill-lime-300/20',
    stroke: 'stroke-lime-300',
  },
  NSEC3: {
    text: 'text-lime-300',
    bg: 'bg-lime-950/50',
    border: 'border-lime-900/50',
    fill: 'fill-lime-300/20',
    stroke: 'stroke-lime-300',
  },
  NSEC3PARAM: {
    text: 'text-lime-400',
    bg: 'bg-lime-950/30',
    border: 'border-lime-900/40',
    fill: 'fill-lime-400/20',
    stroke: 'stroke-lime-400',
  },
  TLSA: {
    text: 'text-emerald-400',
    bg: 'bg-emerald-950/40',
    border: 'border-emerald-900/40',
    fill: 'fill-emerald-400/20',
    stroke: 'stroke-emerald-400',
  },
  SMIMEA: {
    text: 'text-teal-400',
    bg: 'bg-teal-950/40',
    border: 'border-teal-900/40',
    fill: 'fill-teal-400/20',
    stroke: 'stroke-teal-400',
  },
  CERT: {
    text: 'text-green-400',
    bg: 'bg-green-950/40',
    border: 'border-green-900/40',
    fill: 'fill-green-400/20',
    stroke: 'stroke-green-400',
  },
  SSHFP: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-950/50',
    border: 'border-emerald-800/50',
    fill: 'fill-emerald-300/20',
    stroke: 'stroke-emerald-300',
  },
  IPSECKEY: {
    text: 'text-teal-300',
    bg: 'bg-teal-950/50',
    border: 'border-teal-800/50',
    fill: 'fill-teal-300/20',
    stroke: 'stroke-teal-300',
  },
  OPENPGPKEY: {
    text: 'text-green-200',
    bg: 'bg-green-950/30',
    border: 'border-green-900/30',
    fill: 'fill-green-200/20',
    stroke: 'stroke-green-200',
  },
  CDS: {
    text: 'text-emerald-200',
    bg: 'bg-emerald-950/30',
    border: 'border-emerald-900/30',
    fill: 'fill-emerald-200/20',
    stroke: 'stroke-emerald-200',
  },
  CDNSKEY: {
    text: 'text-green-300',
    bg: 'bg-green-950/50',
    border: 'border-green-800/50',
    fill: 'fill-green-300/20',
    stroke: 'stroke-green-300',
  },
  CSYNC: {
    text: 'text-lime-200',
    bg: 'bg-lime-950/30',
    border: 'border-lime-900/30',
    fill: 'fill-lime-200/20',
    stroke: 'stroke-lime-200',
  },
  TA: {
    text: 'text-green-400',
    bg: 'bg-green-950/30',
    border: 'border-green-900/30',
    fill: 'fill-green-400/20',
    stroke: 'stroke-green-400',
  },
  DLV: {
    text: 'text-emerald-400',
    bg: 'bg-emerald-950/30',
    border: 'border-emerald-900/30',
    fill: 'fill-emerald-400/20',
    stroke: 'stroke-emerald-400',
  },

  // --- Authentication & Verification (Rose / Pinks) ---
  TXT: {
    text: 'text-rose-300',
    bg: 'bg-rose-950/60',
    border: 'border-rose-800/50',
    fill: 'fill-rose-400/20',
    stroke: 'stroke-rose-400',
  },
  SPF: {
    text: 'text-pink-300',
    bg: 'bg-pink-950/60',
    border: 'border-pink-800/50',
    fill: 'fill-pink-400/20',
    stroke: 'stroke-pink-400',
  },
  CAA: {
    text: 'text-rose-300',
    bg: 'bg-rose-950/50',
    border: 'border-rose-900/50',
    fill: 'fill-rose-300/20',
    stroke: 'stroke-rose-300',
  },
  URI: {
    text: 'text-pink-300',
    bg: 'bg-pink-950/50',
    border: 'border-pink-900/50',
    fill: 'fill-pink-300/20',
    stroke: 'stroke-pink-300',
  },
  AVC: {
    text: 'text-rose-400',
    bg: 'bg-rose-950/30',
    border: 'border-rose-900/40',
    fill: 'fill-rose-400/20',
    stroke: 'stroke-rose-400',
  },
  DOA: {
    text: 'text-pink-400',
    bg: 'bg-pink-950/30',
    border: 'border-pink-900/40',
    fill: 'fill-pink-400/20',
    stroke: 'stroke-pink-400',
  },

  // --- Metadata & Diagnostics (Orange) ---
  LOC: {
    text: 'text-orange-300',
    bg: 'bg-orange-950/60',
    border: 'border-orange-800/50',
    fill: 'fill-orange-400/20',
    stroke: 'stroke-orange-400',
  },
  HINFO: {
    text: 'text-orange-200',
    bg: 'bg-orange-950/40',
    border: 'border-orange-900/40',
    fill: 'fill-orange-300/20',
    stroke: 'stroke-orange-300',
  },
  MINFO: {
    text: 'text-amber-200',
    bg: 'bg-amber-950/40',
    border: 'border-amber-900/40',
    fill: 'fill-amber-300/20',
    stroke: 'stroke-amber-300',
  },
  RP: {
    text: 'text-amber-300',
    bg: 'bg-amber-950/40',
    border: 'border-amber-900/40',
    fill: 'fill-amber-300/20',
    stroke: 'stroke-amber-300',
  },
  ZONEMD: {
    text: 'text-orange-300',
    bg: 'bg-orange-950/40',
    border: 'border-orange-900/40',
    fill: 'fill-orange-300/20',
    stroke: 'stroke-orange-300',
  },

  // --- Security Keys & Sessions (Yellows) ---
  TKEY: {
    text: 'text-yellow-200',
    bg: 'bg-yellow-950/40',
    border: 'border-yellow-900/40',
    fill: 'fill-yellow-300/20',
    stroke: 'stroke-yellow-300',
  },
  TSIG: {
    text: 'text-yellow-300',
    bg: 'bg-yellow-950/50',
    border: 'border-yellow-800/50',
    fill: 'fill-yellow-400/20',
    stroke: 'stroke-yellow-400',
  },
  SIG: {
    text: 'text-amber-200',
    bg: 'bg-amber-950/30',
    border: 'border-amber-900/30',
    fill: 'fill-amber-200/20',
    stroke: 'stroke-amber-200',
  },
  KEY: {
    text: 'text-yellow-200',
    bg: 'bg-yellow-950/30',
    border: 'border-yellow-900/30',
    fill: 'fill-yellow-200/20',
    stroke: 'stroke-yellow-200',
  },

  // --- Zone Transfers & Special Commands (Red / Crimson) ---
  AXFR: {
    text: 'text-red-300',
    bg: 'bg-red-950/60',
    border: 'border-red-800/50',
    fill: 'fill-red-400/20',
    stroke: 'stroke-red-400',
  },
  IXFR: {
    text: 'text-red-300',
    bg: 'bg-red-950/50',
    border: 'border-red-900/50',
    fill: 'fill-red-300/20',
    stroke: 'stroke-red-300',
  },
  OPT: {
    text: 'text-rose-200',
    bg: 'bg-rose-950/40',
    border: 'border-rose-900/40',
    fill: 'fill-rose-200/20',
    stroke: 'stroke-rose-200',
  },
  ANY: {
    text: 'text-red-400',
    bg: 'bg-red-950/70',
    border: 'border-red-700/60',
    fill: 'fill-red-500/20',
    stroke: 'stroke-red-500',
  },

  // --- Obsolete, Experimental & Rare Types (Muted Gray / Slate) ---
  MD: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  MF: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  MB: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  MG: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  MR: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  NULL: {
    text: 'text-zinc-500',
    bg: 'bg-zinc-900/30',
    border: 'border-zinc-800/30',
    fill: 'fill-zinc-600/20',
    stroke: 'stroke-zinc-600',
  },
  WKS: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  AFSDB: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  X25: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  ISDN: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  RT: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  NSAP: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  'NSAP-PTR': {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  PX: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  GPOS: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  NXT: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  EID: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  NIMLOC: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  ATMA: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  A6: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  SINK: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  APL: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  DHCID: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  HIP: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  NINFO: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  RKEY: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  TALINK: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  UINFO: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  UID: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  GID: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  UNSPEC: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  NID: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  L32: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  L64: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  LP: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  EUI48: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  EUI64: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  MAILB: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  MAILA: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },
  AMTRELAY: {
    text: 'text-zinc-400',
    bg: 'bg-zinc-900/50',
    border: 'border-zinc-800/40',
    fill: 'fill-zinc-500/20',
    stroke: 'stroke-zinc-500',
  },

  // --- Fallback State ---
  UNKNOWN: {
    text: 'text-slate-400',
    bg: 'bg-slate-900/50',
    border: 'border-slate-800/50',
    fill: 'fill-slate-500/20',
    stroke: 'stroke-slate-500',
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
