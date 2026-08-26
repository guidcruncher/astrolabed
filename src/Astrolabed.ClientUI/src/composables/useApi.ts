import { ref } from 'vue'
import type {
  AstrolabedStatusResponse,
  CacheCountResponse,
  CacheEntry,
  PagedResult,
  DhcpLease,
  AllocateOrUpdateDhcpLeaseRequest,
  ReleaseDhcpLeaseRequest,
  DnsResponseEventEntity,
  DiscoveredLanDeviceDto,
  ProblemDetails
} from '../types/api'

const apiBaseUrl = '/';

export function useApi() {
  const loading = ref<boolean>(false)
  const error = ref<string | null>(null)

  const request = async <T>(endpoint: string, options: RequestInit = {}): Promise<T> => {
    loading.value = true
    error.value = null
    try {
      const url = `${endpoint}`
      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          ...options.headers
        },
        ...options
      })

      if (!response.ok) {
        const problem: ProblemDetails | null = await response.json().catch(() => null)
        throw new Error(problem?.detail || problem?.title || `HTTP ${response.status}: ${response.statusText}`)
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

  const getStatus = (): Promise<AstrolabedStatusResponse> =>
    request<AstrolabedStatusResponse>('/api/Astrolabed/status')

  const getCacheCount = (): Promise<CacheCountResponse> =>
    request<CacheCountResponse>('/api/Cache/count')

  const getCacheEntries = (pageNumber = 1, pageSize = 10): Promise<PagedResult<CacheEntry>> =>
    request<PagedResult<CacheEntry>>(`/api/Cache?pageNumber=${pageNumber}&pageSize=${pageSize}`)

  const clearCache = (): Promise<void> =>
    request<void>('/api/Cache', { method: 'DELETE' })

  const getDhcpLeases = (pageNumber = 1, pageSize = 10): Promise<PagedResult<DhcpLease>> =>
    request<PagedResult<DhcpLease>>(`/api/Dhcp/leases?pageNumber=${pageNumber}&pageSize=${pageSize}`)

  const allocateDhcpLease = (payload: AllocateOrUpdateDhcpLeaseRequest): Promise<DhcpLease> =>
    request<DhcpLease>('/api/Dhcp/lease', { method: 'POST', body: JSON.stringify(payload) })

  const releaseDhcpLease = (payload: ReleaseDhcpLeaseRequest): Promise<void> =>
    request<void>('/api/Dhcp/lease/release', { method: 'POST', body: JSON.stringify(payload) })

  const getDnsEvents = (pageNumber = 1, pageSize = 10): Promise<PagedResult<DnsResponseEventEntity>> =>
    request<PagedResult<DnsResponseEventEntity>>(`/api/Dns?pageNumber=${pageNumber}&pageSize=${pageSize}`)

  const purgeDnsEvents = (): Promise<void> =>
    request<void>('/api/Dns', { method: 'DELETE' })

  const getNetworkDevices = (pageNumber = 1, pageSize = 10): Promise<PagedResult<DiscoveredLanDeviceDto>> =>
    request<PagedResult<DiscoveredLanDeviceDto>>(`/api/Network/devices?pageNumber=${pageNumber}&pageSize=${pageSize}`)

  const cleanupStaleDevices = (cutoffIsoDate: string): Promise<void> =>
    request<void>('/api/Network/devices/cleanup', {
      method: 'POST',
      body: JSON.stringify({ cutoff: cutoffIsoDate })
    })

  return {
    apiBaseUrl,
    loading,
    error,
    getStatus,
    getCacheCount,
    getCacheEntries,
    clearCache,
    getDhcpLeases,
    allocateDhcpLease,
    releaseDhcpLease,
    getDnsEvents,
    purgeDnsEvents,
    getNetworkDevices,
    cleanupStaleDevices
  }
}
