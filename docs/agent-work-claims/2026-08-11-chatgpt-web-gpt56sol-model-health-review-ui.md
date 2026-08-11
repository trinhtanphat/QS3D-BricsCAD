# Agent work claim — Model Health review filters and triage UX

- Agent: `chatgpt-web-gpt56sol-model-health-review-2049`
- Registered: `2026-08-11T20:49:00+07:00`
- Status: `ACTIVE`
- Baseline main SHA: `15613ad6854189406bb3f4a6ea8cfa29eff333ca`
- Priority: continue the owner-requested professional UI/UX wave by making Model Health usable on larger issue sets without changing any diagnostic rule or CAD ownership behavior.

## Reserved scope

Enhance the existing document-bound `ModelHealthWindow` with read-only triage controls: search across issue code/element/message, severity filter, visible/total issue count, and explicit stale-snapshot filter lockout. Preserve the source snapshot identity/freshness guard and existing locate behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml.cs`
- a new focused `scripts/preflight-model-health-review-ui.py`
- `docs/UI-MODEL-HEALTH-REVIEW-2026-08-11.md` (new)
- this claim file for close-out

## Functional contract

- Filtering is purely in-memory over the immutable issue list passed to the window; no diagnostic service is rerun and no semantic/project/CAD state is mutated.
- Search is case-insensitive across `Code`, `ElementId` and `Message`.
- Severity selection supports all/Error/Warning/Info and never changes issue severity.
- Locate remains fail-closed on wrong active DWG or stale project snapshot.
- Once the source project snapshot is stale, grid and filter controls are disabled and the user is instructed to rerun Health.

## Explicit exclusions / coordination

- No Core Diagnostics/Health service logic, generated ownership, Rebar/Wall health, BQ/quantity, RightPanel, Workspace, Ribbon, updater, Zone/Family, Commands.cs, release/signing or local-only runtime implementation.
- No overlapping repository-health/docs consolidation work beyond this new focused design note/preflight.
- No BricsCAD V25/WPF runtime PASS claim from the remote lane.

## Validation plan

- Re-fetch current main, claims and target blobs immediately before implementation.
- Preserve project identity/fingerprint/change-version freshness checks exactly.
- Add static coverage for search/severity/count wiring, in-memory source ownership, stale lockout and unchanged locate guard.
- Inspect final current-main ancestry/status metadata; native keyboard/focus/HiDPI rendering remains LOCAL_ONLY.

## Completion condition

Search/severity triage and visible-count UX are on current `main`, focused regression coverage/design notes are committed, freshness/locate guards remain intact, and this claim is closed with exact implementation SHAs.