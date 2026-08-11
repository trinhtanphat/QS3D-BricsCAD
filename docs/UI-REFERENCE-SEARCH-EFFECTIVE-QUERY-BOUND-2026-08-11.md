# Construction Reference Search — effective-query length bound

Date: 2026-08-11  
Scope: `ReferenceSearchWindow` browser-launch query construction

## Problem

Reference Search already bounded user-entered text to 512 characters and also bounded the optional technical-context suffix. The `Video ngắn` (`shorts`) path added its own internal `" video ngắn shorts"` suffix later in `BuildSearchUrl`, after the raw-input guard, without re-checking the final effective query length.

That meant a raw query could satisfy `MaxQueryLength` while the QS3D-expanded query exceeded the same declared boundary immediately before URL encoding and browser launch.

## Fix

A single `AppendBoundedSuffix` helper now owns QS3D-added query suffixes. It verifies `query.Length + suffix.Length <= MaxQueryLength` before concatenation.

Both current internal expansions use the helper:

- optional technical context: `" kỹ thuật xây dựng chi tiết thi công"`;
- `shorts` search context: `" video ngắn shorts"`.

`BuildSearchUrl` therefore receives/creates a bounded effective query before `Uri.EscapeDataString`.

## Preserved boundaries

This lane does not change result categories, provider URLs, SafeSearch flags, query encoding, `ProcessStartInfo`, default-browser launch, active-DWG ownership or modeless document lifetime. It also does not add HTTP scraping/network-fetch code inside QS3D.

## Regression gate

`scripts/preflight-reference-search-effective-query-bound.py` protects:

- the 512-character policy;
- the shared bounded-suffix guard;
- technical-context and `shorts` routing through that guard;
- ordering of the `shorts` bound before URL encoding;
- active-DWG/document-bound and fixed-HTTPS/no-scrape invariants.

## Validation boundary

The source and regression contract were committed through the GitHub connector. GitHub Actions were not dispatched. No local full-repository build or native BricsCAD V25 runtime PASS is claimed by this remote lane.
