# Roadmap

Architecture and feature trajectory for DNS & DHCP engine services.

## PHASE 1: DHCP FEATURES

-  Static MAC-to-IP reservation mapping
-  DHCP Option 55/60 OS fingerprinting
-  Background expired lease reclamation worker
-  Relay agent (giaddr) and multi-subnet scope support

## PHASE 2: DNS INTEGRATION [PLANNED]

-  Automatic local DNS registration (.home.arpa)
-  Dynamic PTR (Reverse DNS) record generator
-  Upstream DNS-over-HTTPS (DoH) resolver client
-  Domain blocklist / sinkhole filtering engine

## PHASE 3: WEB UI & OPERATIONS [PLANNED]

-  Real-time dashboard (SignalR / Minimal APIs)
-  Prometheus metrics export endpoint (/metrics)
-  SQLite WAL mode storage provider alternative
-  OpenAPI / REST management API endpoints	
