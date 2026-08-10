# QS3D — local-agent open-work addendum

Updated: 2026-08-10 (UTC+7)

This file extends `docs/LOCAL-V25-QUALIFICATION.md` and `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` for work that must not be marked complete by a source-only agent. Current `main` remains authoritative if implementation moves ahead of this note.

## Rules

- Fetch/pull the latest `main` before every implementation batch and again before merge.
- Work against one clean exact SHA for runtime evidence.
- Do not commit BricsCAD proprietary DLLs, private/customer DWGs, signing secrets, machine credentials or unsanitized screenshots/logs.
- Do not dispatch GitHub Actions or publish a release unless the owner separately and explicitly requests that operation.
- A source/preflight review is not proof of BricsCAD V25 native behavior.

## P0 — `QS3DCURTAIN3D` whole-command recovery/atomicity

### Current boundary

`src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs` intentionally runs semantic regeneration, LINE host replacement, path host replacement, LINE frame replacement and path-frame replacement as separate stages. Each canonical host/frame builder is internally cross-layer atomic, but a later stage can fail after an earlier stage has already committed. The command correctly reports `PARTIAL COMMIT`; do not remove that warning until a stronger contract is implemented and proven.

### Acceptable implementation directions

Choose one architecture and document it before coding:

1. **Shared native transaction orchestration**: refactor the participating host/frame builders so the high-level command can prepare all work and commit one BricsCAD transaction while semantic state remains rollback-capable; or
2. **Recoverable compensation journal**: snapshot the complete previous host/frame semantic ownership plus enough native replacement state to deterministically restore the previous valid family when a later stage fails.

Do not fake whole-command rollback by restoring only `.qsdb` metadata after native solids have committed. Do not erase foreign/ambiguous generated objects during compensation.

### Required source acceptance

- pre-plan and validate all semantic/rule/ownership/count limits before the first destructive native mutation;
- deterministic ownership for both LINE and open/bulged path host/frame families;
- injected failure after each stage proves either no mutation or deterministic restoration to the previous complete valid state;
- rollback/compensation failure is surfaced as a distinct health/readiness error, never as success;
- `QS3DHEALTHALL` and `QS3DRELEASECHECK` detect an interrupted journal/recovery state;
- save/reopen does not lose a pending recovery marker if a journal design is used;
- add a dedicated `preflight-curtain-orchestration-atomicity.py` only after the contract is real.

### Required V25 proof

On the same exact SHA, test LINE, straight open POLYLINE and bulged POLYLINE GlassWall cases. Force failure in path-host, LINE-frame and path-frame stages. PASS means the drawing and semantic project end in the previous valid complete state or in a clearly persisted recoverable state defined by the design; no silent half-host/half-frame result is allowed.

## P1 — native Direct Draw preview / repeated authoring

### Why this remains local-V25 work

Current authoring uses guarded BricsCAD editor point acquisition/rubber-band behavior and already creates real source + semantic owner + native result. A richer BLT-familiar preview (wall thickness/profile, column/slab footprint, opening width/host cue) and repeated authoring loop depend on exact V25 `DrawJig`/editor behavior. Do not introduce guessed BricsCAD API calls from memory.

### Target contract

Primary source surfaces:

- `src/QS3D.BricsCAD.V25/DirectDrawCommands.cs`
- `src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs`
- `src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs`
- current Family/Type authoring state and native builders.

Implement only after compiling against the installed V25 assemblies:

- transient preview reflects the selected Family/Type dimensions/profile but creates **no database entity and no semantic mutation** before final acceptance;
- ESC at first point, during preview, or at the next repeated iteration leaves zero source/semantic/generated residue for the cancelled iteration;
- repeated mode can create consecutive independent elements without leaking selection/document state;
- document switch or document close exits safely instead of authoring into another DWG;
- World, translated and planar 30°/45°/90° UCS behave consistently; tilted/3D UCS remains fail-closed unless separately generalized and accepted;
- opening preview must not physically cut a host before explicit cut acceptance.

### V25 acceptance

Run Wall, Beam, Column, Slab plus one P1 family and Door/Opening. Verify mouse preview, ORTHO/OSNAP interaction, ESC, UNDO, repeated creation, save/reopen, HiDPI and document switching. Capture only sanitized evidence.

## P0 — Level references → native placement/UI integration

### Current boundary

`docs/LEVEL-REFERENCES.md`, `ProjectFloorService`, `ElementVerticalPlacementService` and Level-reference Health/Release diagnostics establish the semantic contract, but the current native CAD builders and authoring UI intentionally do not expose or consume Bottom/Top Level placement yet. Do not expose Level assignment in UI before the host solids **and every dependent generated system** resolve the same effective bottom/top/height contract.

### Required integration scope

At minimum, review and coherently integrate:

- native host builders for Wall/GlassWall/Column/Beam/Slab/StructuralWall/Foundation/Stair/Railing where vertical placement applies;
- Door/WallOpening host-relative placement and opening-cut geometry;
- Curtain host and LINE/path frame builders;
- generated Column/Beam/Shape/Tie/Stirrup/Slab/Wall/Foundation rebar/mesh that derives Z, height or placement from its host;
- Direct Draw P0/P1/Door/Opening initial semantic values;
- regeneration/rebuild, stale/fingerprint logic and save/reopen;
- Floor/Level Manager assignment UI only after native integration is coherent.

Use `ElementVerticalPlacementService` as the semantic source of truth. Do not independently reimplement Bottom/Top Level arithmetic in each builder. Legacy elements with no Level references must retain existing source-relative behavior exactly.

### Source acceptance

- Bottom only resolves absolute bottom from Level elevation + explicit offset while preserving legacy effective height;
- Bottom + Top resolves absolute bottom/top and effective height from both Levels;
- Top without Bottom, missing Level IDs, non-finite offsets and `top <= bottom` remain fail-closed;
- no double-application of legacy `BottomOffsetM` after `BottomLevelId` is present;
- host and all dependent generated geometry use the same resolved vertical placement;
- changing/renaming a Level does not silently corrupt references; deleting a referenced Level remains guarded;
- Level mutation marks every affected host/dependent generated family stale or deterministically rebuilds it according to the existing dependency contract;
- Health All / Release Check catch host/generated vertical-placement divergence where it can be diagnosed deterministically;
- add/update static preflights only after the native contract is actually wired.

### Required V25 proof

On one exact SHA, test legacy/no-Level and Level-enabled cases in both mm and m drawings. Cover World UCS plus planar 30°/45°/90° UCS, save/reopen and rebuild. For representative host families, verify source/native/generated geometry Z/height before and after Bottom Level change, Top Level change, offsets, invalid/deleted references and UNDO. PASS requires no semantic/native split-brain and no change in legacy geometry when Level references are absent.

## P0 — commercial license enforcement wiring

### Current boundary

Core verification exists in `src/QS3D.Core/Licensing/LicenseVerifier.cs`, but the BricsCAD adapter must not enforce an invented commercial policy. The owner/product decision is required before startup/command gating can be called complete.

### Owner inputs required before implementation

Record explicit values for:

- product/SKU identifier(s);
- perpetual/subscription/trial model;
- seat, machine, Windows-user or named-user binding;
- trial duration, expiry and grace behavior;
- license file location and per-user vs per-machine scope;
- public verification key plus key-rotation/version strategy;
- offline-only vs optional activation service;
- machine-replacement/deactivation/recovery process;
- which commands remain available when unlicensed (for example diagnostics/license UI only);
- clock rollback/offline expiry policy;
- support/admin override policy.

Private license-signing keys must remain outside Git/release artifacts.

### Implementation acceptance after policy exists

- adapter startup loads/verifies the license without blocking BricsCAD startup with an unhandled exception;
- command gating is centralized, deterministic and testable rather than copied into every command;
- invalid signature, wrong SKU, expired license and binding mismatch fail closed with actionable UI;
- allowed diagnostics/license-management commands remain usable when blocked;
- valid offline license survives restart and save/reopen without changing project data;
- key rotation supports an explicit version/migration path;
- Core deterministic tests cover policy-independent verification and adapter preflight locks the chosen gating surface;
- local V25 test proves startup, valid/invalid/expired cases and clean uninstall/reinstall.

Do not mark this PASS until the owner policy above is filled with real values.

## P0 — production signing / trust material

`docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` remains canonical for Authenticode, timestamp, installer/updater and clean-customer lifecycle proof. This addendum adds one stop condition: **never weaken BricsCAD `SECURELOAD`, Windows trust settings or signature verification merely to make a test pass**. Fix the package/trust chain instead.

## P1 — repository/legal distribution model

This is an owner/legal decision, not an engineering default. Do not add or change a root `LICENSE` by assumption. Before commercial 1.0, the owner must choose one intentional model such as public/open-core, source-available commercial, or private production source with public docs/samples. After that decision, a local/release agent may align package notices, third-party notices and release contents with the chosen terms.

## P1 — performance / UX runtime matrix

After functional V25 qualification passes, run representative stress/UX cases on the exact candidate SHA:

- 100/125/150/200% DPI and Vietnamese Unicode labels/paths;
- light/dark host theme where applicable;
- large selections near configured batch caps;
- large semantic projects, Health/ReleaseCheck and XLSX export;
- repeated Hub/Palette refresh while switching two or more DWGs;
- focus/isolate/section/locate on a representative large model;
- memory/object growth after repeated rebuilds and document close/reopen.

A performance issue must include a reproducible sanitized fixture description, command sequence and measured before/after evidence. Do not optimize by removing ownership/health/fail-closed guards.

## Required sanitized close-out format

When a local agent completes any section above, append or update a safe status note containing:

```text
Exact SHA: <40-char SHA>
Environment: Windows x64 + BricsCAD V25 <edition/build>
Gate: <Curtain orchestration | DrawJig/repeated authoring | Level integration | licensing | signing | performance>
Result: PASS/FAIL/BLOCKED
Automated exact-SHA runner: PASS/FAIL
Interactive scenarios: PASS/FAIL
Source commits/PRs: <ids>
Known blockers: <sanitized list>
Evidence location: local artifacts path only, no private content committed
```

`PASS` is allowed only after the required runtime/product/engineering inputs for that gate exist and the affected scenarios are rerun on the fixed exact SHA.
