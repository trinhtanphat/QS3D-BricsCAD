# Work claim — Wall junction ownership Unicode integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:03:00+07:00`
- Completed: `2026-08-12T08:07:00+07:00`
- Baseline main SHA: `e391d9c2f44d48e6b66daa7e2e75736ed5eadd97`
- Priority: evidence-driven remote-safe ownership identity integrity

## Reason

`WallJunctionOwnershipPlanner` canonicalized source-segment ids, wall element ids, project ids, and drawing fingerprints by trimming/uppercasing/length bounding, then hashed packed group/fingerprint keys with default `Encoding.UTF8`. Default UTF-8 replacement fallback could silently map distinct malformed UTF-16 identities to replacement-character bytes before SHA-256.

## Changed scope

Canonical wall-junction ownership identity text must now be well-formed Unicode / strict-UTF8 encodable, and SHA-256 input uses the same strict UTF-8 encoder. Trimming, uppercasing, length bounds, packed key layout, numeric formatting, occurrence assignment, token prefixes, collision guards, planner limits and valid Unicode behavior remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Geometry/WallJunctionOwnershipPlanner.cs`
- `tests/QS3D.Core.SmokeTests/WallJunctionOwnershipUnicodeSmoke.cs`
- this claim file

## Completion

- Claim commit: `7c3f6b96cf3de9369c6ff1819eca6eef724972a9`.
- Implementation commit: `9345adcf39a4c632083d7ff125d84dbb9a982534` — validate canonical ownership identities with `UTF8Encoding(false, true)` and hash `WJP1/WJX1/WJF1` source keys with that strict encoder.
- Regression commit: `0c62a263cb394ed027a4bf82d08e725b4a6f4e5d` — cover malformed high/low surrogate source-segment, wall, project and drawing identities plus valid supplementary-Unicode case/token determinism.
- Validation actually performed:
  - re-fetched the strict UTF-8 field, unchanged planner result path, canonicalization helper and SHA helper after the whole-file replacement;
  - re-fetched the dedicated smoke and confirmed malformed identity rejection plus valid `WJP1/WJX1/WJF1` deterministic behavior is covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

The latest prior wall-junction ownership lane was the completed bounded-enumeration work. The direct `WallJunction` result-snapshot lane is also completed and disjoint. No newer overlapping ownership-token claim was present when this lane was reserved.

## Completion condition

Satisfied: current `main` fails closed on malformed Unicode before `WJP1/WJX1/WJF1` SHA-256 hashing, valid Unicode remains deterministic, focused regression coverage is present, and this claim is released as `COMPLETED`.
