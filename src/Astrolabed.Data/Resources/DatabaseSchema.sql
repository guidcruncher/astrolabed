-- PostgreSQL and SQLite compatible schema
CREATE TABLE IF NOT EXISTS dns_response_events (
    id VARCHAR(36) NOT NULL PRIMARY KEY,
    start_time_utc BIGINT NOT NULL,
    context_id VARCHAR(64) NOT NULL,
    question_name VARCHAR(255) NOT NULL,
    question_type VARCHAR(32) NOT NULL,
    client_endpoint VARCHAR(64) NOT NULL,
    client_name VARCHAR(255) NOT NULL,
    resolution_source VARCHAR(255) NOT NULL,
    duration_ms DOUBLE PRECISION NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_dns_events_start_time ON dns_response_events (start_time_utc);
CREATE INDEX IF NOT EXISTS idx_dns_events_question ON dns_response_events (question_name);
CREATE INDEX IF NOT EXISTS idx_dns_events_context ON dns_response_events (context_id);

CREATE TABLE IF NOT EXISTS discovered_lan_devices (
    mac_address VARCHAR(17) NOT NULL,
    ip_address  VARCHAR(45) NOT NULL,
    ptr_address VARCHAR(64) NOT NULL,
    host_name   VARCHAR(255) NULL,
    first_seen  BIGINT NOT NULL,
    last_seen   BIGINT NOT NULL,

    CONSTRAINT pk_discovered_lan_devices PRIMARY KEY (mac_address)
);

CREATE INDEX IF NOT EXISTS idx_discovered_lan_devices_ip_address 
    ON discovered_lan_devices (ip_address);

CREATE INDEX IF NOT EXISTS idx_discovered_lan_devices_last_seen 
    ON discovered_lan_devices (last_seen DESC);
