# Work claim — issue #72 exact-main customer-style V25 pilot

- Status: `ACTIVE / PENDING_LOCAL`
- Lane-Key: `issue-72`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue72-customer-pilot-20260820`
- Validated publication baseline main SHA:
  `e4bfb1fb59c61f03a47ae99a196dfeed3b2b1ad6`
- Rebased: 2026-08-21 (UTC+7), after current `main` advanced beyond the
  precursor candidate and after the installed licensed host was detected
  locally again.
- Runtime target: licensed BricsCAD V25.2.10 x64 on disposable local copies
  only; the current exact-main host marker is blocked by the managed
  `CodexSandboxOffline` launcher context described below.
- Supersedes the closed, unmerged branch-only carrier
  `agent/codex/local-only-closeout-20260816` / PR #2143 for this exact #72
  pilot; that older carrier is not an implementation dependency.

## Precursor evidence boundary

The prior exact candidate `7820b72f894534443b53e315608f6a2228533248`
passed all 957 discovered feature preflights, Core Release build/smoke, V25
Release build, offline WPF checks and the licensed NETLOAD/Ribbon/Palette
runtime smoke. Its complete Source Reconcile runtime also passed generated and
ambiguous refusal, forced rollback, native Undo/Redo, multi-DWG isolation and
cold reopen. Those results prove the local environment and the tested commit;
they do not qualify the newer current-main candidate and must be rerun where
the acceptance row requires exact-current evidence.

## Current exact-main attempt — 2026-08-21

The clean detached candidate `8b5ece70d1aaf489e14ac68d7606053def1d08ba`
has the exact parent `6aa3270fb71b68d3039e19569d2d89e74e294712`
(`origin/main` at reservation refresh). The official
`scripts/run-local-v25-qualification.ps1` run proved all source/build gates on
that candidate:

- manual-only CI and generic preflight: `PASS`;
- aggregate feature preflight: `960/960 PASS`;
- Core `Release` build: `0 warnings / 0 errors`;
- deterministic Core smoke: `ALL PASS`;
- installed-reference V25 `Release|x64` build: `0 warnings / 0 errors`;
- offline WPF Theme/Workspace/RightPanel smoke: `PASS`;
- matching adapter/Core ProductVersion:
  `0.1.0-preview.10081+8b5ece70d1aaf489e14ac68d7606053def1d08ba`;
- adapter SHA-256:
  `292BBFFF4903A4C596165C4EECAB7BCFED4BA177E01085343F94124A074D5AB9`;
- Core SHA-256:
  `BE49748E84C58C61B02CD6F096A74C9E760470AC4A1E78F14264E3C51FD4D27A`.

The licensed NETLOAD/Ribbon/Palette step is `NO_RESULT`, not a product
failure: the managed `CodexSandboxOffline` runner timed out after 120 seconds
without receiving `QS3DRUNTIMEPROBE`. The official runner emitted no runtime
metadata, removed its launched host, and a post-run scan found zero BricsCAD
processes. Raw scripts/reports remain ignored under `artifacts/`. The older
`7820b72f...` licensed PASS cannot be promoted to this newer candidate, so the
customer-style pilot and current-main native rows remain `PENDING_LOCAL`.

## Publication-carrier refresh — 2026-08-21

Before handoff, the canonical carrier was rebuilt on then-current `main`
`e4bfb1fb59c61f03a47ae99a196dfeed3b2b1ad6` while retaining the prior remote
carrier as its second parent. The combined tree passed all `960/960` aggregate
feature preflights, registered all 838 runnable smoke classes, passed
`git diff --check`, and completed the Core deterministic smoke suite with
`ALL PASS`. This source-only refresh does not retarget or promote the licensed
evidence from `8b5ece70...`; the required current-candidate native and
interactive rows remain `PENDING_LOCAL`.

The final remote audit then observed `main`
`ccf3c8e182415aab2dca5a7d7f363fb56d0bf97a`, ten commits ahead of that
validated carrier. The drift changes semantic documentation/health source,
tests and one focused preflight; it does not overlap the two documentation
paths in this lane. Outbound Git HTTPS was unavailable and the GitHub write API
was denied by the managed approval policy, so this local carrier could not be
published or refreshed again. The next write-capable session must refresh from
the then-current `main`, retain the prior canonical carrier ancestry, rerun the
applicable source gates and push without force.

## Continuation host-activation audit — 2026-08-21

A fresh Session-1 host-only audit proved that the installed V25 executable can
now expose a responsive `BricsCAD Launcher` window. Its UI Automation tree
offered the licensed Ultimate workspace choices and a focused 2D Drafting
`Start` action. Sending the real focused keyboard activation dismissed the
launcher, but the resulting test-owned BricsCAD process again remained
responsive without creating a CAD/document HWND. The process still ran under
the managed `CodexSandboxOffline` token, so no QS3D DLL, command, DWG or
customer-pilot assertion executed. Every test-owned process was removed and
the post-run BricsCAD count was zero. This narrows the existing result to a
post-launcher, pre-CAD-UI activation boundary and remains `NO_RESULT`, not a
QS3D product failure.

The same continuation rechecked both publication paths. Git HTTPS failed before
authentication because port 443 was unreachable, while an unreferenced GitHub
blob capability probe was rejected with `MCP tool call requires approval, but
approval policy is never`. At that retry, remote `main` was exactly
`ccf3c8e182415aab2dca5a7d7f363fb56d0bf97a`; before closeout it advanced six
more commits to `afff082096998fa404f08a5e29bcfd9fbc3830dd`. The canonical
remote branch remained `7820b72f894534443b53e315608f6a2228533248` with no open
PR, so a future write-capable continuation must repin current `main` rather than
publishing this stale local carrier unchanged.

## Qualification gap

Historical local evidence proves many bounded LOCAL-001 rows and an older
single-floor quantity workbook, but it does not prove the complete requested
customer-style pilot on one current exact SHA. In particular, the same pilot
still needs bounded evidence for BQ Locate, cross-DWG isolation, a real native
edit followed by reconcile/Undo/Redo/rebuild, and stale or unsupported
fail-closed behavior.

## Reserved scope

- Re-run one one-floor disposable pilot on the exact committed candidate using
  production Project/Floor/Family and Wall/Beam/Column/Slab/StructuralWall/
  Foundation/Door commands.
- Require generated ownership, repeated regeneration, gross/net BQ,
  spreadsheet export, BQ Locate, explicit save/close, fresh-process reopen and
  recalculation to remain coherent in the same project.
- In that same qualification, exercise one supported top-level native source
  edit followed by production `QS3DSYNCSOURCE`, Undo/Redo and rebuild; prove a
  second DWG is not bound or mutated.
- Exercise bounded unsupported/stale input against disposable state and require
  refusal without semantic, CAD, audit or persistence mutation.
- Keep scripts, drawings, workbooks, screenshots and raw logs under ignored
  `artifacts/`; publish only allowlisted aggregate evidence.
- Update this claim and the existing `LOCAL-001` handoff/status documentation
  only after exact runtime evidence exists.

## Initial implementation boundary

This is a runtime-qualification lane. No production, adapter, Core, shared
runner, probe, workflow or release source is reserved initially. If licensed
execution exposes a product or reusable-runner defect, stop, publish the
smallest sanitized evidence and register a separate concrete Issue/Lane-Key
before any source edit.

## Exclusions

- Do not rewrite the completed issue #3289 / merged PR #3290 Beam-dependent
  MOVE evidence; its exact licensed PASS is independent evidence only.
- Do not overlap other active local/runtime claims; this lane may observe their
  current-main behavior but does not own their implementation surfaces.
- No private/customer DWG, proprietary BricsCAD DLL, machine path, raw Handle,
  ProjectId, drawing fingerprint, screenshot or unsanitized runtime capture is
  committed.
- No 4D/5D feature work, custom DrawJig/grip implementation, package/signing,
  release publication, manual GitHub Actions dispatch/rerun/cancel or write to
  `main`.
- Issue #72 and the broader customer-release qualification remain open unless
  every applicable exact-SHA acceptance row is actually proven.

## Validation plan

1. Publish this claim on the canonical branch before licensed execution.
2. Require a clean exact branch SHA; preserve every pre-existing BricsCAD
   process and launch/clean up only uniquely test-owned processes. Require Core
   Release build/smoke, generic and aggregate preflights, installed-reference
   V25 `Release|x64` build and matching adapter/Core ProductVersion.
3. Create a fresh disposable pilot directory without overwriting historical
   evidence; launch and clean up only test-owned BricsCAD processes.
4. Validate the exported workbook programmatically and render it for visual QA
   while retaining the raw workbook outside Git.
5. Record only sanitized booleans/counts/totals and exact SHA/build identity.
6. Commit/push the bounded handoff, observe automatic branch evidence when
   applicable, and open/update the single canonical PR. Follow the current
   protected same-task merge contract only after every prompt-required
   LOCAL_ONLY acceptance row is actually satisfied.

