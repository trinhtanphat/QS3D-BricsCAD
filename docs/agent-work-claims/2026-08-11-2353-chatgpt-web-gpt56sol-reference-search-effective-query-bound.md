# Work claim — Construction reference search effective-query bound

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:53:00+07:00`
- Pre-registration observed main SHA: `055a30e4c338f963f786b7347a9715f6b463d92a`
- Registration parent main SHA: `d22fdee7428e65aa6c91f172c5d0915c56308e9b`
- Claim commit: `b5ea9abc76d89ca6803d9d4b5aa38bea8aaf95a1`
- Priority: evidence-driven source-safe input-boundary hardening

## Coordination note

`main` advanced between the pre-registration HEAD read and the contents-API claim commit. The registration parent above is authoritative; this claim-only reconciliation is committed before any substantive source edit.

## Confirmed defect

`ReferenceSearchWindow` declares `MaxQueryLength = 512` and enforces the limit both for raw input and after adding the optional technical-context suffix. The `shorts` result path, however, later appends `" video ngắn shorts"` inside `BuildSearchUrl` without applying the same limit. A near-limit valid query can therefore become an internally expanded effective query longer than the declared bound before URL encoding/browser launch.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml.cs`
- `scripts/preflight-reference-search-effective-query-bound.py` (new focused gate)
- `docs/UI-REFERENCE-SEARCH-EFFECTIVE-QUERY-BOUND-2026-08-11.md` (new focused note)
- this claim file

The existing construction-reference command/XAML, Start Center, shared browser/query planners, Workspace/RightPanel, quantity, persistence, grid, schedule-placement and other concurrent lanes are explicitly out of scope.

## Intended contract

1. Every QS3D-added query suffix must keep the final effective query within `MaxQueryLength` before `Uri.EscapeDataString` and browser launch.
2. Technical-context behavior remains bounded with the same 512-character policy.
3. The `shorts` helper suffix retains its current search semantics when the expanded query fits.
4. Active-DWG ownership, document-bound lifetime, SafeSearch, fixed HTTPS provider and browser-only/no-scrape boundary remain unchanged.
5. Add a focused source preflight; do not dispatch GitHub Actions and do not claim native BricsCAD V25 runtime PASS remotely.

## Completion condition

No internally appended Reference Search suffix can bypass the declared effective-query length bound, the focused regression contract is on `main`, and this claim is closed without touching concurrent scopes.
