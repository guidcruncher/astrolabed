import { useFetchClient } from './useFetchClient'
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
  DnsBenchmarkResult,
  DnsBenchmark,
  DnsServiceRanking,
  BlockRateResponse,
  RetryStormResult,
  CnameCloakingResult,
  DhcpLease,
  AllocateOrUpdateDhcpLeaseRequest,
  ReleaseDhcpLeaseRequest,
  DnsResponseEventEntity,
  DiscoveredLanDeviceDto,
  DnsWireMessage,
  DnsType,
  BlockingStatusResponse,
  DisableBlockingRequest,
  DomainMatchResponse,
  PagedResultOfFilterRuleDto,
  FilterRuleDto,
  DnsListEntity,
} from '../types/api'

export function useApi() {
  const { apiBaseUrl, loading, error, request } = useFetchClient()

  // --- Astrolabed ---
  const getStatus = (): Promise<AstrolabedStatusResponse> =>
    request<AstrolabedStatusResponse>('/api/Astrolabed/status')

  // --- Stats ---
  const getDnsHourlyEventSummary = (
    startEpoch = 0,
    endEpoch = 0
  ): Promise<DnsHourlyEventSummary[]> =>
    request<DnsHourlyEventSummary[]>(
      `/api/stats/dns/hourly?startepoch=${startEpoch}&endepoch=${endEpoch}`
    )

  const getDnsQuestionTypeSummary = (
    startEpoch = 0,
    endEpoch = 0
  ): Promise<DnsQuestionTypeSummary[]> =>
    request<DnsQuestionTypeSummary[]>(
      `/api/stats/dns/question-types?startepoch=${startEpoch}&endepoch=${endEpoch}`
    )

  // --- DNS Analytics ---
  const getBlockRate = (startTimeUtc?: number): Promise<BlockRateResponse> => {
    const query = new URLSearchParams()
    if (startTimeUtc !== undefined) query.append('startTimeUtc', startTimeUtc.toString())
    return request<BlockRateResponse>(`/api/dns/analytics/block-rate?${query.toString()}`)
  }

  const getRetryStorms = (startTimeUtc?: number, limit = 50): Promise<RetryStormResult[]> => {
    const query = new URLSearchParams()
    if (startTimeUtc !== undefined) query.append('startTimeUtc', startTimeUtc.toString())
    query.append('limit', limit.toString())
    return request<RetryStormResult[]>(`/api/dns/analytics/retry-storms?${query.toString()}`)
  }

  const getCnameCloaking = (): Promise<CnameCloakingResult[]> =>
    request<CnameCloakingResult[]>('/api/dns/analytics/cname-cloaking')

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

  const getDnsBenchmarks = (): Promise<DnsBenchmarkResult> =>
    request<DnsBenchmarkResult>('/api/benchmarks')

  const getDnsBenchmarkByName = (serverName: string): Promise<DnsBenchmarkResult> =>
    request<DnsBenchmarkResult>(`/api/benchmarks/${encodeURIComponent(serverName)}`)

  const getDnsBenchmarkRankings = (): Promise<DnsServiceRanking[]> =>
    request<DnsServiceRanking[]>(`/api/benchmarks/metrics`)

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

  // --- Blocking & Filtering ---
  const getBlockingStatus = (): Promise<BlockingStatusResponse> =>
    request<BlockingStatusResponse>('/api/Blocking/status')

  const disableBlocking = (payload: DisableBlockingRequest): Promise<void> =>
    request<void>('/api/Blocking/disable', {
      method: 'POST',
      body: JSON.stringify(payload),
    })

  const enableBlocking = (): Promise<void> =>
    request<void>('/api/Blocking/enable', { method: 'POST' })

  const matchDomain = (domain: string): Promise<DomainMatchResponse> =>
    request<DomainMatchResponse>(`/api/Filtering/match?domain=${encodeURIComponent(domain)}`)

  const getFilterRules = (
    listId = 0,
    pageNumber = 1,
    pageSize = 10
  ): Promise<PagedResultOfFilterRuleDto> =>
    request<PagedResultOfFilterRuleDto>(
      `/api/Filtering/rules?pageNumber=${pageNumber}&pageSize=${pageSize}&listId=${listId}`
    )

  const addFilterRule = (payload: FilterRuleDto): Promise<void> =>
    request<void>('/api/Filtering/rules', {
      method: 'POST',
      body: JSON.stringify(payload),
    })

  const deleteFilterRule = (pattern: string): Promise<void> =>
    request<void>(`/api/Filtering/rules?pattern=${encodeURIComponent(pattern)}`, {
      method: 'DELETE',
    })

  const getLists = (): Promise<DnsListEntity[]> => request<DnsListEntity[]>('/api/Lists')

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
    getDnsBenchmarkByName,
    getDnsBenchmarkRankings,
    getBlockRate,
    getRetryStorms,
    getCnameCloaking,
    getDnsHourlyEventSummary,
    getDnsQuestionTypeSummary,
    getBlockingStatus,
    disableBlocking,
    enableBlocking,
    matchDomain,
    getFilterRules,
    addFilterRule,
    deleteFilterRule,
    getLists,
  }
}
