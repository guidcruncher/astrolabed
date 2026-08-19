# Architecture & Flow Diagrams

Below are simple mermaid diagrams illustrating the high-level architecture and request flows for DNS, DHCP and NTP.

## High-level component diagram

```mermaid
graph TD
    classDef boundaryStyle fill:#0f172a,stroke:#38bdf8,stroke-width:2px,color:#fff
    classDef coreStyle fill:#1e1b4b,stroke:#818cf8,stroke-width:2px,color:#fff
    classDef engineStyle fill:#111827,stroke:#34d399,stroke-width:2px,color:#fff
    classDef dataStyle fill:#1f2937,stroke:#fbbf24,stroke-width:2px,color:#fff
    classDef extStyle fill:#1e293b,stroke:#94a3b8,stroke-width:2px,color:#fff

    subgraph Transport ["Network Inbound Interface"]
        UdpListener["UDP Server Listener"] :::boundaryStyle
        TcpListener["TCP Server Listener"] :::boundaryStyle
        DohListener["DoH / HTTPS Endpoint"] :::boundaryStyle
    end

    subgraph Pipeline ["DnsPipeline (Request / Response Pipeline)"]
        ContextBuilder["Context Builder & Decoder"] :::engineStyle
        RespBuilder["Response Builder & Encoder"] :::engineStyle
    end

    subgraph CoreEngine ["RuleEngine (Core Resolution Hub)"]
        MatchHub["Rule Matcher Hub"] :::coreStyle
        ExecHub["Query Executor"] :::coreStyle
        
        subgraph Snapshot ["State Snapshot (Lock-Free Read)"]
            HostsTable["Hosts Dictionary"] :::dataStyle
            RulesAutomata["Rule Compiler & Automata"] :::dataStyle
            UpstreamChain["Upstream Chain Builder"] :::dataStyle
        end
    end

    subgraph SystemServices ["Cross-Cutting Services"]
        Cache["IDnsCache (DNS Cache)"] :::engineStyle
        Metrics["IDnsMetrics (Telemetry & Events)"] :::engineStyle
        Options["IOptionsMonitor (Configuration)"] :::engineStyle
    end

    subgraph DynamicLoaders ["Background State Loaders"]
        HostsLoader["Hosts File Sources"] :::dataStyle
        BlocklistLoader["Blocklist Sources"] :::dataStyle
    end

    subgraph UpstreamResolvers ["External DNS Upstreams"]
        PrimaryDns["Primary Upstream (e.g. DoH / DoT)"] :::extStyle
        FallbackDns["Fallback Upstream (e.g. UDP)"] :::extStyle
    end

    %% Flow Connections
    Transport --> ContextBuilder
    ContextBuilder --> MatchHub

    MatchHub <--> Cache
    MatchHub -. Reads .-> Snapshot
    
    HostsLoader -- Async Swap --> Snapshot
    BlocklistLoader -- Async Swap --> Snapshot
    Options -. Reload .-> Snapshot

    MatchHub --> ExecHub
    ExecHub --> PrimaryDns
    ExecHub --> FallbackDns

    ExecHub --> RespBuilder
    MatchHub -- Blocked/Cached --> RespBuilder
    
    RespBuilder --> Metrics
    RespBuilder --> Transport
```

## DNS request sequence

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client App / Host
    participant RuleEngine as Astrolabed DNS Engine
    participant Cache as IDnsCache
    participant Snapshot as State Snapshot (Rules/Hosts)
    participant Upstream as External Upstream DNS

    Note over Client, Upstream: --- Scenario 1: Allowed Request (Cache Miss -> Forwarded) ---
    Client->>RuleEngine: 1. Send DNS Query (e.g., example.com)
    RuleEngine->>Cache: 2. TryGet(context)
    Cache-->>RuleEngine: 3. Cache MISS
    RuleEngine->>Snapshot: 4. Match("example.com")
    Snapshot-->>RuleEngine: 5. RuleResult (Block = False, Upstreams = [PrimaryDNS])
    
    RuleEngine->>Upstream: 6. Forward Query
    Upstream-->>RuleEngine: 7. DNS Response Payload (IP: 93.184.216.34)
    RuleEngine->>Cache: 8. Store response with TTL
    RuleEngine-->>Client: 9. Return Response (A 93.184.216.34)

    Note over Client, Upstream: --- Scenario 2: Blocked Request (Match Blocklist Rule) ---
    Client->>RuleEngine: 10. Send DNS Query (e.g., ad.tracker.com)
    RuleEngine->>Cache: 11. TryGet(context)
    Cache-->>RuleEngine: 12. Cache MISS
    RuleEngine->>Snapshot: 13. Match("ad.tracker.com")
    Snapshot-->>RuleEngine: 14. RuleResult (Block = True)
    
    Note over RuleEngine: Construct Block Response<br/>(NXDOMAIN / Refused / 0.0.0.0 based on Mode)
    RuleEngine-->>Client: 15. Return Block Response (e.g., NXDOMAIN + EDE Code 15)

    Note over Client, Upstream: --- Scenario 3: Cached Allowed Request (Cache Hit) ---
    Client->>RuleEngine: 16. Send DNS Query (e.g., example.com)
    RuleEngine->>Cache: 17. TryGet(context)
    Cache-->>RuleEngine: 18. Cache HIT (Payload found)
    RuleEngine-->>Client: 19. Immediate Return Cached Response (No Upstream/Rule Evaluation)
```

## DHCP flow (simplified)

```mermaid
sequenceDiagram
    autonumber
    actor Client as DHCP Client
    participant Server as DHCP Server

    Note over Client, Server: 1. Discover Phase (Broadcast)
    Client->>Server: UDP/67 (Src: 0.0.0.0:68, Dst: 255.255.255.255:67)<br/><b>DHCPDISCOVER</b> (Client MAC, Requested IP)

    Note over Client, Server: 2. Offer Phase (Unicast or Broadcast)
    Server-->>Client: UDP/68 (Src: ServerIP:67, Dst: OfferedIP/255.255.255.255:68)<br/><b>DHCPOFFER</b> (Offered IP, Subnet Mask, Lease Time, Gateway)

    Note over Client, Server: 3. Request Phase (Broadcast)
    Client->>Server: UDP/67 (Src: 0.0.0.0:68, Dst: 255.255.255.255:67)<br/><b>DHCPREQUEST</b> (Selected Server IP, Accepted Offered IP)

    Note over Client, Server: 4. Acknowledgment Phase (Unicast or Broadcast)
    Server-->>Client: UDP/68 (Src: ServerIP:67, Dst: OfferedIP/255.255.255.255:68)<br/><b>DHCPACK</b> (IP Assignment, DNS Servers, Lease Duration)

    Note over Client: Client assigns IP to interface & starts Lease Timer
```

## NTP flow (simplified)

```mermaid
sequenceDiagram
    autonumber
    actor Client as NTP Client (Mode 3)
    participant Server as NTP Server / Stratum 1 (Mode 4)

    Note over Client: Client records Originate Timestamp (t1)
    Client->>Server: UDP/123: Client Request Packet<br/>[Transmit Timestamp = t1]

    Note over Server: Server receives packet at Receive Timestamp (t2)<br/>Server processes and sets Transmit Timestamp (t3)
    Server-->>Client: UDP/123: Server Response Packet<br/>[Originate = t1, Receive = t2, Transmit = t3]

    Note over Client: Client receives packet at Destination Timestamp (t4)

    Note over Client: <b>Client Clock Calculation:</b><br/>Round-Trip Delay (d) = (t4 - t1) - (t3 - t2)<br/>Clock Offset (θ) = ((t2 - t1) + (t3 - t4)) / 2<br/><i>Adjusts local system clock by θ</i>
```
