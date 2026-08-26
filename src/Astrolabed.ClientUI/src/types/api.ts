export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
}

export interface AstrolabedStatusResponse {
  status: string;
  timestamp: string;
}

export interface CacheCountResponse {
  count: number;
  timestamp: string;
}

export interface CacheEntry {
  payload?: string;
  expiresAt: string;
  isExpired?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageSize: number;
  totalPages?: number;
  pageNumber?: number;
  currentPage?: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

export interface IPAddress {
  addressFamily?: number;
  scopeId?: number;
  isIPv6Multicast?: boolean;
  isIPv6LinkLocal?: boolean;
  isIPv6SiteLocal?: boolean;
  isIPv6Teredo?: boolean;
  isIPv6UniqueLocal?: boolean;
  isIPv4MappedToIPv6?: boolean;
  address?: number;
}

export interface DhcpLease {
  clientId: string;
  clientName: string;
  macAddress: string;
  ipAddress?: IPAddress | string;
  leaseStartTime?: string;
  leaseEndTime?: string;
  isActive?: boolean;
}

export interface AllocateOrUpdateDhcpLeaseRequest {
  clientId: string;
  clientName: string;
  macAddress: string;
  requestedIp: string;
  durationInSeconds: number;
}

export interface ReleaseDhcpLeaseRequest {
  clientId: string;
  macAddress: string;
}

export interface DnsResponseEventEntity {
  id?: string;
  startTimeUtc?: number;
  contextId?: string;
  questionName?: string;
  questionType?: string;
  clientEndpoint?: string;
  clientName?: string;
  resolutionSource?: string;
  durationMs?: number;
  blocked?: number;
}

export interface DiscoveredLanDeviceDto {
  macAddress: string;
  ipAddress: string;
  hostName: string | null;
  lastSeen: string;
}

export interface CleanupStaleDevicesRequest {
  cutoff: string;
}
