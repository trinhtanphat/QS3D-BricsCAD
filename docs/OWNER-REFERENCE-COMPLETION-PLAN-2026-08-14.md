# Owner Reference Completion Plan — 2026-08-14

## Goal

Close every remaining non-overlapping, source-verifiable gap from the owner's `TEST QS3D` spreadsheet, linked usage document, supplied PDFs/images, and the current ChatGPT session while preserving QS3D's BricsCAD-hosted semantic/native architecture.

This plan is deliberately evidence-driven: an item is changed only when current `main` still proves a gap. Already-satisfied behavior is recorded instead of rewritten, active work owned by another agent is not touched, and exact BricsCAD runtime acceptance remains `LOCAL_ONLY` unless current-SHA licensed evidence exists.

## Reference matrix

| Owner reference | Current-source checkpoint | Planned action | Regression proof | Runtime status |
|---|---|---|---|---|
| Direct Draw beam changes the camera/zoom after authoring | Beam quick path already suppresses the automatic view switch, but `DirectDrawCommands` still defaults most other categories to post-authoring `QS3DVIEW3D` | Make the Direct Draw completion contract uniform: preserve the user's current view for every Direct Draw category and retain implied selection/highlight of generated native objects | Focused source preflight: no `QS3DVIEW3D` dispatch from Direct Draw; `SetImpliedSelection` remains | Source-verifiable; visual feel remains LOCAL_ONLY |
| Family/property creation takes too many operations | Family Manager/property surfaces already exist and have recent active-family UX work | Re-audit current Family Manager after active claims settle; only add a shortcut if the requested operation is still materially multi-step and no active claim owns the same symbols | Existing Family Manager guards plus a focused guard only if changed | LOCAL_ONLY for click-count/HiDPI acceptance |
| Component detail panel did not show expected quantity/details | Quantity geometry/data/detail explainer implementation is already present with exact-geometry and registration guards | Read current detail-data/registration source; do not duplicate implementation unless a live source gap remains | Existing quantity geometry/data preflights and Core smoke | Source-verifiable; selection/UI behavior LOCAL_ONLY |
| `QS3DSETUP` throws `StaticResourceExtension` during XAML load | Settings window and dark-host theme exist; exact resource reachability must be rechecked on current main | Resolve every settings `StaticResource` against local/merged resource dictionaries; if a key is not guaranteed reachable, fix the resource ownership/merge and add a source guard | Focused setup-resource preflight; build/source validation | Final XAML construction in BricsCAD LOCAL_ONLY |
| Smoke executable shows Windows `.NET` application-error dialog (`0xe0434352`) | Needs current entry-point audit | Ensure top-level smoke exceptions are reported deterministically to stderr and return nonzero without hiding failures, preventing an unhandled process crash dialog | Core smoke entry-point guard/test | Process behavior source/desktop verifiable; exact old dialog reproduction not required |
| Generic `SE`: active Family + one/many closed 2D polylines -> corresponding 3D components | Existing Plan-to-3D is wall-centerline-specific and intentionally rejects closed polylines as centerlines; therefore it is not the generic SE contract | Implement a separate generic closed-profile path that resolves the active/preferred Family/category, validates the whole selection before mutation, preserves source polylines, uses existing semantic/native builders, performs scoped regeneration and whole-batch compensation, and fails closed for unsupported categories | Dedicated SE source/preflight contract: command discoverability, closed-planar validation, active Family binding, ownership/source preservation, scoped regeneration, rollback | Exact BricsCAD object creation remains LOCAL_ONLY until licensed run |
| Drawing primitives / degenerate Line interaction mentioned earlier in the session | Re-audit current active-family drawing commands and their guards before touching | If current input path still allows equal points to reach a zero-direction failure, reject/re-prompt at the prompt boundary without weakening geometry validation | Focused drawing-source guard only if a gap remains | LOCAL_ONLY interaction acceptance |
| Historical session gates: rebar `project.Touch`, Direct Draw live ModelSpace/reference-wall contract, V26 runtime-token/net48 issues | Several lanes have been actively fixed/claimed since those reports | Re-run a source audit against current main and current claims. Do not reopen already-fixed or actively-owned lanes. Record exact remaining owner-visible gaps only | Existing aggregate/focused preflights | As documented by each lane |

## Implementation order

1. **Freeze a fresh source checkpoint** — read current `main`, recent claims and every file before writing it.
2. **Direct Draw ergonomics** — smallest independent owner-visible fix; preserve camera/zoom and select the generated component.
3. **Generic SE** — inspect active-Family selection and native-builder APIs, then implement the closed-profile command without changing the wall-centerline Plan-to-3D contract.
4. **SETUP resource audit** — resolve all settings-window resource keys and repair only proven reachability gaps.
5. **Smoke process boundary** — make top-level failures deterministic/non-dialog while preserving failing exit codes.
6. **Family/detail/drawing re-audit** — change only proven residual gaps after checking current claims.
7. **Full-session/project audit** — compare current source/status/preflights with historical open items; classify each as implemented, already satisfied, active-agent-owned, or LOCAL_ONLY.
8. **Validation and closeout** — read back all commits, inspect current-main checks without weakening gates, update the work claim with exact SHAs and remaining runtime-only acceptance.

## Non-negotiable invariants

- No force push and no stale-file replacement.
- No duplicate semantic ownership or alternate persistence model.
- No deleting/replacing the user's source 2D geometry as part of SE authoring.
- Preserve project snapshot/rollback, drawing-unit/UCS freshness and scoped regeneration boundaries.
- Do not weaken a test/preflight merely to obtain green status.
- Do not claim exact BricsCAD V25/V26 runtime PASS without exact-current-SHA licensed evidence.
- Do not edit surfaces reserved by another ACTIVE claim; leave an explicit handoff instead.

## Completion definition

The session is complete when every owner-reference/session item has one of four evidence-backed outcomes:

1. implemented and pushed to `main` with a focused regression contract;
2. verified already implemented on current `main`;
3. explicitly delegated/blocked by an ACTIVE non-overlapping agent claim; or
4. explicitly marked `LOCAL_ONLY` because the remaining proof intrinsically needs licensed BricsCAD/runtime interaction.
