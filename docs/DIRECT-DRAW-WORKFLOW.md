# QS3D Direct Draw workflow — product requirement

Updated: 2026-08-10 (UTC+7)

## Status

This document records an explicit owner product requirement for future QS3D work.

QS3D must remain a **BricsCAD V25 x64 .NET plugin** running inside BricsCAD. The native BricsCAD viewport, editor and DWG database remain the CAD host. Direct Draw is not a request to create a standalone CAD engine or separate QS3D executable.

Existing capture workflows such as `LINE/POLYLINE -> QS3DWALL/QS3DBEAM/... -> QS3DBUILD3D` remain valid compatibility workflows. The requirement below adds a faster authoring layer and must not break existing semantic capture, ownership, health, regeneration or schedule behavior.

---

## Owner intent

The current semantic-capture workflow is useful for converting existing drawings, but it is too indirect for day-to-day modeling and product demos.

QS3D should provide **BLT-style direct authoring commands** where the user starts a QS3D command, picks geometry directly in the BricsCAD viewport, supplies or inherits Family/Type parameters, and receives the semantic object plus native/generated 3D result in the same workflow.

Target user experience:

```text
QS3DDRAWWALL
-> pick first point
-> pick next point(s)
-> choose/inherit Family
-> set or inherit width/height/offset
-> finish
-> semantic wall + owned native 3D result exist immediately
```

The user should not normally need to manually run:

```text
LINE
-> QS3DWALL
-> QS3DBUILD3D
```

for a brand-new wall they are authoring from scratch.

---

## Priority commands

Implement the direct-authoring family incrementally, starting with the most demonstrable architecture/structure workflows.

### P0

- `QS3DDRAWWALL` — direct ArchitecturalWall/Tường Gạch authoring.
- `QS3DDRAWBEAM` — direct Beam authoring from a picked linear path.
- `QS3DDRAWCOLUMN` — direct Column authoring from insertion point/profile parameters or a guarded footprint workflow.
- `QS3DDRAWSLAB` — direct Slab authoring from an interactively created closed boundary.

### P1 candidates

After the P0 architecture is stable and shared authoring infrastructure exists, consider:

- `QS3DDRAWGLASSWALL`;
- `QS3DDRAWWALLPIER`;
- `QS3DDRAWSTRUCTWALL`;
- `QS3DDRAWFOUNDATION`;
- `QS3DDRAWOPENING`;
- `QS3DDRAWDOOR`.

Do not create separate one-off command implementations if a shared Direct Draw authoring service/tooling can safely handle common point acquisition, Family defaults, preview, transaction, semantic capture and native generation.

---

## Required UX behavior

Direct Draw commands should feel native inside BricsCAD:

1. Start from Ribbon/Hub or command line.
2. Acquire points/entities through the BricsCAD editor, with normal ESC/cancel behavior.
3. Use the active compatible Family when one is selected; otherwise offer/create only a safe starter Family according to current semantic rules.
4. Show or accept important parameters without forcing the user through a long modal sequence.
5. Prefer live/transient preview when practical, but never persist preview geometry as project ownership.
6. On commit, create/update the semantic element and the owned native/generated geometry as one user operation.
7. Select/highlight the newly created semantic object and synchronize the QS3D Workspace.
8. Make repeated drawing efficient: after one object finishes, allow continuing the same command/family when that matches normal CAD authoring expectations.

UI should be Vietnamese-first and consistent with the current compact BLT-style QS3D Ribbon/WPF workflow.

---

## Geometry expectations

### Wall

Minimum desired P0 wall behavior:

- pick start/end points directly in the viewport;
- allow a multi-segment path only when it can reuse the current guarded wall-footprint semantics safely;
- use Family/instance width, height and supported axis offsets;
- create source/semantic provenance compatible with existing wall health, opening host, quantity and regeneration paths;
- generate/update native 3D without requiring the user to call `QS3DBUILD3D` separately.

Do not weaken current guards for bulges, self-intersections, invalid offsets, ownership or unsupported junction-solid reconciliation merely to make Direct Draw accept every shape.

### Beam

Minimum desired P0 beam behavior:

- pick start/end points;
- use active/inherited beam section and elevation parameters;
- create semantic Beam plus native result through existing guarded Beam generation rules;
- preserve compatibility with Beam rebar/stirrup workflows.

### Column

Minimum desired P0 column behavior:

- pick insertion point;
- choose/inherit a supported rectangular profile and dimensions;
- create a deterministic source representation/provenance suitable for the existing Column semantic/native/rebar workflow;
- generate the native result immediately.

Do not invent unsupported arbitrary column profiles in the first Direct Draw iteration.

### Slab

Minimum desired P0 slab behavior:

- interactively acquire a closed planar boundary, similar to native polyline authoring;
- use active/inherited slab thickness/elevation parameters;
- create semantic Slab and native result immediately;
- preserve compatibility with quantity and Slab rebar mesh workflows.

The first implementation may deliberately support only the same guarded footprint family already proven by current semantic/native code; unsupported geometry should fail clearly rather than producing invalid solids.

---

## Architecture requirement

Direct Draw must be a thin authoring/orchestration layer over existing product invariants, not a second independent model system.

Prefer this conceptual flow:

```text
Editor point acquisition
-> transient/direct-draw plan
-> guarded source representation
-> existing semantic capture/project transaction contract
-> existing deterministic/native generator
-> ownership + stale/health metadata
-> Workspace selection sync
```

Where current capture APIs require an existing persistent source entity, agents may introduce a carefully designed source-creation step, but the resulting source must remain a real BricsCAD-owned DWG entity with stable Handle provenance. Do not create fake handles or semantic-only geometry that bypasses current source/live-CAD health contracts.

Reuse existing:

- Family/Instance inheritance and active-Family logic;
- `ProjectStateSnapshot` rollback semantics where applicable;
- BricsCAD transaction/document locking patterns;
- generated-handle ownership policy;
- stale/invalidation lifecycle;
- deterministic native geometry builders;
- Health All / Release Readiness expectations;
- semantic selection synchronization.

Do not duplicate geometry formulas already implemented in Core/native builders.

---

## Atomicity and cancellation

A Direct Draw command must not leave partial project or CAD state after cancel/failure.

Required behavior:

- ESC before commit leaves no semantic object and no persistent generated output;
- invalid geometry leaves no half-created semantic object;
- if semantic capture succeeds but native generation fails, restore/rollback according to the existing project/CAD transaction boundaries rather than leaving an apparently finished object with inconsistent ownership;
- if a persistent source entity must be created as part of the command, failure must remove/rollback that source when it belongs exclusively to the failed new Direct Draw operation;
- never erase or replace foreign/ambiguous generated handles.

Add deterministic Core tests where logic is CAD-independent and focused static/runtime regression coverage for the adapter behavior.

---

## Existing commands must remain supported

Do **not** remove or silently repurpose these existing workflows:

- `QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`;
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`;
- `QS3DBUILD3D`;
- recognition/B4D capture workflows.

They are still needed for existing DWGs and conversion of already-drawn CAD geometry.

Direct Draw is an additional creation workflow:

```text
Existing drawing -> Capture commands
New object       -> Direct Draw commands
```

Both paths must converge on the same semantic/native model after creation.

---

## Ribbon / discoverability

When implemented, the primary Direct Draw actions should be visible from the main QS3D authoring UI and not require command memorization.

Suggested architecture/structure authoring group:

- Tường
- Dầm
- Cột
- Sàn

The Ribbon button may invoke `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB` respectively.

Existing Capture/Bóc chọn actions should remain available separately for converting source CAD.

---

## Acceptance criteria for P0

Do not call Direct Draw complete based only on command declarations or mocked dialogs.

For each P0 command:

1. command is registered uniquely and exposed through the intended UI;
2. user can create the supported object starting from an ordinary BricsCAD drawing without pre-drawing its source geometry manually;
3. the resulting source/semantic/generated ownership is compatible with existing selection, save/reopen and regeneration;
4. quantity/schedule paths see the object through the normal semantic model;
5. edits/invalidation do not bypass current Family/Instance and stale rules;
6. cancel/failure does not leave partial semantic/generated state;
7. Health All/ownership checks remain clean for a valid object;
8. existing capture commands continue to work;
9. deterministic tests/static preflights cover the shared architecture;
10. exact-current-sha behavior still requires licensed BricsCAD V25 interactive runtime validation before being described as production-ready.

Runtime validation should include at minimum Wall, Beam, Column and Slab creation, save/reopen, regenerate, selection sync, undo/cancel behavior and representative DWG screenshots.

---

## Agent priority note

When an agent is asked to make QS3D more BLT-like, improve drawing UX, improve demos, add authoring tools, or `continue all`, check this document before inventing another capture-only workflow.

**Product direction:** new geometry should increasingly be authorable directly through QS3D inside BricsCAD, while capture commands remain the path for converting pre-existing CAD geometry.

Do not dispatch GitHub Actions merely because this document exists or because Direct Draw source is implemented. Follow `CI_POLICY.md`; build/runtime/release require separate explicit owner authorization.
