import { ref } from 'vue'
import type {
  AstrolabedStatusResponse,
  DnsHourlyEventSummary,
  DnsQuestionTypeSummary,
  CacheCountResponse,
  CacheEntryView,
  PagedResultOfCacheEntryView,
  PagedResultDtoOfDhcpLease,
  PagedResultOfDnsResponseEventEntity,
  PagedResultOfDiscoveredLanDeviceDto,
  DnsBenchmark,
  DhcpLease,
  AllocateOrUpdateDhcpLeaseRequest,
  ReleaseDhcpLeaseRequest,
  DnsResponseEventEntity,
  DiscoveredLanDeviceDto,
  DnsWireMessage,
  DnsType,
  ProblemDetails,
} from '../types/api'

const apiBaseUrl = ref<string>('')

export function useApi() {
  const loading = ref<boolean>(false)
  const error = ref<string | null>(null)

  const request = async <T>(endpoint: string, options: RequestInit = {}): Promise<T> => {
    loading.value = true
    error.value = null
    try {
      const baseUrl = apiBaseUrl.value.endsWith('/')
        ? apiBaseUrl.value.slice(0, -1)
        : apiBaseUrl.value
      const path = endpoint.startsWith('/') ? endpoint : `/${endpoint}`
      const url = `${baseUrl}${path}`

      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          ...options.headers,
        },
        ...options,
      })

      if (!response.ok) {
        const problem: ProblemDetails | null = await response.json().catch(() => null)
        throw new Error(
          problem?.detail || problem?.title || `HTTP ${response.status}: ${response.statusText}`
        )
      }

      if (response.status === 204) {
        return null as unknown as T
      }

      return await response.json()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred.'
      error.value = message
      throw err
    } finally {
      loading.value = false
    }
  }

  // --- Astrolabed ---
  const getStatus = (): Promise<AstrolabedStatusResponse> =>
    request<AstrolabedStatusResponse>('/api/Astrolabed/status')

  // --- Stats ---
  const getDnsHourlyEventSummary = (): Promise<DnsHourlyEventSummary[]> =>
    request<DnsHourlyEventSummary[]>('/api/stats/dns/hourly')

  const getDnsQuestionTypeSummary = (): Promise<DnsQuestionTypeSummary[]> =>
    request<DnsQuestionTypeSummary[]>('/api/stats/dns/question-types')

  // --- Cache ---
  const getCacheCount = (): Promise<CacheCountResponse> =>
    request<CacheCountResponse>('/api/Cache/count')

  const getCacheEntries = (pageNumber = 1, pageSize = 10): Promise<PagedResultOfCacheEntryView> =>
    request<PagedResultOfCacheEntryView>(`/api/Cache?pageNumber=${pageNumber}&pageSize=${pageSize}`)

  const clearCache = (): Promise<void> => request<void>('/api/Cache', { method: 'DELETE' })

  // --- DHCP ---
  const getDhcpLeases = (pageNumber = 1, pageSize = 10): Promise<PagedResultDtoOfDhcpLease> =>
    request<PagedResultDtoOfDhcpLease>(
      `/api/Dhcp/leases?pageNumber=${pageNumber}&pageSize=${pageSize}`
    )

  const getDhcpLease = (params: { clientId?: string; macAddress?: string }): Promise<DhcpLease> => {
    const query = new URLSearchParams()
    if (params.clientId) query.append('clientId', params.clientId)
    if (params.macAddress) query.append('macAddress', params.macAddress)
    return request<DhcpLease>(`/api/Dhcp/lease?${query.toString()}`)
  }

  const getDhcpLeaseByPtr = (ptrAddress: string): Promise<DhcpLease> =>
    request<DhcpLease>(`/api/Dhcp/lease/ptr?ptrAddress=${encodeURIComponent(ptrAddress)}`)

  const getDhcpLeaseByIp = (ipAddress: string): Promise<DhcpLease> =>
    request<DhcpLease>(`/api/Dhcp/lease/ip/${encodeURIComponent(ipAddress)}`)

  const checkDhcpAvailability = (ipAddress: string, clientId: string): Promise<boolean> =>
    request<boolean>(
      `/api/Dhcp/availability?ipAddress=${encodeURIComponent(ipAddress)}&clientId=${encodeURIComponent(clientId)}`
    )

  const allocateDhcpLease = (payload: AllocateOrUpdateDhcpLeaseRequest): Promise<DhcpLease> =>
    request<DhcpLease>('/api/Dhcp/lease', { method: 'POST', body: JSON.stringify(payload) })

  const releaseDhcpLease = (payload: ReleaseDhcpLeaseRequest): Promise<void> =>
    request<void>('/api/Dhcp/lease/release', { method: 'POST', body: JSON.stringify(payload) })

  // --- DNS ---
  const queryDns = (domain: string, type?: DnsType): Promise<DnsWireMessage> => {
    const query = new URLSearchParams({ domain })
    if (type !== undefined) {
      query.append('type', type.toString())
    }
    return request<DnsWireMessage>(`/api/dns/query?${query.toString()}`)
  }

  const getDnsEvents = (
    pageNumber = 1,
    pageSize = 10
  ): Promise<PagedResultOfDnsResponseEventEntity> =>
    request<PagedResultOfDnsResponseEventEntity>(
      `/api/Dns?pageNumber=${pageNumber}&pageSize=${pageSize}`
    )

  const getDnsEventById = (id: string): Promise<DnsResponseEventEntity> =>
    request<DnsResponseEventEntity>(`/api/Dns/${encodeURIComponent(id)}`)

  const getDnsBenchmarks = (): Promise<DnsBenchmark[]> =>
    request<DnsBenchmark[]>(`/api/benchmarks/metrics`)

  const purgeDnsEvents = (): Promise<void> => request<void>('/api/Dns', { method: 'DELETE' })

  // --- Network ---
  const getNetworkDevices = (
    pageNumber = 1,
    pageSize = 10
  ): Promise<PagedResultOfDiscoveredLanDeviceDto> =>
    request<PagedResultOfDiscoveredLanDeviceDto>(
      `/api/Network/devices?pageNumber=${pageNumber}&pageSize=${pageSize}`
    )

  const getNetworkDeviceByMac = (macAddress: string): Promise<DiscoveredLanDeviceDto> =>
    request<DiscoveredLanDeviceDto>(`/api/Network/devices/mac/${encodeURIComponent(macAddress)}`)

  const getNetworkDeviceByIp = (ipAddress: string): Promise<DiscoveredLanDeviceDto> =>
    request<DiscoveredLanDeviceDto>(`/api/Network/devices/ip/${encodeURIComponent(ipAddress)}`)

  const getNetworkDeviceByPtr = (ptrAddress: string): Promise<DiscoveredLanDeviceDto> =>
    request<DiscoveredLanDeviceDto>(
      `/api/Network/devices/ptr?ptrAddress=${encodeURIComponent(ptrAddress)}`
    )

  const cleanupStaleDevices = (cutoffIsoDate: string): Promise<void> =>
    request<void>('/api/Network/devices/cleanup', {
      method: 'POST',
      body: JSON.stringify({ cutoff: cutoffIsoDate }),
    })

  // --- Time ---
  const getCurrentTime = (): Promise<string> => request<string>('/api/Time')

  return {
    apiBaseUrl,
    loading,
    error,
    getStatus,
    getCacheCount,
    getCacheEntries,
    clearCache,
    getDhcpLeases,
    getDhcpLease,
    getDhcpLeaseByPtr,
    getDhcpLeaseByIp,
    checkDhcpAvailability,
    allocateDhcpLease,
    releaseDhcpLease,
    queryDns,
    getDnsEvents,
    getDnsEventById,
    purgeDnsEvents,
    getNetworkDevices,
    getNetworkDeviceByMac,
    getNetworkDeviceByIp,
    getNetworkDeviceByPtr,
    cleanupStaleDevices,
    getCurrentTime,
    getDnsBenchmarks,
    getDnsHourlyEventSummary,
    getDnsQuestionTypeSummary,
  }
}
