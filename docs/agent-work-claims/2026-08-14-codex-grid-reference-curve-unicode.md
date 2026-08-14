# Work claim — Grid reference-curve Unicode integrity

- Status: `COMPLETED`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T15:34:00+07:00`
- Baseline main SHA: `7fdbf55506ed8d3c1029facf905a6d6221bfd395`
- Issue: `#79`
- Priority: remote-safe first-class Grid/reference identity correctness

## Verified gap

`GridReferenceCurve.NormalizeElementId` rejects blank and overlength IDs but accepts malformed UTF-16 such as an unpaired surrogate. `GridIntersectionPlanner` can consequently publish an otherwise valid intersection carrying an ID that the already-hardened `GridIntersectionIdentityPlanner` rejects at its strict-UTF8 pair-ownership boundary. An accepted first-class Grid reference curve can therefore fail before receiving deterministic `GIP1:` / `GIX1:` identity.

No open PR or active exact claim owns this factory boundary. The completed Grid intersection-identity Unicode claim covers downstream hashing/assignment and remains the preserved contract for this bounded upstream hardening.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs`: require `GridReferenceCurve` element IDs to be strict-UTF8 encodable inside the existing normalization helper.
- `tests/QS3D.Core.SmokeTests/GridIntersectionIdentityUnicodeSmoke.cs`: prove LINE and ARC factories reject malformed high/low surrogates and valid supplementary Unicode flows through curve creation, intersection planning, and deterministic identity assignment.
- this claim document for closeout only.

## Preserved contracts and exclusions

- Preserve ID trimming, 128-character bound, valid Unicode, all intersection geometry/math, pair ordering, SHA-256/token formats, occurrence semantics, and native materialization policy.
- No Level/Floor source, native geometry/UI, LOCAL-002/003/004, BricsCAD/private data, release/signing, or GitHub Actions changes.
- Validate focused Grid intersection/identity gates, Core `Release` build, and full Core smoke; report the next unrelated blocker without expanding scope.

Completion means the source/smoke fix is merged by normal PR, this claim is closed, and the exact merged-main SHA is returned to `/root`.

## Outcome

- Merged source/smoke fix: PR `#1224`, main SHA `010aadc852a097d6704c4e4705e5b814c7185858`.
- `GridReferenceCurve.Line` and `.Arc` now reject malformed high/low surrogate IDs before any intersection can be published; valid supplementary Unicode retains trim-only curve identity and flows through intersection planning into the existing deterministic pair token.
- Core and smoke-project `Release` builds passed with 0 warnings and 0 errors.
- Grid intersection, intersection-identity, system-planner, and spatial-ordering preflights passed.
- Full Core smoke advanced beyond this Grid Unicode fixture and next stopped at the independent `ProjectBrowserReferenceCanonicalitySmoke` padded-Floor expectation; no scope expansion was made.
- No Level/Floor source, native geometry/UI, LOCAL automation, BricsCAD/private data, release/signing, or Actions surface changed.
