# Heuristic Scoring

The heuristic score is a quantitative risk assessment (from 0 to 100+) calculated by analyzing the lexical patterns, structural characteristics, and statistical properties of a domain name. Rather than relying on binary blocklists, it sums weighted risk indicators to determine if an unknown domain exhibits behaviors typical of ad-serving and tracking infrastructure.

## How the Scoring Model Works
The Engine evaluates an incoming domain across three main categories. Each rule triggered adds points to a cumulative threat score. If the cumulative score meets or exceeds the configured ThreatThreshold (e.g., 40.0), the domain is flagged as an ad domain (IsAdDomain = true).

```
Final Threat Score = Keyword Score + Entropy Score + Structural Score
```

## Detailed Breakdown of Heuristic Rules

1. Keyword & Terminology Matching (Weight: High)
Ad networks and tracking providers frequently incorporate descriptive tokens into their domain hierarchies or subdomains.
 * Exact Keyword Match (+35 points per segment):
   * Trigger: A domain segment exactly matches a known tracking term (e.g., adservice, telemetry, analytics, pixel, tracker).
   * Example: In adservice.google.com, the segment adservice triggers +35 points.
 * Partial Keyword Match (+15 points per segment):
   * Trigger: A domain segment contains a tracking keyword embedded alongside other characters (e.g., pagead, adsystem, mytracker).
   * Example: In pagead2.googlesyndication.com, pagead2 contains ad, triggering +15 points.

2. Shannon Entropy / Character Randomness (Weight: Medium-High)
Dynamic ad networks and fingerprinters use Programmatic Domain Generation Algorithms (DGAs) or high-entropy hash strings in subdomains to bypass static filters and target specific user sessions.
 * High Subdomain Entropy (+25 points):
   * Trigger: Calculated Shannon entropy on subdomains exceeds 3.8 with a length greater than 8 characters.
   * Formula: Measures character frequency distribution (H(X) = -\sum P(x_i) \log_2 P(x_i)).
   * Example: a839f9a2b.telemetry.adnetwork.net
     * The subdomain sequence a839f9a2btelemetry has high character variety/randomness. It triggers +25 points.
     * A standard human-readable subdomain like blog.example.com has low entropy and triggers 0 points.

3. Domain Depth & Structural Patterns (Weight: Low-Medium)
Tracking networks often use deep subdomain hierarchies to implement CNAME cloaking or route traffic through multi-tenant CDN nodes.
 * Excessive Domain Depth (+15 points):
   * Trigger: The FQDN consists of 5 or more dot-separated segments.
   * Example: ad.tracker.cdn.east.example.com (6 segments) triggers +15 points.
 * Numeric Identifier Subdomain (+20 points):
   * Trigger: The primary subdomain starts with 4 or more consecutive digits, typical of dynamic impression trackers or automated ad servers.
   * Example: p10243.tracker.example.org contains 1024 in the first segment, triggering +20 points.

## Example Assessment Runs

| Domain | Triggered Rules | Individual Scores | Final Score | Action (Threshold = 40) |
|---|---|---|---|---|
| github.com | None | 0 | 0.0 | Allowed |
| adservice.google.com | Exact Keyword: adservice | +35 | 35.0 | Allowed (Below Threshold) |
| a839f9a2b.telemetry.adnetwork.net | Partial Keyword (telemetry), Partial Keyword (adnetwork), High Entropy | +15, +15, +25 | 55.0 | Blocked |
| 10243.pixel.tracker.cdn.example.com | Numeric Subdomain, Exact Keyword (pixel), Exact Keyword (tracker), Depth (6 segments) | +20, +35, +35, +15 | 105.0 | Blocked |

## Whitelist Override

To prevent false positives on legitimate services (like raw.githubusercontent.com or analytics.azure.com), the scanner runs a Whitelist Check before calculating the heuristic score. If a domain or parent domain matches the whitelist, scanning short-circuits immediately with a score of 0.0.

Note: The scores are stored for your Analysis and optmising block lists. They do not automatically block domains.
