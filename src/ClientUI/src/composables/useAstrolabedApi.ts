import { ref, type Ref } from 'vue'
import type { PagedResult } from '../components/types'

// ==========================================
// TYPES & INTERFACES
// ==========================================

export interface ProblemDetails {
    type?: string | null
    title?: string | null
    status?: number | string | null
    detail?: string | null
    instance?: string | null
}

export interface DiscoveredLanDeviceDto {
    ipAddress: string
    macAddress: string
    hostName: string | null
}

export interface DnsResponseEvent {
    clientIp: string
    clientName: string | null
    queryName: string
    queryType: string
    status: string
    responseIp: string | null
    timestamp: string
}

export interface CreateReservationRequest {
    mac: string
    ip: string
    clientName: string
}

export interface PaginationParams {
    pageNumber?: number
    pageSize?: number
}

export interface RangeQueryParams extends PaginationParams {
    start: string // ISO date-time string
    end: string // ISO date-time string
}

// ==========================================
// COMPOSABLE IMPLEMENTATION
// ==========================================

export function useAstrolabedApi(baseUrl = 'http://192.168.1.202:1081') {
    const loading: Ref<boolean> = ref(false)
    const error: Ref<ProblemDetails | string | null> = ref(null)

    /**
     * Universal fetch helper handling JSON serialization and error parsing
     */
    async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
        loading.value = true
        error.value = null

        try {
            const response = await fetch(`${baseUrl}${path}`, {
                ...options,
                headers: {
                    'Content-Type': 'application/json',
                    Accept: 'application/json',
                    ...options.headers,
                },
            })

            if (!response.ok) {
                let errData: ProblemDetails | string
                try {
                    errData = await response.json()
                } catch {
                    errData = response.statusText || `HTTP Error ${response.status}`
                }
                error.value = errData
                throw errData
            }

            // Return empty response for 204 No Content or empty bodies
            const text = await response.text()
            return text ? JSON.parse(text) : ({} as T)
        } catch (err: any) {
            if (!error.value) {
                error.value = err.message || 'An unexpected network error occurred'
            }
            throw err
        } finally {
            loading.value = false
        }
    }

    // ==========================================
    // DNS ENDPOINTS
    // ==========================================

    /**
     * Clears the DNS cache
     * DELETE /api/v1/Dns/cache
     */
    async function clearDnsCache(): Promise<void> {
        return apiFetch<void>('/api/v1/Dns/cache', { method: 'DELETE' })
    }

    /**
     * Executes a manual DNS query lookup
     * GET /api/v1/Dns/query
     */
    async function queryDns(name?: string, type: string = 'A'): Promise<unknown> {
        const query = new URLSearchParams()
        if (name) query.append('name', name)
        if (type) query.append('type', type)

        return apiFetch<unknown>(`/api/v1/Dns/query?${query.toString()}`, { method: 'GET' })
    }

    // ==========================================
    // DNS EVENTS ENDPOINTS
    // ==========================================

    /**
     * Gets paged list of DNS response events
     * GET /api/v1/dns-events
     */
    async function getDnsEvents(
        params: PaginationParams = {},
    ): Promise<PagedResult<DnsResponseEvent>> {
        const query = new URLSearchParams({
            pageNumber: String(params.pageNumber ?? 1),
            pageSize: String(params.pageSize ?? 100),
        })
        return apiFetch<PagedResult<DnsResponseEvent>>(`/api/v1/dns-events?${query.toString()}`, {
            method: 'GET',
        })
    }

    /**
     * Gets paged list of DNS response events filtered by date-time range
     * GET /api/v1/dns-events/range
     */
    async function getDnsEventsByRange(
        params: RangeQueryParams,
    ): Promise<PagedResult<DnsResponseEvent>> {
        const query = new URLSearchParams({
            start: params.start,
            end: params.end,
            pageNumber: String(params.pageNumber ?? 1),
            pageSize: String(params.pageSize ?? 100),
        })
        return apiFetch<PagedResult<DnsResponseEvent>>(
            `/api/v1/dns-events/range?${query.toString()}`,
            { method: 'GET' },
        )
    }

    /**
     * Gets paged list of DNS response events for a specific client IP
     * GET /api/v1/dns-events/client/{clientIp}
     */
    async function getDnsEventsByClientIp(
        clientIp: string,
        params: PaginationParams = {},
    ): Promise<PagedResult<DnsResponseEvent>> {
        const query = new URLSearchParams({
            pageNumber: String(params.pageNumber ?? 1),
            pageSize: String(params.pageSize ?? 100),
        })
        return apiFetch<PagedResult<DnsResponseEvent>>(
            `/api/v1/dns-events/client/${encodeURIComponent(clientIp)}?${query.toString()}`,
            { method: 'GET' },
        )
    }

    /**
     * Gets paged list of DNS response events filtered by query status
     * GET /api/v1/dns-events/status/{status}
     */
    async function getDnsEventsByStatus(
        status: string,
        params: PaginationParams = {},
    ): Promise<PagedResult<DnsResponseEvent>> {
        const query = new URLSearchParams({
            pageNumber: String(params.pageNumber ?? 1),
            pageSize: String(params.pageSize ?? 100),
        })
        return apiFetch<PagedResult<DnsResponseEvent>>(
            `/api/v1/dns-events/status/${encodeURIComponent(status)}?${query.toString()}`,
            { method: 'GET' },
        )
    }

    /**
     * Purges DNS events older than the given cutoff timestamp
     * DELETE /api/v1/dns-events/purge
     */
    async function purgeDnsEvents(cutoffIsoString: string): Promise<void> {
        const query = new URLSearchParams({ cutoff: cutoffIsoString })
        return apiFetch<void>(`/api/v1/dns-events/purge?${query.toString()}`, { method: 'DELETE' })
    }

    // ==========================================
    // LEASES ENDPOINTS
    // ==========================================

    /**
     * Retrieves DHCP leases
     * GET /api/v1/Leases
     */
    async function getLeases(activeOnly: boolean = true): Promise<unknown> {
        const query = new URLSearchParams({ activeOnly: String(activeOnly) })
        return apiFetch<unknown>(`/api/v1/Leases?${query.toString()}`, { method: 'GET' })
    }

    /**
     * Retrieves a specific lease by identifier
     * GET /api/v1/Leases/{identifier}
     */
    async function getLeaseByIdentifier(identifier: string): Promise<unknown> {
        return apiFetch<unknown>(`/api/v1/Leases/${encodeURIComponent(identifier)}`, {
            method: 'GET',
        })
    }

    // ==========================================
    // NETWORK ENDPOINTS
    // ==========================================

    /**
     * Gets list of discovered LAN devices
     * GET /api/Network/devices
     */
    async function getDiscoveredNetworkDevices(): Promise<DiscoveredLanDeviceDto[]> {
        return apiFetch<DiscoveredLanDeviceDto[]>('/api/Network/devices', { method: 'GET' })
    }

    // ==========================================
    // RESERVATIONS ENDPOINTS
    // ==========================================

    /**
     * Creates a new static IP reservation
     * POST /api/v1/Reservations
     */
    async function createReservation(payload: CreateReservationRequest): Promise<void> {
        return apiFetch<void>('/api/v1/Reservations', {
            method: 'POST',
            body: JSON.stringify(payload),
        })
    }

    /**
     * Retrieves a reservation by MAC address
     * GET /api/v1/Reservations/{mac}
     */
    async function getReservationByMac(mac: string): Promise<unknown> {
        return apiFetch<unknown>(`/api/v1/Reservations/${encodeURIComponent(mac)}`, {
            method: 'GET',
        })
    }

    // ==========================================
    // TIME ENDPOINTS
    // ==========================================

    /**
     * Gets current NTP / time server status
     * GET /api/v1/Time/ntp
     */
    async function getNtpTime(): Promise<unknown> {
        return apiFetch<unknown>('/api/v1/Time/ntp', { method: 'GET' })
    }

    return {
        loading,
        error,
        // DNS
        clearDnsCache,
        queryDns,
        // DNS Events
        getDnsEvents,
        getDnsEventsByRange,
        getDnsEventsByClientIp,
        getDnsEventsByStatus,
        purgeDnsEvents,
        // Leases
        getLeases,
        getLeaseByIdentifier,
        // Network
        getDiscoveredNetworkDevices,
        // Reservations
        createReservation,
        getReservationByMac,
        // Time
        getNtpTime,
    }
}
