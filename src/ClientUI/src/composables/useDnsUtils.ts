import { computed, isRef, type MaybeRef } from 'vue'

export const DNS_STATUS_MAP: Record<number, string> = {
    0: 'NOERROR',
    1: 'FORMERR',
    2: 'SERVFAIL',
    3: 'NXDOMAIN',
    4: 'NOTIMP',
    5: 'REFUSED',
    6: 'YXDOMAIN',
    7: 'YXRRSET',
    8: 'NXRRSET',
    9: 'NOTAUTH',
    10: 'NOTZONE',
}

export type DnsStatusValue = string | number | undefined | null

export function useDnsUtils() {
    /**
     * Helper function to convert a raw DNS status value into a readable label.
     */
    const getDnsStatusLabel = (status: DnsStatusValue): string => {
        if (status === null || status === undefined) return 'UNKNOWN'

        if (typeof status === 'number') {
            return DNS_STATUS_MAP[status] || `RCODE_${status}`
        }

        const parsed = parseInt(status, 10)
        if (!isNaN(parsed) && String(parsed) === status.trim()) {
            return DNS_STATUS_MAP[parsed] || `RCODE_${parsed}`
        }

        return status
    }

    /**
     * Creates a reactive computed reference for a dynamic status variable or Ref.
     */
    const formatDnsStatus = (statusRef: MaybeRef<DnsStatusValue>) => {
        return computed(() => {
            const rawStatus = isRef(statusRef) ? statusRef.value : statusRef
            return getDnsStatusLabel(rawStatus)
        })
    }

    return {
        DNS_STATUS_MAP,
        getDnsStatusLabel,
        formatDnsStatus,
    }
}
