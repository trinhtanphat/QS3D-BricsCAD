# Platform Quantity Hardening Floor

Issue: #6146
Lane: C02 Quantity / Estimating / Export integration
Runtime: REMOTE_SAFE source/integration only.

## Purpose

The BricsCAD repository must not pin a QS3D-Platform generation whose QuantitySchedule or BOQ materializers can read `IEnumerator.Current` after an advertised collection Count has already been exhausted. A hostile `Count=N` / `yield=N+1` source must fail on the surplus `MoveNext()` probe without exposing the surplus `Current` value.

## Admission contract

The pinned Platform tree must contain both QuantitySchedule and BOQ hostile-count executable regressions and their dedicated captured-count materializers. Unknown-count sources may remain bounded single-pass enumeration. Known-count sources must consume exactly N successful `MoveNext()+Current` pairs, then make one surplus `MoveNext()` probe only. Negative, conflicting, drifting and oversized Count values remain fail-closed.

The selected Platform generation must also preserve deterministic ordinal schedule/BOQ ordering, exact quantity provenance and project snapshot affinity, finite/overflow admission, stable-generation checks, CSV round-trip/formula-safety behavior, and BOQ checked commercial arithmetic.

## Current production descendant

The audited descendant selected by this carrier is QS3D-Platform `5c1b650e475af3c823ae38aaf1a4a85f41985457`, a direct descendant of the former pin `fcf24893aac7fabe11017bbd5ed0072f5becd87d` (407 commits ahead, 0 behind). It contains the QuantitySchedule and BOQ known-count/no-overread materializers and executable regressions, plus the intervening C02 provenance/generation/export hardening reviewed by this carrier.

## Validation

Run `python scripts/preflight-platform-quantity-hardening-floor.py`, then the normal protected Shared CI. The final candidate requires exact-head `preflight` and `core` success after reconciling current protected `main`.

Hosted/static success is not licensed BricsCAD runtime evidence. Do not relabel REMOTE_SAFE validation as LOCAL_PASS.
