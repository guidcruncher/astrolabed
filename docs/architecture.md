# Architecture & Flow Diagrams

Below are simple mermaid diagrams illustrating the high-level architecture and request flows for DNS, DHCP and NTP.

## High-level component diagram

```mermaid
graph TD
    classDef client fill:#34495e,stroke:#2c3e50,color:#fff;
    classDef core fill:#2980b9,stroke:#1f618d,color:#fff;
    classDef rule fill:#27ae60,stroke:#1e8449,color:#fff;
    classDef upstream fill:#8e44ad,stroke:#6c3483,color:#fff;
    classDef util fill:#d35400,stroke:#a04000,color:#fff;

    subgraph Clients["Clients & Network Traffic"]
        DNS_Client["DNS Client Requests<br/>(UDP / TCP / DoH)"]:::client
    end

    subgraph CoreEngine["Astrolabed.Dns.Core"]
        Server["DnsServer Listener<br/>• Dual-Stack Socket Engine<br/>• Bounded Worker Channel<br/>• Zero-Alloc ArrayPool<byte>"]:::core
        Parser["DnsParser & DnsMessage<br/>• Wire-Format Parsing<br/>• Pointer Recursion Guard (Max 128)"]:::core
        Forwarder["DnsForwarderService<br/>• Pipeline Orchestration<br/>• EDNS0 Truncation Inspection"]:::core
        CachingDecorator["CachingDnsClientDecorator<br/>• In-Memory Fast Lookup<br/>• Dynamic TTL Expiration"]:::core
    end

    subgraph RuleEngine["Astrolabed.Dns.RuleEngine"]
        Cache["DnsCache<br/>• ConcurrentDictionary Storage<br/>• Single-Capacity Eviction Channel<br/>• TransID & 0x20 Case Patching<br/>• NXDOMAIN Water Torture Guard"]:::rule
        Compiler["RuleCompiler<br/>• Pattern Categorization"]:::rule
        
        subgraph Matchers["Matching Engine"]
            Exact["Exact Matcher<br/>(Dictionary<string, CompiledRule>)"]:::rule
            Suffix["SuffixTrie<br/>(*.domain.com)"]:::rule
            Prefix["PrefixTrie<br/>(domain.*)"]:::rule
            Aho["AhoCorasickMatcher<br/>(*keyword*)"]:::rule
            RegexM["Regex Rules"]:::rule
            HostM["HostMatcher<br/>(Host-to-IP Specificity)"]:::rule
        end

        ChainBuilder["ResolverChainBuilder<br/>• Chain Routing Assignment"]:::rule
        BlockBuilder["BlockResponseBuilder<br/>• Synthesizes NXDOMAIN/REFUSED<br/>• Remaps ZeroIP / CustomIP"]:::rule
        Executor["QueryExecutor<br/>• Upstream Timeout CTS Loops"]:::rule
    end

    subgraph Transport["Upstream Clients (DefaultDnsClientFactory)"]
        UdpClient["UdpDnsClient"]:::upstream
        TcpClient["TcpDnsClient"]:::upstream
        DohClient["DohDnsClient (RFC 8484)"]:::upstream
    end

    subgraph Utilities["Utilities"]
        ListPool["ListPool<T><br/>• Lock-free Thread-Local Array Pooling"]:::util
    end

    %% Ingestion Flow
    DNS_Client -->|Raw Byte Packets| Server
    Server -->|Parse Request| Parser
    Server -->|Context Envelope| Forwarder
    
    %% Pipeline & Cache Checking
    Forwarder -->|1. Try Get Cached| Cache
    Forwarder -->|2. Check Rules| Compiler
    
    %% Matching Process
    Compiler --> Matchers
    Matchers -->|Host Match| HostM
    Matchers -->|Exact Match| Exact
    Matchers -->|Suffix Match| Suffix
    Matchers -->|Prefix Match| Prefix
    Matchers -->|Substring Match| Aho
    Matchers -->|Pattern Match| RegexM

    %% Routing Decisions
    Matchers -->|Rule Match / Result| ChainBuilder
    
    %% Action Branching
    ChainBuilder -->|Blocked Query| BlockBuilder
    BlockBuilder -->|Synthesized Block Response| Forwarder
    
    ChainBuilder -->|Allowed Query / Upstreams| Executor
    Executor -->|Forward Query| CachingDecorator
    
    %% Upstream Protocols
    CachingDecorator --> UdpClient
    CachingDecorator --> TcpClient
    CachingDecorator --> DohClient

    %% Storage & Memory Optimizations
    Executor -->|Store Positive Response| Cache
    Executor .->|Rent / Return Lists| ListPool
```

## DNS request sequence

```mermaid
sequenceDiagram
    autonumber
    actor Client as DNS Client
    participant Listener as DnsServer Listener
    participant Forwarder as DnsForwarderService
    participant Cache as DnsCache
    participant Engine as RuleEngine & Matchers
    participant Executor as QueryExecutor
    participant Upstream as Upstream DNS (UDP/TCP/DoH)

    Client->>Listener: Transmit DNS Query Packet
    Listener->>Listener: Parse Wire Format & Validate Pointer Loops
    Listener->>Forwarder: Enqueue DnsRequestContext
    
    Forwarder->>Cache: TryGet(RequestContext)
    alt Cache Hit
        Cache-->>Forwarder: Cached Response Buffer
        Forwarder->>Forwarder: Patch TxID, RD Flag, 0x20 Case
        Forwarder-->>Client: Return Cached Response Packet
    else Cache Miss
        Forwarder->>Engine: Match Domain & Rule Logic
        alt Rule Action: Block
            Engine-->>Forwarder: Synthesize Block Response (NXDOMAIN / Custom IP)
            Forwarder-->>Client: Return Synthesized Response
        else Rule Action: Allow
            Engine->>Executor: Build & Assign Resolver Chain
            Executor->>Upstream: Forward Query via UDP / TCP / DoH
            Upstream-->>Executor: Raw DNS Response
            Executor->>Cache: Store Response (if TTL > 0)
            Executor-->>Forwarder: DNS Response Packet
            Forwarder-->>Client: Return DNS Response
        end
    end
```

## DHCP flow (simplified)

```mermaid
sequenceDiagram
    autonumber
    actor Client as DHCP Client
    participant Server as DHCP Server

    Note over Client, Server: Phase 1: Discovery
    Client->>Server: DHCPDISCOVER (Broadcast 255.255.255.255)
    
    Note over Client, Server: Phase 2: Offer
    Server->>Client: DHCPOFFER (Unicast / Broadcast with Offered IP & Options)
    
    Note over Client, Server: Phase 3: Request
    Client->>Server: DHCPREQUEST (Broadcast - Confirming Selected Offer)
    
    Note over Client, Server: Phase 4: Acknowledgment
    Server->>Client: DHCPACK (Unicast / Broadcast - Lease Granted, Subnet, Gateway, DNS)
```

## NTP flow (simplified)

```mermaid
sequenceDiagram
    autonumber
    actor Client as NTP Client
    participant Server as NTP Stratum Server

    Note over Client: Record Timestamp t1 (Transmit Time)
    Client->>Server: NTP Request Packet (t1)
    
    Note over Server: Record Timestamp t2 (Receive Time)
    Note over Server: Process Request & Record t3 (Transmit Time)
    Server-->>Client: NTP Response Packet (t1, t2, t3)
    
    Note over Client: Record Timestamp t4 (Destination Time)
    
    Note over Client: Calculate Metrics:<br/>Round-Trip Delay = (t4 - t1) - (t3 - t2)<br/>Clock Offset = ((t2 - t1) + (t3 - t4)) / 2
    Note over Client: Adjust System Clock
```
