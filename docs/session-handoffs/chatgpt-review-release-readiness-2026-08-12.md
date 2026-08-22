# ChatGPT Review / Release Readiness Handoff — 2026-08-12

> **Purpose:** persistent repository handoff for the 2026-08-12 review/fix session. This note exists so the chat transcript is not required to resume safely.
>
> **Truth boundary:** current source, canonical docs, claims, tests, and native/runtime evidence override this dated note if they later diverge.

## 1. Repository-side work completed by this session

A confirmed Core integrity bug in `HostLinkService.UnlinkOpening` was fixed and regression-covered:

- Claim start: `5cb6b32d81ce40a2b52bf2fe204425888170c5d0`
- Source fix: `727a3d084e06bea725f688d21f532ee303e24bef`
- Regression: `0674505c1b08d691a4a57afcf979d0b345db9b21`
- Claim closeout: `672152ec550edd78634fe73ee11d866ec2b4bdbe`

Defect contract: an opening with an existing but blank/whitespace `HostWallId` must fail closed before metadata, dependency, dirty-state, audit, or `ChangeVersion` mutation. The regression preserves all of those surfaces on the rejected path.

The session also audited representative Core persistence, semantic mutation, geometry, dependency, family/instance, generated-handle, adapter/build, and release areas. Candidate areas already owned by other active/recent claims were deliberately excluded rather than patched speculatively.

## 2. Latest release evidence observed in this session

The newest release run available when this handoff was written was:

- Workflow: `QS3D Cloud V25 Preview Build & Release`
- Run: `#57`
- Run ID: `31610444779`
- Head SHA: `bb066a38befd172e844c2e5b576e74ca07ac9fdc`
- Result: `FAILURE`
- Confirmed failing stage: deterministic Core smoke tests.
- In the observed sequence, source guards and the Core Release build had already passed before the deterministic smoke blocker.

Run #57 is **not** current-main qualification. `main` advanced after that run. In particular, post-#57 test-fixture repairs include:

- `f695ca233bf338ecf50bc7ecd93e24cc6aafb5df` — `test(export): keep door/opening XLSX sanitization fixture range-valid`.

That commit makes the sanitization fixture use valid nonzero width/height so the test reaches its intended sanitization assertion instead of failing an earlier numeric-range guard.

The release workflow is manually triggered (`workflow_dispatch`). Repository CI policy for this agent session prohibits manually dispatching/rerunning GitHub Actions. Therefore this session did **not** manufacture a fresh release result for the newer `main` snapshot.

### Required qualification follow-up

An owner/authorized release operator should manually run `QS3D Cloud V25 Preview Build & Release` against the then-current `main` HEAD. If deterministic Core smoke still fails, capture the exact failing smoke/exception and continue from that current snapshot. Do not infer the current result from run #57 because #57 tested the older `bb066a38...` source/test tree.

No statement in this handoff promotes current `main` to V25/V26 production-ready without that fresh release/native evidence.

## 3. BricsCAD V21 / V22 / V23 / V24 compatibility assessment

Current architecture observed during the session:

- `QS3D.Core`: `netstandard2.0`
- BricsCAD V25 adapter: `net48`, exact V25 host assemblies supplied externally via `BRICSCAD_V25_DIR`
- BricsCAD V26 adapter: `net8.0-windows`, exact V26 host assemblies supplied externally via `BRICSCAD_V26_DIR`, with intentional source sharing from the V25 adapter
- Canonical product boundary at the time of review: V25 + V26 only

Official BricsCAD framework documentation reviewed during the session indicates:

- V21: .NET Framework 4.5.1
- V22: .NET Framework 4.8
- V23: .NET Framework 4.8
- V24: .NET Framework 4.8

The architectural conclusion is:

### V22 / V23 / V24

Technically feasible, but **not currently declared supported or qualified**. The safe implementation model is one dedicated host-major assembly/project per version, targeting `net48`, referencing that exact BricsCAD major's SDK assemblies (for example separate `BRICSCAD_V22_DIR`, `BRICSCAD_V23_DIR`, `BRICSCAD_V24_DIR` inputs), then compiling and qualifying each major independently.

Do **not** relabel or rename the V25 binary as a V22/V23/V24 binary. Shared adapter source is acceptable only after exact-SDK compile and runtime verification prove the APIs used are available and behavior is correct for that major.

### V21

Not a drop-in extension of the present architecture. BricsCAD V21's .NET Framework 4.5.1 host cannot consume the current `netstandard2.0` Core baseline. V21 requires a deliberate legacy compatibility strategy: for example a compatible Core target/compatibility layer plus a dedicated V21 host adapter, followed by separate build and runtime qualification. This is materially more work/risk than V22-V24.

No speculative V21-V24 project files were added in this session because exact legacy BricsCAD SDK compilation/runtime evidence was not available. Adding unqualified adapter projects would create a false support claim.

## 4. What is safe to forget with the chat transcript

The repository now contains the material session handoff:

- the completed HostLink claim and its source/regression commits;
- this release/readiness checkpoint;
- the V21-V24 compatibility conclusion and qualification boundary.

The chat transcript is therefore **not required as a recovery source** once the companion handoff claim is marked `COMPLETED`.

## 5. What remains external to this session

These are qualification/future-product tasks, not uncommitted chat-only work:

1. Manually run the V25 preview/release workflow on the then-current `main` HEAD and resolve any remaining deterministic smoke failure revealed by that fresh run.
2. Perform licensed/native BricsCAD V25/V26 qualification where the release policy requires it.
3. If legacy-host support is approved as product scope, implement and qualify V22/V23/V24 as dedicated exact-SDK adapters first; treat V21 as a separate compatibility program.

This handoff intentionally does not claim that every runtime path in the repository is bug-free; it records what was fixed, what was statically audited, and exactly which release/native gates still require authoritative evidence.
