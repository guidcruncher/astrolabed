import { ref, readonly, type Ref } from 'vue'

export interface ServerOptions {
    Dns: DnsOptions
    Dhcp: DhcpOptions
    Ntp: NtpOptions
    Metrics: MetricsOptions
    WebUI: WebUiOptions
    DbOptions: DbOptions
    NetworkScanner: NetworkScannerOptions
    Logging: LoggingOptions
}

export interface DnsOptions {
    Listen: Listen
    UpstreamTimeoutMs: number
    DefaultResolvers: DefaultResolver[]
    Resolvers: Resolver[]
    Blocklists: any[]
    Allowlists: any[]
    HostsFiles: string[]
    Caching: Caching
    BlockResponse: BlockResponse
    ConditionalForwarding: ConditionalForwarding
}

export interface Listen {
    Address: string
    Port: number
}

export interface DefaultResolver {
    Name: string
    Address: string
    Port: number
}

export interface Resolver {
    Name: string
    Address?: string
    Port?: number
    Rule: string
    Block: boolean
}

export interface Caching {
    Enabled: boolean
    TtlSeconds: number
    MaxEntries: number
    CleanupIntervalMinutes: number
}

export interface BlockResponse {
    Mode: string
    StaticIp: string
    Ttl: number
}

export interface ConditionalForwarding {
    Enabled: boolean
    DhcpServerIp: string
    DhcpServerPort: number
    LocalDomain: string
    LocalSubnetCidr: string
    ForwardNonFqdn: boolean
}

export interface DhcpOptions {
    Enabled: boolean
    ListenAddress: string
    ListenPort: number
    LeaseStorePath: string
    BadIpStorePath: string
    PoolCidr: string
    ServerIdentifier: string
    Router: string
    DnsServer: string
    NtpServer: string
    DomainName: string
    InterfaceMtu: number
    TftpServerName: string
    BootfileName: string
    WebProxyServerUrl: string
    LeaseHours: number
    ArpTimeoutMs: number
}

export interface NtpOptions {
    Enabled: boolean
    ListenAddress: string
    Port: number
    BufferSize: number
    Stratum: number
    ReferenceId: string
    Upstream: Upstream
}

export interface Upstream {
    Enabled: boolean
    Servers: string[]
    PollIntervalSeconds: number
}

export interface MetricsOptions {
    Enabled: boolean
    StorageEngine: string
    Location: string
    ListenAddress: string
    ListenPort: number
}

export interface WebUiOptions {
    Enabled: boolean
    ListenAddress: string
    ListenPort: number
}

export interface DbOptions {
    DatabaseProvider: string
    ConnectionString: string
}

export interface NetworkScannerOptions {
    MaxDegreeOfParallelism: number
    PingTimeoutMs: number
}

export interface LoggingOptions {
    Level: string
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
