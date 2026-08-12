# Work claim — HostLinkService global semantic element identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-host-link-global-element-integrity-20260812-0910`
- Registered: `2026-08-12T09:10:00+07:00`
- Baseline main SHA: `78a298e7e509c2de65f3efb638016f0a5adc448a`
- Priority: P1 — prevent audited host-link mutation on a project whose semantic element identity set is already ambiguous.

## Confirmed defect

`HostLinkService.LinkOpening(...)` and `UnlinkOpening(...)` resolved only the requested opening/host via `ProjectState.FindElement(...)`. Because `FindUnique` only detects duplicates matching the requested ID, an unrelated duplicate pair such as `DUP` + `dup` could coexist while a unique `OPEN`/`WALL` link or unlink still mutated semantic relations and audit state.

## Implemented fix

- Both LinkOpening and UnlinkOpening preflight the complete `project.Elements` identity set before target lookup.
- Null semantic entries and case-insensitive duplicate element IDs fail closed before HostWallId/dependency/dirty/audit/project revision mutation.
- Canonical no-op behavior, physical-cut safety, dependency canonicalization, rollback protection and audit-owned one-revision semantics remain unchanged.
- Focused smoke covers failed Link and Unlink atomicity on unrelated duplicates plus a valid canonical link control.

## Integration evidence

- Claim registration: `55301299f8878eee87ef447aa110bb98cd01af73`.
- Branch source commit: `905508d4b88b6d5a255d2bdfaf6f880f555dd0d1`.
- Focused smoke commit: `58141d0f3b8babf4823678357e8ac48058b80cf3`.
- Exact branch diff was only `HostLinkService.cs` (+15) plus the new 99-line smoke.
- Comparison from claim registration to then-current `main` `eb752d4305e91be94ce1011be3ec055a8ec170dc` showed 23 intervening commits and no reserved-path overlap.
- PR `#676` squash-merged at `6580869d56982de6a445edc73d58c545529d2037`.

## Coordination

The older `2026-08-11-2331` HostLink audit-owned-revision claim is `COMPLETED`; this lane did not alter its one-revision-per-audited-mutation contract.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.
