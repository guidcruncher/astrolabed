-- PostgreSQL and SQLite compatible schema for DnsResponseEvent
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
