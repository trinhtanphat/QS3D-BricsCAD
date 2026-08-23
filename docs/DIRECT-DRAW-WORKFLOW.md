# QS3D Direct Draw workflow — product requirement

Updated: 2026-08-10 (UTC+7)

## Status

This document records the owner product requirement and the current implementation contract for QS3D authoring.

QS3D remains a **BricsCAD V25 x64 .NET plugin** running inside BricsCAD. The native BricsCAD viewport, editor and DWG database remain the CAD host. Direct Draw is not a request to create a standalone CAD engine or separate QS3D executable.

Existing capture workflows such as `LINE/POLYLINE -> QS3DWALL/QS3DBEAM/... -> QS3DBUILD3D` remain valid compatibility workflows. Direct Draw adds a faster authoring layer and must not break existing semantic capture, ownership, health, regeneration or schedule behavior.

Current implementation direction on `main`:

- P0 Direct Draw: `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB`.
- Production repeated linear Direct Draw: `QS3DDRAWWALLREPEAT`, `QS3DDRAWBEAMREPEAT`, and active-Family route `QS3DDRAWACTIVEREPEAT`.
- P1 Direct Draw: `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`.
- Host-aware opening authoring: `QS3DDRAWDOOR`, `QS3DDRAWOPENING`; Auto Host is part of authoring, while physical boolean cutting remains explicit through the established cut commands.
- BLT-style wall compatibility flow is deliberately **capture -> edit -> build**, not capture-and-build in one command.
- Instance inspector exposes source-derived geometry such as `LengthM`, `AreaM2`, `VolumeM3`, `PerimeterM` and source `Layer` as read-only CAD provenance instead of pretending those measurements are independent editable Family dimensions.

Exact licensed evidence is now recorded for the bounded repeated Wall/Beam slice at clean final candidate SHA `e5725e96eed6dcebb46370c33e6f8a88e2cc2b68`: BricsCAD V25.2.10 and V26.2.07 both passed exact-plugin `NETLOAD`, DrawJig preview, repeated segments, Enter/physical ESC, planar UCS, document-switch isolation, whole-command Undo/Redo, save and fresh-process cold reopen. This does not certify the broader quick/advanced prompt, Auto Host/reference, private-DWG or release matrix.

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
-> set or inherit thickness/height/source-relative offset
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

For **existing CAD references**, however, the BLT-style compatibility flow intentionally remains:

```text
LINE/open POLYLINE
-> QS3DWALL              # capture semantic only
-> review/edit Family or Instance properties
-> QS3DBUILD3D           # explicit native 3D commit/rebuild
```

This separation is important: the user must have a chance to inspect/change thickness, height, material and supported offsets before the native solid is committed.

---

## Priority commands

### P0 — implemented authoring surface

- `QS3DDRAWWALL` — direct ArchitecturalWall/Tường Gạch authoring.
- `QS3DDRAWBEAM` — direct Beam authoring from a picked linear path.
- `QS3DDRAWCOLUMN` — direct Column authoring from insertion point and guarded rectangular dimensions.
- `QS3DDRAWSLAB` — direct Slab authoring from an interactively created closed boundary.

### P1 — implemented guarded authoring surface

- `QS3DDRAWGLASSWALL`;
- `QS3DDRAWWALLPIER`;
- `QS3DDRAWSTRUCTWALL`;
- `QS3DDRAWFOUNDATION`;
- `QS3DDRAWOPENING`;
- `QS3DDRAWDOOR`.

Future work should continue converging common point acquisition, Family defaults, preview, transaction, semantic capture and native generation instead of creating unnecessary independent geometry systems.

### Production repeated linear mode

`QS3DDRAWWALLREPEAT` and `QS3DDRAWBEAMREPEAT` implement the native high-frequency loop for linear Wall/Beam authoring:

1. the first editor point is normalized from the current planar UCS into WCS exactly once;
2. each next endpoint is acquired by a database-free `DrawJig` profile strip;
3. an accepted segment creates a canonical WCS `LINE`, semantic owner and native `Solid3d` through the existing `DirectDrawCommands.ExecuteDirect` pipeline;
4. the next preview starts at the accepted endpoint;
5. Enter or ESC removes only the in-progress transient and exits; accepted segments remain;
6. unit, planar-UCS, Model Space, active-DWG, project and active-Family context are revalidated before each commit;
7. structural builders suppress their nested marker while the repeated command owns one whole-command semantic/native Undo transition, so one native Undo/Redo restores the matching segment set instead of leaving semantic references to removed CAD.

The transient does not append entities, write XData, create/cache a project, capture semantics or generate ownership. A cancellation before the first accepted segment therefore leaves no CAD/project/semantic/native residue. This is a new interaction surface over the canonical pipeline, not a second authoring engine.

---

## Required UX behavior

Direct Draw commands should feel native inside BricsCAD:

1. Start from Ribbon/Hub or command line.
2. Acquire points/entities through the BricsCAD editor, with normal ESC/cancel behavior.
3. Use the active compatible Family when one is selected; otherwise offer/create only a safe starter Family according to current semantic rules.
4. Show or accept important parameters without forcing the user through a long modal sequence.
5. Prefer live/transient preview when practical, but never persist preview geometry as project ownership.
6. On commit, create/update the semantic element and the owned native/generated geometry as one user operation.
7. Select/highlight the newly created semantic/generated object and synchronize the QS3D Workspace.
8. Make repeated drawing efficient when that matches normal CAD authoring expectations.
9. Reject malformed configured Family dimensions instead of silently replacing invalid values with defaults.
10. Keep source-derived measurements read-only in Instance scope when CAD source is authoritative.

UI should be Vietnamese-first and consistent with the current compact BLT-style QS3D Ribbon/WPF workflow.

---

## Geometry expectations

### Wall

Minimum wall behavior:

- pick start/end points directly in the viewport;
- allow a multi-segment path only when it can reuse the current guarded wall-footprint semantics safely;
- use Family/instance thickness, height and supported source-relative bottom offset;
- create source/semantic provenance compatible with existing wall health, opening host, quantity and regeneration paths;
- generate/update native 3D without requiring the user to call `QS3DBUILD3D` separately when using Direct Draw;
- preserve `LengthM` as a measurement derived from the real LINE/open POLYLINE source in the compatibility workflow.

`AxisLeftOffsetM` / `AxisRightOffsetM` may exist in Family/UI data, but **do not claim BLT-equivalent native axis/face offset geometry until the exact semantic contract is defined and the builder applies it deterministically**. The reference screenshot shows these controls, but guessing their geometric meaning would be worse than an explicit guarded gap.

Do not weaken current guards for bulges, self-intersections, invalid offsets, ownership or unsupported junction-solid reconciliation merely to make Direct Draw accept every shape.

### Beam

Minimum behavior:

- pick start/end points;
- use active/inherited beam section and elevation/source-relative offset parameters;
- create semantic Beam plus native result through existing guarded Beam generation rules;
- preserve compatibility with Beam rebar/stirrup workflows.

### Column

Minimum behavior:

- pick insertion point;
- choose/inherit a supported rectangular profile and dimensions;
- create a deterministic source representation/provenance suitable for the existing Column semantic/native/rebar workflow;
- generate the native result immediately.

Do not invent unsupported arbitrary column profiles in the first Direct Draw iterations.

### Slab

Minimum behavior:

- interactively acquire a closed planar boundary, similar to native polyline authoring;
- use active/inherited slab thickness and supported source-relative offset;
- create semantic Slab and native result immediately;
- preserve compatibility with quantity and Slab rebar mesh workflows.

Unsupported geometry should fail clearly rather than producing invalid solids.

### Door / Opening

Current guarded behavior:

- pick two plan points; their plan length is authoritative `WidthM`;
- prompt/inherit positive `HeightM` and non-negative sill/boolean-clearance values;
- reject malformed configured Family numerics instead of silently masking them;
- create a real LINE source and exactly one semantic Door/WallOpening;
- Auto Host only the newly-created opening and require a unique host;
- rollback source + semantic authoring state when no unique host is found;
- keep global/physical boolean cutting as a separate explicit operation until a targeted cut transaction is proven safe.

---

## Workspace property semantics

The BLT-style property panel has two different kinds of data and QS3D must keep them distinct.

**Editable Family / Instance design properties** include values such as thickness, width, height, material and supported offsets. Invalid positive geometry values (for example zero/negative thickness or height) should be rejected at edit time instead of waiting for a native builder to fail later.

**Source-derived CAD measurements** include `LengthM`, `AreaM2`, `VolumeM3`, `PerimeterM` and source `Layer` when they come from the captured CAD reference. These are read-only provenance in Instance scope. To change a wall's measured length, edit its source LINE/open POLYLINE and recapture/rebuild; do not type an unrelated semantic length that disagrees with the DWG source.

`BottomOffsetM` and `TopOffsetM` are currently **source-relative offsets**, not full BLT top/bottom level references. UI labels and documentation must not call them absolute elevations. Full level-reference behavior should be implemented as a separate explicit contract rather than inferred from a screenshot.

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

Where current capture APIs require an existing persistent source entity, the resulting source must remain a real BricsCAD-owned DWG entity with stable Handle provenance. Do not create fake handles or semantic-only geometry that bypasses current source/live-CAD health contracts.

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
- if a persistent source entity is created as part of the command, failure must remove/rollback that operation-owned source;
- never erase or replace foreign/ambiguous generated handles;
- nested authoring commands must re-check the active DWG before delegating to command surfaces that resolve `MdiActiveDocument` internally;
- post-commit UI synchronization failures must not destructively undo an otherwise successful CAD/project commit.

Add deterministic Core tests where logic is CAD-independent and focused static/runtime regression coverage for adapter behavior.

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
Existing drawing -> Capture -> review/edit -> QS3DBUILD3D
New object       -> Direct Draw -> semantic/native commit in one authoring flow
```

Both paths must converge on the same semantic/native model after creation.

---

## Ribbon / discoverability

Primary Direct Draw actions should be visible from the main QS3D authoring UI and not require command memorization.

Architecture/structure authoring group includes the current P0/P1 commands for Wall, Beam, Column, Slab, Glass Wall, Wall Pier, Structural Wall, Foundation, Door and Opening as they become available in the Ribbon/Hub.

Existing Capture/Bóc chọn actions remain separately available for converting source CAD.

---

## Acceptance criteria

Do not call Direct Draw complete based only on command declarations or mocked dialogs.

For each supported command:

1. command is registered uniquely and exposed through the intended UI;
2. user can create the supported object starting from an ordinary BricsCAD drawing without pre-drawing its source geometry manually;
3. the resulting source/semantic/generated ownership is compatible with existing selection, save/reopen and regeneration;
4. quantity/schedule paths see the object through the normal semantic model;
5. edits/invalidation do not bypass current Family/Instance and stale rules;
6. cancel/failure does not leave partial semantic/generated state;
7. Health All/ownership checks remain clean for a valid object;
8. existing capture commands continue to work;
9. deterministic tests/static preflights cover the shared architecture;
10. every unqualified row still requires exact-current-sha licensed interactive runtime evidence before being described as production-ready; the recorded #3612 repeated Wall/Beam evidence qualifies only that bounded row.

Runtime validation should include at minimum Wall, Beam, Column and Slab creation, save/reopen, regenerate, selection sync, undo/cancel behavior and representative DWG screenshots. Door/Opening runtime validation additionally needs unique-host, ambiguous-host, no-host and explicit physical-cut scenarios.

---

## Agent priority note

When an agent is asked to make QS3D more BLT-like, improve drawing UX, improve demos, add authoring tools, or `continue all`, check this document before inventing another capture-only workflow.

**Product direction:** new geometry should increasingly be authorable directly through QS3D inside BricsCAD, while capture commands remain the path for converting pre-existing CAD geometry.

Do not dispatch GitHub Actions merely because source implementation or documentation changes. Follow `CI_POLICY.md`; build/runtime/release require separate explicit owner authorization.
