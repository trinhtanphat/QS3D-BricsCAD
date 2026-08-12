# Work claim — targeted opening request blank-id fail-closed

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-opening-request-blank-id`
- Registered: `2026-08-12T13:49:00+07:00`
- Baseline main SHA: `7493794b079e188fa1ac9ab04411eb5bb0b3f359`
- Priority: destructive targeted opening cuts must reject malformed explicit target collections before any CAD boolean mutation instead of silently dropping blank/null ids and executing the valid subset.

## Reserved scope

Harden only explicit requested-opening normalization in `src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs`, plus focused source regression/preflight coverage and this claim close-out.

## Canonical evidence

- `NormalizeRequestedOpenings(...)` currently constructs a set from `openingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())`, so an explicit request such as `[validOpeningId, "   "]` silently becomes `[validOpeningId]`.
- The returned set is the authorization boundary for the subsequent targeted physical cut. Silently narrowing malformed destructive input can therefore execute a partial request instead of failing before CAD mutation.
- `PhysicalOpeningCutTargetStateCodec.Normalize(...)` already rejects blank opening ids (commit `044c84903fab09428533bf526bb9e6e99bb3437b`), so persisted cut-state and explicit requested-cut normalization currently disagree on fail-closed handling.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs`
- focused regression/preflight under `scripts/` or existing opening smoke surface
- this claim file for close-out

## Excluded scope

- No changes to physical cut geometry, target-state codec encoding, Direct Draw, UI/ribbon, health logic or unrelated opening ownership rules.
- No GitHub Actions dispatch, build/release publication or BricsCAD V25 runtime PASS claim.

## Validation plan

- Reject null/blank entries in a non-null explicit `openingIds` collection before lookup/boolean work.
- Preserve `openingIds == null` as the existing all-linked request semantics.
- Preserve canonical trimming/case-insensitive duplicate handling for valid ids unless current contract requires a stricter existing rule.
- Add a focused static/local regression that guards against reintroducing `.Where(x => !string.IsNullOrWhiteSpace(x))` subset filtering and requires explicit blank-id rejection before target lookup/mutation.
- Re-fetch exact source before update, read back after commit, verify main ancestry, then close this claim with exact SHA evidence.

## Coordination

Recent main claims reserve Grid Annotation handle identity, semantic schedule canonical id, formula underflow, Preview Review CDATA, interchange name canonicality, Room Finish XLSX and related independent lanes. Recent opening preflight/message claims are completed and do not reserve `OpeningBooleanService` request normalization.

## Completion condition

Malformed explicit opening-id collections fail closed before any physical cut can run; focused regression is committed to `main`; this claim is closed with exact evidence.