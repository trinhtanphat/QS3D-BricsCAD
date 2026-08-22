# Work claim — Regeneration preview subset input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-preview-subset-freshness`
- Registered: `2026-08-12T00:36:00+07:00`
- Completed: `2026-08-12T00:39:00+07:00`
- Baseline main SHA: `f209d97920b20e4463aebb6c853562065e06ec14`
- Reservation commit: `857613da7e1b805538864a49013b73ce0a8e8571`
- Priority: P1 — bind subset-preview freshness before caller-controlled target enumeration.

## Defect fixed

`RegenerationPreviewService.PreviewSubset(...)` enumerated the caller-provided `IEnumerable<string>` through `CanonicalPreviewTargets(elementIds, project.Elements.Count)` before `PreviewInternal(...)` captured `project.ChangeVersion`. A lazy target sequence could therefore mutate/touch the project during enumeration; the resulting preview was then stamped with the post-enumeration revision and missed that the project changed while target scope was being established.

Subset preview now captures the live `ChangeVersion` and semantic element count before caller enumeration, uses the captured cardinality for the bounded canonical target list, and passes the immutable revision into `PreviewInternal`. `PreviewInternal` verifies that revision before detached snapshot creation, so a target enumerable that changed project state fails closed instead of silently rebasing freshness.

## Published commits

- `a0f28f1854f3ba79d1c624b362ef50c85deca667` — `fix(regeneration): bind subset preview before target enumeration`.
- `a188e13e996d9496d4dc9a1caed38cd5446fa8a6` — `test(regeneration): cover subset preview input freshness`.
- `82ce93cc99ab931f84b40322041a968ed7e402db` — `test(regeneration): pin subset preview input freshness`.

## Preserved contract

- Full-project preview remains revision-bound before its detached snapshot.
- Canonical target validation, cardinality bound, detached regeneration, health/revision diff and guarded apply behavior remain unchanged.
- The focused smoke uses a lazy one-target sequence that calls `project.Touch()` during enumeration and requires immediate stale failure before any regeneration quantity reaches the live element.
- The static gate pins `ChangeVersion -> element count -> target enumeration -> immutable PreviewInternal` ordering and the pre-snapshot freshness check.

## Validation notes

Current source and focused smoke were read from `main` immediately around publication; the dedicated static preflight is committed and auto-discovered by the repository preflight convention. This connector-only lane did not execute Core smoke or repository Python gates, so no executable PASS is claimed. Concurrent `main` changes were preserved via current-blob/current-main writes; no force-push was used. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.

## Excluded scope

No RegenerationEngine/DependencyGraph rewrite, no native/UI work and no release workflow changes.

## Completion condition

Satisfied for the remote-safe source/static contract: subset preview freshness begins before caller-controlled target enumeration, focused regression/static coverage is on current `main`, and this reservation is released.
