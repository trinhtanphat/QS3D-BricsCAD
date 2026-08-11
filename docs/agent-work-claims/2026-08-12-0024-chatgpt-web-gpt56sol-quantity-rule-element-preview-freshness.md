# Work claim — Quantity Rule element preview freshness window

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-element-preview-freshness`
- Registered: `2026-08-12T00:24:00+07:00`
- Baseline main SHA: `fdc992439cf16653a7e7972c0886b8138a397bb8`
- Priority: P1 — bind element preview freshness to the project revision that existed before detached snapshot capture.

## Confirmed defect

`QuantityRulePreviewService.PreviewElement(...)` creates the detached project snapshot first and reads `project.ChangeVersion` only afterwards. If project state changes while the detached snapshot is being captured, the returned preview can describe the earlier/mixed snapshot while being stamped with the later live revision. `ApplyElement(...)` then has no version mismatch to reject solely from that change. `PreviewProject(...)` already uses the correct ordering by capturing `ChangeVersion` before `CreateDetachedCopy(...)`.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRulePreviewService.cs`
- `scripts/preflight-quantity-rule-element-preview-freshness.py` (new)
- this claim file for close-out

## Intended contract

- `PreviewElement(...)` captures `project.ChangeVersion` before detached snapshot creation and stamps the preview with that immutable scalar.
- Existing exact-owned-element checks, detached preview semantics, apply equivalence checks and mutation tracking remain unchanged.
- `PreviewProject(...)` ordering remains the reference contract.
- Static regression must reject the legacy post-snapshot `project.ChangeVersion` stamp.

## Validation boundary

This is a source-ordering TOCTOU fix. A deterministic concurrent snapshot interleaving is not exposed by current Core hooks, so regression is static/source-contract based rather than a timing-sensitive threaded smoke. No GitHub Actions dispatch and no BricsCAD V25 runtime claim.

## Completion condition

Element preview freshness starts before snapshot capture, the source gate is on current `main`, and this claim is closed with exact SHAs and truthful validation limits.
