# Work claim — LOCAL-003 shared native Level Z-chain

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-11T19:43:12+07:00`
- Baseline main SHA: `c7dd212d36677a1d2e005becf8709768fe98d6a1`
- Priority: `LOCAL-003 / P0` — close the semantic/native vertical-placement split using the available licensed local runtime

## Reserved scope

Implement and qualify the coherent native Level Z-chain using `ElementVerticalPlacementService` as the single semantic source of truth. Preserve exact legacy source-relative geometry when Level references are absent; support Bottom Level only and Bottom + Top Level placement; fail closed for Top-only, missing/ambiguous Levels, non-finite offsets, and invalid vertical ranges.

The reserved chain covers native host placement plus every generated system that derives Z/height from those hosts, so the product cannot expose a partial Level assignment that leaves openings, Curtain output or reinforcement spatially detached.

## Expected surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`, Level reference/stale/health contracts, and focused smoke coverage only where the shared contract needs strengthening.
- BricsCAD wall/GlassWall/WallPier and structural Beam/Column/Slab/StructuralWall/Foundation native builders.
- Door/WallOpening host-relative matching and physical cutter placement.
- Curtain host, LINE/path frame and native panel placement consumption; no topology or ownership redesign.
- Generated longitudinal/tie/stirrup/shape/slab/wall/foundation reinforcement placement consumption; no fabrication-rule invention.
- Direct Draw and Floor/Level assignment UI only after the complete native/dependent chain is coherent and guarded.
- Focused static preflight(s), Core smoke registration, exact-V25 build/runtime probe or sanitized qualification support needed for `LOCAL-003`.
- `docs/LOCAL-AGENT-INBOX.md` and the existing Level/local handoff documents for exact scenario/evidence status.

## Excluded scope

- No Create Similar work reserved by `2026-08-11-chatgpt-web-create-similar.md`.
- No Workspace multi-selection policy work reserved by `2026-08-11-chatgpt-web-gpt56sol-workspace-multi-policy.md`.
- No modeless schedule/revision viewer identity work reserved by `2026-08-11-chatgpt-web-modeless-viewer-project-identity.md`.
- No Core Navigation/Review/Interchange/Rules mutation atomicity audit reserved by `2026-08-11-chatgpt-web-core-mutation-atomicity.md`.
- No Room Finish mutation/regeneration lane reserved by `2026-08-11-chatgpt-web-gpt56sol-room-finish-mutation-safety.md`.
- No Curtain panel topology, clipping, ownership or P01-P12 implementation; this claim only makes existing Curtain output consume the shared vertical placement.
- No `LOCAL-001` baseline expansion or `LOCAL-004` `QS3DSYNCSOURCE` qualification/implementation.
- No B4D/ED2/proxy parity, physical L/T/X junction output, polygon rebar topology, standard-specific fabrication rules, licensing, signing, installer, release or GitHub Actions dispatch.
- No standalone QS3D executable or AutoCAD adapter work.

## Validation plan

- Re-fetch current `main` before each write and before integration; inspect concurrent changes for every touched builder.
- Run existing Level reference smoke/preflight plus focused new guards for native shared-service consumption, legacy compatibility, full dependent-family coverage and fail-closed unsupported states.
- Run Core Release build/smoke and aggregate local preflights on the exact candidate SHA.
- Compile `QS3D.BricsCAD.V25` x64/Release against the installed BricsCAD V25 managed assemblies.
- Use disposable mm and m drawings to verify legacy/no-Level, Bottom-only, Bottom+Top, Top-only refusal, missing/deleted/ambiguous Level refusal, Level edit invalidation, host-opening-Curtain-rebar Z alignment, Undo, save/reopen and multi-DWG isolation.
- Keep native/private evidence under gitignored `artifacts/`; commit only a sanitized exact-SHA summary. Do not claim `LOCAL_PASS` until the exact runtime matrix passes.

## Coordination

This is a deliberately broad but single coherent vertical-placement lane because splitting hosts from their dependents would create semantic/native divergence. The active neighboring claims visible immediately before publication cover Create Similar, Workspace policy, modeless viewer identity, scoped Core atomicity and Room Finish; none owns native Level placement. Agents may continue those lanes and unrelated Curtain topology, but should not independently wire `BottomLevelId` / `TopLevelId` into native builders or Level assignment UI while this claim is `ACTIVE`.

If a concurrent commit touches a required builder for another feature, this agent will re-read and preserve that implementation, limiting changes to vertical-placement consumption and its tests.

## Completion condition

The complete shared Level Z-chain is integrated on current `main`, deterministic tests/static guards pass, the exact-V25 build and required sanitized runtime matrix are recorded, `LOCAL-003` status/evidence is updated truthfully, no dependent family remains on a conflicting Z calculation, and this claim is marked `COMPLETED` with exact pushed SHA(s).

## 2026-08-11 source-safe wave heartbeat

- Synced baseline: `origin/main@e085c82732d80eb25ba3dcb719715d6ca077b37f` before final validation.
- Implemented: geometry-driving Level-key invalidation, transitive dependent stale propagation, pure effective-span preparation, and the first wall/structural native host adapter wave.
- Safety boundary: native mutation and production quantity regeneration reject configured Level references through `LevelReferenceNativeIntegrationPolicy.EnsureQualified(...)`; the policy still qualifies no category.
- Automated exact-SHA evidence: `36c170dcaf75e0018e3370a42978e63849530602` passed 453/453 aggregate gates, Core Release/smoke, adapter V25 x64 Release, offline WPF, and licensed V25 NETLOAD/Ribbon/Palette runtime. Plugin SHA-256: `762e28dafb1ac9427602efdf032f2fd6cc6e7511e2869f4861c9d52b30d1bcc7`.
- Qualification boundary: `FULL INTERACTIVE/PRIVATE-DWG PRODUCT MATRIX = NOT_RUN`; customer release qualification remains false. This automated baseline does not qualify Level Z geometry.
- Remaining: straight/curved opening cutters, AutoHost, Curtain LINE/path frame/panel/live fingerprints, generated rebar/mesh/shape families, UI, and the complete mm/m V25 runtime matrix.
- Claim remains `ACTIVE`; LOCAL-003 remains `OPEN / PENDING_LOCAL`.
