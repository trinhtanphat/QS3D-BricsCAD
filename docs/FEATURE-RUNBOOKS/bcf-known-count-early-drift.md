# BCF known-Count early drift

Status: `SOURCE_READY / PENDING_LOCAL`

Lane-Key: `issue-4349`

## Purpose

BCF 3.0 exchange materializes bounded collections at four shared package levels: top-level topics, topic viewpoints, topic comments, and viewpoint components. A collection that exposes trustworthy Count metadata must not be allowed to yield an additional value and let that unexpected value enter null, duplicate, reference, sorting, or package validation before the cardinality contract fails.

## Source contract

`BcfIssueExchangeContract.MaterializeBounded<T>` owns the shared collection boundary.

- Negative, conflicting, or larger-than-supported known Count values fail before enumeration.
- When a known Count exists, the first value yielded after that Count is exhausted fails before the value is appended to the materialized list.
- A source that yields fewer values than its known Count fails after traversal completes.
- Sources with no known Count retain the independent package maximum bound.
- Honest counted inputs and pure streaming inputs retain canonical sorting and existing BCF identity/reference validation.
- Count drift is therefore detected before nested semantic validation for topics, viewpoints, comments, and components.

## Deterministic regression

`BcfIssueExchangeKnownCountEarlyDriftSmoke` covers:

1. topics: Count=1 with a valid topic followed by an unexpected null;
2. viewpoints: Count=1 with a valid viewpoint followed by an unexpected null;
3. comments: Count=1 with a valid comment followed by an unexpected null;
4. components: Count=1 with a valid component followed by an unexpected null;
5. under-yield: Count=2 with one topic, rejected only after completed traversal;
6. pure streaming topics: no Count metadata, still accepted and canonically sorted.

The overrun regressions assert exactly two `MoveNext` calls: the expected item and the first unexpected item. No later value may be requested or materialized after the drift is proven.

## Validation boundary

This package is Core BCF/export data-integrity work. It changes no BCF schema, IFC GUID representation, ZIP/archive I/O, native CAD adapter, or release artifact.

There is no licensed BricsCAD runtime acceptance in this source lane. `PENDING_LOCAL` is retained only as an explicit non-claim boundary: hosted CI, deterministic smoke, or protected merge must never be represented as `LOCAL_PASS`.

Merge requires the canonical current-main candidate to pass protected `preflight` and `core`, followed by expected-head merge and exact-main verification.
