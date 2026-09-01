CREATE TABLE IF NOT EXISTS dns_lists (
    id int NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    path VARCHAR(255) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_dns_lists_path ON dns_lists (path);

CREATE TABLE IF NOT EXISTS dns_response_events (
    id VARCHAR(36) NOT NULL PRIMARY KEY,
    start_time_utc BIGINT NOT NULL,
    context_id VARCHAR(64) NOT NULL,
    question_name VARCHAR(255) NOT NULL,
    question_type VARCHAR(32) NOT NULL,
    client_ip VARCHAR(45) NOT NULL,
    client_port INT NULL,
    client_name VARCHAR(255) NULL,
    resolution_source VARCHAR(64) NOT NULL,
    rcode VARCHAR(16) NOT NULL,
    duration_ms DOUBLE PRECISION NOT NULL,
    blocked INT NOT NULL,
    upstream VARCHAR(64) NULL,
    answer_data VARCHAR(255) NULL,
    ttl_seconds INT NULL,
    block_rule_id VARCHAR(128) NULL
);

-- Performance Indexes
CREATE INDEX IF NOT EXISTS idx_dns_events_time_blocked ON dns_response_events (start_time_utc, blocked);
CREATE INDEX IF NOT EXISTS idx_dns_events_client ON dns_response_events (client_ip, start_time_utc);
CREATE INDEX IF NOT EXISTS idx_dns_events_question ON dns_response_events (question_name, blocked);

CREATE INDEX IF NOT EXISTS idx_dns_events_start_time ON dns_response_events (start_time_utc);
CREATE INDEX IF NOT EXISTS idx_dns_events_context_id ON dns_response_events (context_id);

CREATE TABLE IF NOT EXISTS discovered_lan_devices (
    mac_address VARCHAR(17) NOT NULL,
    ip_address  VARCHAR(45) NOT NULL,
    ptr_address VARCHAR(64) NOT NULL,
    host_name   VARCHAR(255) NULL,
    vendor      VARCHAR(255) NOT NULL,
    device_type VARCHAR(32) NOT NULL,
    first_seen  BIGINT NOT NULL,
    last_seen   BIGINT NOT NULL,

    CONSTRAINT pk_discovered_lan_devices PRIMARY KEY (mac_address)
);

CREATE INDEX IF NOT EXISTS idx_discovered_lan_devices_ip_address 
    ON discovered_lan_devices (ip_address);

CREATE INDEX IF NOT EXISTS idx_discovered_lan_devices_last_seen 
    ON discovered_lan_devices (last_seen DESC);

-- Create dhcp_leases table storing timestamps as Unix epoch seconds and booleans as integers
CREATE TABLE IF NOT EXISTS dhcp_leases (
    client_id VARCHAR(255) NOT NULL,
    client_name VARCHAR(255) NOT NULL DEFAULT '',
    mac_address VARCHAR(32) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    lease_start_time BIGINT NOT NULL,
    lease_end_time BIGINT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT pk_dhcp_leases PRIMARY KEY (client_id)
);

-- Index for queries filtering by MAC address (GetLeaseByClientIdOrMacAsync, ReleaseLeaseAsync)
CREATE INDEX IF NOT EXISTS idx_dhcp_leases_mac_address 
ON dhcp_leases (mac_address);

-- Composite index for fast active IP lookups (GetLeaseByIpAsync)
CREATE INDEX IF NOT EXISTS idx_dhcp_leases_ip_active 
ON dhcp_leases (ip_address, is_active);

-- Covering index for IP availability checks against active leases and expiration times (IsIpAvailableAsync)
CREATE INDEX IF NOT EXISTS idx_dhcp_leases_availability 
ON dhcp_leases (ip_address, is_active, lease_end_time, client_id);
