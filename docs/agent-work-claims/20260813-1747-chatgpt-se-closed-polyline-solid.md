# Work claim — SE closed-polyline to 3D Solid

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-se-20260813`
- Registered: `2026-08-13T17:47:00+07:00`
- Baseline main SHA: `455759887d9a34ac5f91a7aff3914abc47f2009c`
- Priority: owner-requested continuation of the supplied SE workflow reference: active Family/Type on Workspace panel -> `SE` -> select closed 2D polylines -> native 3D Solids.

## Reserved scope

Implement and complete the BricsCAD V25 command `SE` for converting selected closed planar 2D polylines into native 3D Solid output using the canonical active QS3D Family/Type and its category-specific dimensions/elevation semantics. The command must preserve source polylines, support multi-selection, fail closed on stale/invalid active-Family context, isolate invalid selections without corrupting successful output, persist source/family/category ownership metadata through existing QS3D mechanisms, and provide deterministic command-line/status summary feedback.

Target categories: Architectural Wall, Structural Wall, Beam, Column, Slab, Door, Stair, Foundation, limited to categories for which current source exposes a safe closed-footprint extrusion/native-generation contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/` command, selection and existing native solid/family integration surfaces discovered from current source.
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` only if V26 source-link parity requires the new shared V25 command source.
- `scripts/` focused deterministic/static SE contract guard where host execution cannot run remotely.
- `tests/` CAD-independent regression coverage only where current architecture exposes a deterministic seam.
- focused SE workflow documentation and this claim closeout.
- `docs/LOCAL-AGENT-INBOX.md` only if the implementation introduces or materially changes an exact licensed-BricsCAD runtime scenario that is not already covered by an existing LOCAL item.

## Excluded scope

- No changes to the completed active-Family basic Line/Rectangle/Circle drawing lane except reuse of its canonical active-Family freshness/context mechanisms.
- No Curtain, Source Reconcile, Schedule/Quantity/Family Manager UI, release/versioning, CI workflow, packaging, signing or unrelated responsive/dark-theme work.
- No replacement of current Direct Draw semantic builders with a parallel architecture.
- No claim of licensed BricsCAD V25/V26 runtime PASS from remote/source evidence.
- No GitHub Actions dispatch under this request.

## Validation plan

- Re-read current `main` and overlapping claims before source mutation and again before integration.
- Reuse existing active-Family/project freshness, Model Space transaction, semantic ownership and native Solid3d builder patterns from current source.
- Add deterministic/static regressions that assert unique `SE` registration, closed-polyline validation, source preservation, active-Family binding, category dimension resolution, multi-selection result accounting and safe failure behavior.
- Review exact final diff on the newest `main`; licensed BricsCAD interactive/native Solid3d qualification remains LOCAL_ONLY and will be parked in the canonical inbox only if not already covered.

## Coordination

An earlier same-agent claim existed only on private branch `feat/se-polyline-to-solid` at `83d2e25fbebf06b30b7729152961267f08feda63`; per repository policy it did not reserve work because it was not on `main`. This main-visible claim supersedes that private reservation. Recent claim history shows the neighboring active-Family basic drawing lane completed via PR #1033 / integration `a456efd50310c92520a903131b0b818157aaec2d`; this SE lane reuses that context but does not modify its primitive drawing behavior. No recent SE-specific main-visible reservation was found before registration.

## Completion condition

`SE` is present on current `main`, binds to the canonical active Family/Type without fallback, accepts only valid closed planar polyline sources, creates supported category-appropriate native 3D solids while preserving source entities and QS3D ownership/provenance, reports partial failures safely, carries focused deterministic/static regression coverage/documentation, and this claim is marked `COMPLETED` with exact integration evidence. Runtime-only proof is handed to the existing/new LOCAL item without being misreported as remote PASS.
