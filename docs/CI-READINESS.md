# CI readiness

This document separates current CI policy from historical qualification evidence.

## Current policy

Current GitHub Actions behavior is defined only by `../CI_POLICY.md` plus the executable workflows.

The current shared model includes automatic branch/PR validation. Older statements that GitHub Actions are broadly `manual-only` are historical and must not be used as current operating rules.

Normal task agents may observe and remediate the automatic validation for their own carrier. Manual release/runtime workflows remain separately controlled.

## Current protected merge evidence

A normal task merge requires the protected current PR candidate to satisfy:

- required `preflight` SUCCESS;
- required `core` SUCCESS;
- strict freshness;
- mergeability;
- ownership/collision requirements;
- applicable same-task authorization from `MAIN-WRITE-AUTHORIZATION.md`.

Branch CI is early exact-head evidence and a known red branch run must be remediated, but its completion timestamp is not permanent PR identity.

## Historical evidence

Recorded CI/local/runtime runs in older audits or handoffs remain evidence only for the exact source tree they actually tested. Never use a historical green run to qualify a newer head.

## Licensed/local qualification

Hosted CI does not automatically prove:

- BricsCAD interactive NETLOAD/DemandLoad behavior;
- native Windows UI behavior;
- private/customer DWG behavior;
- signing/trust behavior requiring private credentials;
- machine-specific runtime/performance.

Use the current V25/V26/local runbook and exact-SHA evidence when those acceptance classes are actually required.