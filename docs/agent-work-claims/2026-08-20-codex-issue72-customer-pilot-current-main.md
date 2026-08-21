# Work claim — issue #72 exact-main customer-style V25 pilot

- Status: `ACTIVE / PENDING_LOCAL`
- Lane-Key: `issue-72`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue72-customer-pilot-20260820`
- Canonical PR: `#3402`
- Validated publication baseline main SHA:
  `577d835872ada46f6521fff1c2e85a4f15cedd46`
- Refreshed: 2026-08-21 (UTC+7), after fetching the GitHub remote, merging the
  then-current `main`, and re-running the licensed exact-candidate gates.
- Runtime target: licensed BricsCAD V25.2.10 x64 on disposable synthetic
  copies only. Exact task candidate `0ae7fb4369172198d25347b9b0d75bdbceead2bb`
  passes the official NETLOAD/Ribbon/Palette baseline and the bounded Level,
  project-lifecycle, Source Reconcile and Curtain P10/P11/P12 gates below.
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

## Licensed exact-candidate continuation — 2026-08-21

The carrier fetched GitHub `main@577d835872ada46f6521fff1c2e85a4f15cedd46`,
merged it without conflict and froze exact task candidate
`0ae7fb4369172198d25347b9b0d75bdbceead2bb`. The matching V25 x64 Release
adapter/Core ProductVersion is
`0.1.0-preview.10081+0ae7fb4369172198d25347b9b0d75bdbceead2bb`; adapter
SHA-256 is
`B725F335AA71E90E9584EA1A6940A6889ACA2E2FDB22D88C2CB3713047268D01`
and Core SHA-256 is
`2A5DCE45CC74EB9248A7079E02835DA81DEFD5A492AAF318CF21FB001CB44A2A`.

The official `scripts/run-local-v25-qualification.ps1` report is `PASS` for
the exact clean candidate:

- manual-only CI policy and generic source preflight: `PASS`;
- aggregate feature preflight: `962/962 PASS`;
- Core Release build: `0 warnings / 0 errors`;
- deterministic Core smoke: `ALL PASS`;
- V25 `Release|x64` build: `0 warnings / 0 errors`;
- offline WPF Theme/Workspace/RightPanel smoke: `PASS`;
- licensed BricsCAD V25.2.10 NETLOAD/Ribbon/Palette runtime: `PASS`.

The official scope remains `source-build+runtime-smoke`; package/signing were
not requested, the full interactive/private-DWG matrix is `NOT_RUN`, and
`customerReleaseQualified=false`.

Additional exact-candidate licensed results use only repository-generated
disposable drawings whose original/restored SHA-256 is
`CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`:

- Level Z passed separately for native Millimeter (`INSUNITS=4`) and Meter
  (`INSUNITS=6`), including Curtain `16/14`, Beam bars/stirrups `4/6`, Level
  health/edit invalidation and graceful no-save cleanup;
- the representative Level/Curtain P11 two-process lifecycle passed native
  Undo/Redo, save/close, cold reopen and disjoint `1/10/15` rebuild with zero
  health issues; both broad product flags intentionally remain false;
- Project Lifecycle schema 3 passed four-document save/reopen identity,
  canonical bind, detached-snapshot isolation, absent/corrupt sidecar
  fail-closed, nine REGEN/REFRESH/FINISH phases, legacy/native unit boundaries
  and explicit unbound Meter override resolution;
- the LOCAL-004 base Source Reconcile matrix returned
  `production_local004_qualified=true`: three successful reconciles,
  generated/ambiguous/multi-DWG refusal, forced rollback, native Undo `BEFORE`,
  Redo `AFTER`, save/cold reopen and complete cleanup;
- Curtain P12 passed two-DWG modeless ownership, wrong-DWG routed-button and
  command refusal, A reactivation, A-destroy window close, B preservation and
  byte-for-byte restoration;
- Curtain P10 passed the production Workspace review for one live generated
  panel and one canonical GlassWall owner: Family review matched, Instance
  scope was active, Health All and Release Check were ready, the project and
  source stayed unchanged, the panel stayed live and health reported zero
  issues. Process, script, private-state, DWG and UI-layout cleanup all passed.
  Issue `#3397` was fixed and closed through PR `#3398` on `main` by production
  fix `752f918c1f24970106bcf246eac9b77f1da0a663` plus static guard
  `088731d9efd539c0ecbee4422d39920a29a82576`; this carrier merged those source
  commits and did not create a competing production UI implementation.

The carrier contains four licensed-runtime-driven automation corrections:
granular Level host-build classification, canonical project binding in the
Level probe, exact-PID discard of the test-owned unsaved-project close prompt,
and unbound explicit-unit override lifecycle expectations. No customer/private
DWG, raw Handle, ProjectId, drawing fingerprint or unsanitized exception text
is committed; raw evidence remains ignored under `artifacts/` or the local
temporary directory.

## Final publication sync audit — 2026-08-21

After the exact-SHA native matrix completed, the carrier fetched GitHub
`main@6d2e6050e4e310904c11959236916defd3bee85c`. The five newer commits belong
to PR `#3394` and modify only `src/QS3D.Core/Features/AddCreateStateMachine.cs`
plus its focused Core smoke; they do not overlap this lane's source, runner or
handoff paths. They were deliberately not merged after qualification because
doing so would change the tested SHA and Core binary identity. The task carrier
therefore remains frozen at `0ae7fb4369172198d25347b9b0d75bdbceead2bb`; any
later integration that adds those Core commits must treat the result as a new
candidate and rerun the applicable exact-SHA gates.

## Local preview installation correction — 2026-08-21

The stale persistent install was independently repaired without replacing it
with the task-branch test build. GitHub preview release
`v0.1.0-preview.10192` is tied to source commit
`afff082096998fa404f08a5e29bcfd9fbc3830dd`. The downloaded ZIP and checksum
sidecar have SHA-256
`ED1FF224A7121770B1A99288E5DDE8F3B1C5F662452ECB710F94D32B2642BC92`
and `2055DBFDF8DD8D69D2D3086F884BF6D1BE5C4F5E0809FBAF5B1C3D2DF7FC439B`.
The package manifest covered 17 files; the verified unblock path removed
Mark-of-the-Web from the 18 extracted package files without weakening
PowerShell policy or BricsCAD trust settings.

The transactional installer replaced the fixed per-user DemandLoad location,
registered `LoadCtrls=4` and 462 commands, and installed adapter SHA-256
`6B090154648CBA7378CD09C2396A620130BD8EFECC80DA0B542FA98A6FBCC7A7`.
A cold DemandLoad-only run loaded that exact fixed-path DLL and passed with
zero process residue, so reboot no longer returns to `0.1.0-preview.2`.
This preview package is intentionally Authenticode `NotSigned`; checksum,
manifest and loaded-binary integrity are verified, but no publisher/timestamp
or commercial trust claim is made.

## Qualification gap

The current candidate now has exact cross-DWG Curtain evidence and the base
native Source Reconcile/Undo/Redo/cold-reopen matrix, superseding those older
gaps. It still does not prove the complete requested one-floor customer pilot
on one exact SHA. Remaining work includes BQ Locate/export continuity across
the full pilot, all requested authoring categories and repeated regeneration,
interactive prompt/grip/jig/cancel ordering, broader Family/H.1 and LOCAL_ONLY
matrices, an explicitly authorized private/reference drawing where required,
and production signing/clean-machine release qualification.

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

