export interface DnsBenchmark {
  rank: number
  serverName: string
  combinedAverageLatencyMs: number
  minLatencyMs: number
  maxLatencyMs: number
  combinedPacketLossPercentage: number
  endpointsCount: number
}

export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number | null
  detail?: string | null
  instance?: string | null
}

export interface AstrolabedStatusResponse {
  status: string
  timestamp: string
}

export interface CacheCountResponse {
  count: number
  timestamp: string
}

export type AddressFamily = number
export type DnsOpCode = number
export type DnsResponseCode = number
export type DnsType = number

export interface IPAddress {
  addressFamily?: AddressFamily
  scopeId?: number
  isIPv6Multicast?: boolean
  isIPv6LinkLocal?: boolean
  isIPv6SiteLocal?: boolean
  isIPv6Teredo?: boolean
  isIPv6UniqueLocal?: boolean
  isIPv4MappedToIPv6?: boolean
  address?: number
}

export interface EdnsOptionCode {
  code?: number
  data?: string
}

export interface EdnsOptions {
  udpPayloadSize?: number
  extendedRCode?: number
  version?: number
  dnssecOk?: boolean
  options?: EdnsOptionCode[] | null
}

export interface DnsResourceRecord {
  name?: string
  type?: DnsType
  class?: number
  ttl?: number
  data?: string
  parsedIp?: string | null
}

export interface DnsWireMessage {
  transactionId?: number
  isResponse?: boolean
  opCode?: DnsOpCode
  authoritativeAnswer?: boolean
  truncated?: boolean
  recursionDesired?: boolean
  recursionAvailable?: boolean
  responseCode?: DnsResponseCode
  questionName?: string
  questionType?: DnsType
  questionClass?: number
  answers?: DnsResourceRecord[] | null
  authorities?: DnsResourceRecord[] | null
  additionals?: DnsResourceRecord[] | null
  edns?: EdnsOptions | null
}

export interface CacheEntryView {
  payload: DnsWireMessage
  expiresAt: string
  isExpired?: boolean
}

export interface DhcpLease {
  clientId: string
  clientName: string
  macAddress: string
  ipAddress: IPAddress
  leaseStartTime?: string
  leaseEndTime?: string
  isActive?: boolean
}

export interface AllocateOrUpdateDhcpLeaseRequest {
  clientId: string
  clientName: string
  macAddress: string
  requestedIp: string
  durationInSeconds: number
}

export interface ReleaseDhcpLeaseRequest {
  clientId: string
  macAddress: string
}

export interface DnsResponseEventEntity {
  id?: string
  startTimeUtc?: number
  contextId?: string
  questionName?: string
  questionType?: string
  clientEndpoint?: string
  clientName?: string
  resolutionSource?: string
  durationMs?: number
  blocked?: number
  upstream: string
}

export interface DiscoveredLanDeviceDto {
  macAddress: string
  ipAddress: string
  hostName: string | null
  lastSeen: string
  vendor: string
  deviceType: string
}

export interface CleanupStaleDevicesRequest {
  cutoff: string
}

export interface PagedResultDtoOfDhcpLease {
  items: DhcpLease[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface PagedResultOfCacheEntryView {
  items: CacheEntryView[]
  totalCount: number
  pageSize: number
  totalPages?: number
  currentPage?: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
}

export interface PagedResultOfDiscoveredLanDeviceDto {
  items: DiscoveredLanDeviceDto[]
  totalCount: number
  pageSize: number
  totalPages?: number
  currentPage?: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
}

export interface PagedResultOfDnsResponseEventEntity {
  items: DnsResponseEventEntity[]
  totalCount: number
  pageSize: number
  totalPages?: number
  currentPage?: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
}
