# Work claim — Quantity report semantic selection freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-report-semantic-selection-freshness`
- Registered: `2026-08-12T11:42:00+07:00`
- Completed: `2026-08-12T11:47:00+07:00`
- Baseline main SHA: `85e6410783fb2fc69de1207b57b5458b543c416e`
- Priority: P2 — fail closed when caller-controlled lazy report selection changes the project semantic version during enumeration.

## Confirmed defect

`ProjectQuantityReportBuilder.ResolveSelection(...)` already protected structural freshness by re-checking selected element instances after caller-controlled `IEnumerable<string>` enumeration. It did not capture or re-check `ProjectState.ChangeVersion`. A lazy selection could call `project.Touch()` while yielding the same selected element instances (or even yield no ids), after which report construction continued across a project semantic-version boundary.

## Delivered contract

- Capture `project.ChangeVersion` immediately before caller-controlled selection enumeration.
- If `ChangeVersion` differs after enumeration, fail before report aggregation.
- Preserve the existing structural selected-instance freshness checks for direct collection remove/replace mutations that may not increment `ChangeVersion`.
- A mutating empty lazy selection also fails closed.
- Stable lazy Group/Detail selections retain existing behavior.
- No public API signature changes.

## Evidence

- Claim: `0a298f9b68136a970b09dfa5bd6f850598df1b4b`
- Plan: `42fb8e4049a07cad89f6e8e50f11cabda8b7ab86`
- Source fix: `ab29a302a63e75009affced8a0a04fc33f68c18a`
- Focused smoke: `4c2d9348d93c22d9ea6c24eceaebf0d918c7ca9f`
- Smoke registration: `d32590ea32c9b577110a38feee38ed7624ecdc61`
- Static preflight: `acd030b9aebf75eb1fddaef9b3a48e954897b09f`

Readback on current `main` confirmed capture-before-enumeration, semantic-version check after enumeration, preservation of the prior structural instance freshness guard, mutate-yield/mutate-empty/stable smoke coverage, ModuleInitializer registration, and the static preflight after concurrent writes.

## Validation limits

The GitHub connector session did not execute the Core smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime. No PASS is claimed for those execution environments.

## Excluded scope

- Existing structural selection-freshness lane completed by `53b99cd5b89ef722bc7d51215801a4ee190a456c`.
- Quantity formulas/grouping semantics unrelated to selection freshness.
- UI/export/runtime integration.
- GitHub Actions or licensed BricsCAD runtime qualification.
