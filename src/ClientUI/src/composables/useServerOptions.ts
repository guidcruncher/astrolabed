import { ref, readonly, type Ref } from 'vue'

export interface ServerOptions {
    dns: DnsOptions
    dhcp: DhcpOptions
    ntp: NtpOptions
    metrics: MetricsOptions
    webUI: WebUiOptions
    dbOptions: DbOptions
    networkScanner: NetworkScannerOptions
    logging: LoggingOptions
}

export interface LoggingOptions {
    level: string
}

export interface DnsOptions {
    listen: Listen
    upstreamTimeoutMs: number
    defaultResolvers: DefaultResolver[]
    resolvers: Resolver[]
    blocklists: any[]
    allowlists: any[]
    hostsFiles: string[]
    caching: Caching
    blockResponse: BlockResponse
    conditionalForwarding: ConditionalForwarding
}

export interface Listen {
    address: string
    port: number
}

export interface DefaultResolver {
    name: string
    address: string
    port: number
}

export interface Resolver {
    name: string
    address?: string
    port?: number
    rule: string
    block: boolean
}

export interface Caching {
    enabled: boolean
    ttlSeconds: number
    maxEntries: number
    cleanupIntervalMinutes: number
}

export interface BlockResponse {
    mode: string
    staticIp: string
    ttl: number
}

export interface ConditionalForwarding {
    enabled: boolean
    dhcpServerIp: string
    dhcpServerPort: number
    localDomain: string
    localSubnetCidr: string
    forwardNonFqdn: boolean
}

export interface DhcpOptions {
    enabled: boolean
    listenAddress: string
    listenPort: number
    leaseStorePath: string
    badIpStorePath: string
    poolCidr: string
    serverIdentifier: string
    router: string
    dnsServer: string
    ntpServer: string
    domainName: string
    interfaceMtu: number
    tftpServerName: string
    bootfileName: string
    webProxyServerUrl: string
    leaseHours: number
    arpTimeoutMs: number
}

export interface NtpOptions {
    enabled: boolean
    listenAddress: string
    port: number
    bufferSize: number
    stratum: number
    referenceId: string
    upstream: Upstream
}

export interface Upstream {
    enabled: boolean
    servers: string[]
    pollIntervalSeconds: number
}

export interface MetricsOptions {
    enabled: boolean
    storageEngine: string
    location: string
    listenAddress: string
    listenPort: number
}

export interface WebUiOptions {
    enabled: boolean
    listenAddress: string
    listenPort: number
}

export interface DbOptions {
    provider: string
    connectionString: string
}

export interface NetworkScannerOptions {
    maxDegreeOfParallelism: number
    pingTimeoutMs: number
}

export function useServerOptions(apiBaseUrl: string = '/api/configuration') {
    const options = ref<ServerOptions | null>(null)
    const isLoading = ref<boolean>(false)
    const error = ref<Error | null>(null)

    /**
     * Fetches current application configuration from the API.
     */
    const fetchOptions = async (): Promise<ServerOptions | null> => {
        isLoading.value = true
        error.value = null

        try {
            const response = await fetch(apiBaseUrl, {
                method: 'GET',
                headers: {
                    Accept: 'application/json',
                },
            })

            if (!response.ok) {
                throw new Error(
                    `Failed to fetch configuration: ${response.status} ${response.statusText}`,
                )
            }

            const data: ServerOptions = await response.json()
            options.value = data
            return data
        } catch (err: any) {
            const fetchError = err instanceof Error ? err : new Error(String(err))
            error.value = fetchError
            return null
        } finally {
            isLoading.value = false
        }
    }

    /**
     * Sends updated ServerOptions configuration back to the API.
     * @param newOptions Optional configuration object. Defaults to current reactivity payload.
     */
    const updateOptions = async (newOptions?: ServerOptions): Promise<boolean> => {
        isLoading.value = true
        error.value = null

        const payload = newOptions ?? options.value

        if (!payload) {
            error.value = new Error('No configuration payload available to update.')
            isLoading.value = false
            return false
        }

        try {
            const response = await fetch(apiBaseUrl, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    Accept: 'application/json',
                },
                body: JSON.stringify(payload),
            })

            if (!response.ok) {
                throw new Error(
                    `Failed to update configuration: ${response.status} ${response.statusText}`,
                )
            }

            // Sync updated payload locally on success
            options.value = payload
            return true
        } catch (err: any) {
            const updateError = err instanceof Error ? err : new Error(String(err))
            error.value = updateError
            return false
        } finally {
            isLoading.value = false
        }
    }

    return {
        options,
        isLoading: readonly(isLoading) as Ref<boolean>,
        error: readonly(error) as Ref<Error | null>,
        fetchOptions,
        updateOptions,
    }
}
