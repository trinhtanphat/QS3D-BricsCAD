# QS3D Local MCP / BLT parity plan

Updated: 2026-08-11 (UTC+7)

## Decision

Build a separate local automation/MCP project for clean-room behavioral comparison between BLT and QS3D inside a licensed Windows CAD environment. The MCP is a development/qualification tool, not part of the shipping QS3D BricsCAD plugin.

Recommended future repository: `trinhtanphat/QS3D-CAD-MCP`.

`QS3D-BricsCAD` remains the product repository and remains a BricsCAD V25 x64 .NET plugin. The MCP repository should orchestrate local CAD/runtime testing and produce sanitized evidence/tasks/patch guidance back to `QS3D-BricsCAD`.

## Owner/operator constraint

The owner is not expected to know how to operate BLT, BricsCAD, or AutoCAD. Therefore the local MCP must be designed as **operator-light / zero-training** rather than as a recorder that depends on the owner manually demonstrating every workflow.

Normal operation should be:

```text
install local bridge once
-> agent detects available CAD installations/plugins
-> agent starts the target application
-> agent discovers visible BLT/QS3D surfaces
-> agent executes bounded scenarios
-> agent captures UI + CAD/database evidence
-> agent compares behavior
-> agent creates an actionable parity finding
-> remote/source agent fixes QS3D
-> local agent rebuilds/reloads/retests
```

Human intervention is acceptable only for actions that cannot safely or legally be automated, such as initial product activation/login, license dialogs, UAC/administrator approval, installation of proprietary software, or an explicit owner/product decision.

The MCP must never require the owner to know a BLT command name, explain CAD terminology, or manually draw a reference model just to make progress when the behavior can be discovered safely by the agent.

## Clean-room boundary

BLT is a workflow/UX reference only.

Allowed:

- observe documented or user-visible behavior;
- inspect visible Ribbon/menu/palette/property labels;
- invoke user-visible commands through normal application surfaces;
- record prompts, screenshots, timing and resulting DWG-visible geometry/properties;
- compare observable results against QS3D;
- use owner-supplied sample drawings and documentation when the owner is entitled to use them.

Not part of this plan:

- copying BLT source code;
- decompiling proprietary assemblies to reproduce implementation internals;
- committing BLT DLLs, license files, credentials, installers or proprietary assets;
- bypassing licensing/access controls;
- making QS3D runtime-dependent on BLT binaries.

BLT ZIP/install material, if supplied, stays local and gitignored. The reusable output is a behavioral contract/scenario/evidence summary, not copied product code or assets.

## Architecture

### 1. MCP server

A local MCP server exposes high-level CAD and parity tools to an authorized agent. It owns session policy, tool schemas, capability discovery, timeouts, cancellation, evidence IDs and safety gates.

Suggested implementation: .NET 8 or TypeScript/Node for the MCP host, with a strong preference for the simplest Windows packaging/deployment path. BricsCAD-native probe code remains a separate .NET Framework-compatible DLL where required by V25.

Core tools should include concepts equivalent to:

```text
host.status
host.capabilities
host.start_app
host.stop_app
host.focus_app
host.screenshot

cad.status
cad.open_drawing
cad.new_drawing
cad.save
cad.save_as
cad.close_drawing
cad.active_document
cad.run_command
cad.send_escape
cad.undo
cad.redo
cad.selection
cad.inspect_entity
cad.list_entities
cad.measure
cad.database_snapshot
cad.command_log

ui.snapshot
ui.find_control
ui.invoke_control
ui.set_value
ui.send_keys
ui.click
ui.capture_region

qs3d.status
qs3d.health
qs3d.project_snapshot
qs3d.semantic_element
qs3d.generated_ownership

scenario.discover
scenario.record
scenario.run
scenario.replay
scenario.compare
scenario.export_evidence
```

Exact public tool names can change during implementation; the capability boundaries should not.

### 2. Windows host controller

A separate out-of-process Windows controller performs process/window discovery and UI Automation. It should prefer Microsoft UI Automation/control patterns and keyboard commands over absolute screen coordinates.

Coordinate/image-based clicking is fallback-only and must include confidence checks plus before/after screenshots.

Responsibilities:

- discover installed BricsCAD/AutoCAD versions;
- discover active windows/dialogs and PID/process identity;
- foreground/focus management;
- Windows UI Automation tree inspection;
- safe keyboard/mouse automation;
- screenshots and bounded screen recording when explicitly enabled;
- dialog classification;
- detect hangs/timeouts;
- detect unexpected app/document switches.

### 3. BricsCAD probe plugin

Create a small development-only BricsCAD probe DLL loaded only on authorized local test machines. It must not become a runtime dependency of the shipping QS3D plugin.

The probe provides structured information that screen automation cannot reliably obtain:

- active Document identity and path;
- database fingerprint;
- command start/end/cancel/fail events;
- selected ObjectId/Handle/type/layer;
- ModelSpace/PaperSpace entity inventories;
- extents and safe geometric measurements;
- Solid3d/entity counts;
- transaction-safe read-only snapshots;
- Undo/Redo markers when safely observable;
- save/reopen/session events;
- UCS/WCS and system-variable snapshots relevant to a scenario.

Read-only inspection is the default. Any probe write helper must be explicitly scoped to test fixture setup and must never silently mutate a production/private drawing.

### 4. QS3D instrumentation adapter

Where useful, expose a local-only development endpoint/command that reports sanitized QS3D state without bypassing normal mutation contracts:

- ProjectId and active DWG binding;
- semantic element IDs/categories/families;
- generated ownership metadata;
- dirty/change version;
- Health All summary;
- source/generated Handle relationships;
- active Family/Instance properties when safe.

This instrumentation should reuse canonical QS3D services and remain read-only unless a specific existing product command is being invoked.

### 5. Scenario engine

Scenarios must be data-driven so the agent can discover once and replay many times.

Recommended representation: YAML or JSON with a versioned schema.

Each scenario records:

```text
id
product target (BLT / QS3D)
required capabilities
fixture
preconditions
UI/command actions
CAD inputs/points
expected prompts/transitions
capture checkpoints
observable outputs
cleanup
comparison rules
risk level
```

Coordinates in model space should be semantic/test-fixture coordinates, not hardcoded screen pixels.

### 6. Evidence store

All runtime evidence is local by default, for example:

```text
artifacts/
  runs/<run-id>/
    manifest.json
    environment.json
    steps.jsonl
    screenshots/
    cad-before.json
    cad-after.json
    command-log.txt
    compare.json
    summary.md
```

Raw evidence must not be committed automatically. A separate sanitizer produces a small Markdown/JSON summary safe for GitHub.

Every evidence bundle should bind at least:

- MCP version/commit;
- QS3D exact SHA when testing QS3D;
- Windows version;
- BricsCAD/AutoCAD version/build;
- BLT version when observable;
- drawing fixture identity/hash where safe;
- scenario schema/version;
- timestamps;
- success/failure/cancel classification.

## Autonomous discovery mode

Because the owner is not a CAD/BLT operator, autonomous discovery is a first-class feature rather than an optional convenience.

Use a bounded state machine:

```text
DISCOVER
-> RECORD
-> NORMALIZE
-> REPLAY
-> COMPARE
-> FINDING
```

### DISCOVER

The agent inventories visible application surfaces without mutating production files:

- Ribbon tabs/panels/buttons;
- menus/context menus;
- palettes/windows;
- command aliases obtainable through normal visible UI/documentation;
- field/property labels;
- command prompts after explicitly invoking a selected visible action on a disposable fixture.

Discovery must be bounded and reversible. Do not randomly invoke destructive commands.

### RECORD

For one discovered workflow, record the visible interaction sequence and CAD state checkpoints on a disposable drawing.

### NORMALIZE

Convert UI-specific interactions into semantic actions such as `pick_point`, `select_entity`, `set_numeric_property`, `finish`, `cancel`, `repeat`, `save_reopen`.

### REPLAY

Replay the normalized scenario against BLT and QS3D independently.

### COMPARE

Compare behavior rather than pixel-perfect appearance. Example dimensions:

- discoverability;
- prompts and input sequence;
- preview behavior;
- OSNAP/ORTHO/POLAR/UCS compatibility;
- created source/native entities;
- dimensions/placement;
- property inheritance;
- repeated authoring;
- cancellation/rollback;
- Undo/Redo grouping;
- editing/grips;
- save/reopen;
- quantity/schedule visibility;
- health/ownership integrity.

### FINDING

Produce an actionable result:

```text
Feature
Observed BLT behavior
Observed QS3D behavior
Parity status
Severity
Evidence IDs
Likely QS3D area/files
Source-safe fix candidate
Local-only requalification scenario
```

## BLT parity matrix

Maintain a generated/curated matrix with at least these workstreams:

1. Wall, Beam, Column, Slab, Foundation.
2. Structural Wall, Glass/Curtain Wall, Wall Pier.
3. Door, Window if applicable, Opening/host behavior.
4. Direct Draw UX: preview, chain/repeat, flip, offset, dynamic input, OSNAP, ORTHO, POLAR, UCS.
5. Native modify: selection, grips, MOVE, ROTATE, STRETCH and source/generated behavior.
6. Grid/Level/vertical placement.
7. Rebar: Beam, Column, Slab, Wall, Foundation, stirrups/ties and BBS.
8. Room/finish/material workflows.
9. Family/Type/Instance/property inheritance.
10. Quantities/BQ/schedules/tables/tags.
11. Layout/View/Viewport/PaperSpace where relevant.
12. Undo/Redo/cancel/failure atomicity.
13. Save/reopen/SaveAs/multi-DWG.
14. Ribbon/palette/context-menu/keyboard/DPI/Unicode UX.
15. Performance on bounded synthetic fixtures, then owner-approved representative private drawings.

Each feature should track `NOT_DISCOVERED`, `BLT_RECORDED`, `QS3D_RECORDED`, `GAP`, `SOURCE_FIX_READY`, `PENDING_LOCAL`, `PASS`, or `NOT_APPLICABLE`.

## Fail-closed safety requirements

The local bridge has authority over desktop software, so safety is mandatory.

- Bind every CAD operation to PID + active Document fingerprint.
- Abort when the active application/document unexpectedly changes.
- Prefer disposable copies of fixtures; never overwrite an original private DWG by default.
- Default all filesystem access to an allowlisted workspace.
- No arbitrary PowerShell/cmd execution through generic MCP tool parameters; expose bounded operations instead.
- No credential extraction, browser-password access, license-file copying or secret collection.
- No arbitrary process injection.
- Do not expose a general unauthenticated TCP listener. Prefer loopback-only transport or Windows named pipes plus a per-install secret/session token.
- Support an emergency stop that cancels the current scenario and releases synthetic input.
- Every mutating desktop/CAD action is journaled.
- Detect modal dialogs and stop on unknown/high-risk dialogs rather than blind-clicking.
- Never auto-accept license/EULA/purchase/activation/overwrite/destructive-security prompts.

## Version/capability handshake

At connection time return a capability manifest rather than assuming one machine layout:

```text
mcpVersion
hostControllerVersion
probeVersion
windowsVersion
installedCadProducts[]
activeCadProduct
cadBuild
bltDetected
qs3dDetected
qs3dSha
supportedProbeCapabilities[]
supportedUiCapabilities[]
```

Scenarios declare required capabilities and fail as `UNSUPPORTED` rather than producing false failures when the local machine lacks them.

## Installation experience

The user should not have to manually configure MCP JSON, environment paths or BricsCAD load paths unless automatic discovery fails.

Target bootstrap:

```text
Install-QS3DCadMcp.ps1
```

or a small signed installer later.

Bootstrap should:

1. install the local MCP/host controller;
2. discover BricsCAD installations;
3. install/register the development probe in an isolated test location;
4. create the local evidence/workspace directories;
5. write the MCP client configuration for the chosen agent host where technically supported;
6. run `doctor` diagnostics;
7. show a simple PASS/WARN/FAIL summary.

Do not automatically install BLT, BricsCAD, AutoCAD, bypass licensing, or download proprietary dependencies.

## Doctor command

Provide one owner-friendly command such as:

```text
qs3d-cad-mcp doctor
```

It should answer without CAD expertise:

- Is MCP reachable?
- Is BricsCAD V25 found?
- Is BLT found?
- Is QS3D found?
- Is the probe loaded/compatible?
- Can a disposable DWG be opened?
- Can UI Automation see the host window?
- Is the evidence directory writable?
- Is the machine ready for autonomous parity runs?

The final line should be an unambiguous `READY`, `ACTION_REQUIRED`, or `BLOCKED`, with exact next action when human intervention is unavoidable.

## Relationship with QS3D local-agent queue

`QS3D-BricsCAD/docs/LOCAL-AGENT-INBOX.md` remains the authoritative product LOCAL_ONLY queue. The MCP does not replace it.

Instead:

1. a QS3D inbox item/scenario requests local evidence;
2. the MCP executes an applicable scenario;
3. sanitized evidence is attached/referenced;
4. the exact QS3D SHA is qualified;
5. the inbox item can be updated to PASS only when its required evidence is actually satisfied.

New MCP-discovered product gaps should become actionable QS3D issues or inbox updates rather than a second competing product backlog.

## Repository split

Create a separate repository rather than placing the whole MCP inside `QS3D-BricsCAD`.

Recommended split:

```text
QS3D-BricsCAD
  shipping plugin
  Core/domain
  product tests
  local qualification queue

QS3D-CAD-MCP
  MCP server
  Windows host controller
  BricsCAD probe
  optional future AutoCAD probe/adapter
  scenario engine
  BLT/QS3D parity scenarios
  local installer/doctor
  evidence sanitizer
```

Reasons:

- MCP has a different runtime/deployment lifecycle;
- it may eventually support both BricsCAD and AutoCAD;
- it must remain development-only rather than accidentally shipping with QS3D;
- local proprietary dependencies/evidence need stronger isolation;
- MCP releases can evolve without changing the QS3D plugin package;
- permissions/security can be narrower and audited separately.

Start the new repo private unless/until its artifact boundaries and secret/proprietary exclusions are proven safe. The repo must contain no BLT binaries, BricsCAD SDK DLL redistribution, private DWGs, credentials or raw proprietary evidence.

## Proposed new-repo structure

```text
QS3D-CAD-MCP/
  README.md
  AGENTS.md
  LICENSE-or-private-policy.md
  src/
    Qs3d.CadMcp.Server/
    Qs3d.CadMcp.Host/
    Qs3d.CadMcp.Protocol/
    Qs3d.CadProbe.BricsCAD.V25/
    Qs3d.CadProbe.AutoCAD/          # future/optional
  scenarios/
    discovery/
    modeling/
    modify/
    documentation/
    lifecycle/
  schemas/
    scenario.schema.json
    evidence.schema.json
    capability.schema.json
  scripts/
    install.ps1
    uninstall.ps1
    doctor.ps1
  tests/
    protocol/
    scenario-engine/
    sanitizer/
  docs/
    ARCHITECTURE.md
    SECURITY.md
    CLEAN-ROOM-BLT.md
    OPERATIONS.md
    PARITY-MATRIX.md
  artifacts/                       # gitignored
  local-reference/                 # gitignored; BLT/private inputs only
```

## Implementation phases

### P0 — bootstrap and safe host

- repo skeleton;
- MCP transport;
- capability handshake;
- process/window discovery;
- screenshot/UI Automation primitives;
- allowlisted workspace;
- audit journal;
- doctor command;
- emergency stop;
- schemas/tests.

### P1 — BricsCAD structured probe

- BricsCAD V25 probe DLL;
- active-document fingerprint;
- command lifecycle events;
- entity/selection/database snapshots;
- measurement;
- save/reopen/undo observation;
- probe/MCP version handshake.

### P2 — QS3D integration

- detect exact QS3D SHA/version;
- read-only project/ownership/health adapter;
- run existing LOCAL_ONLY qualification scenarios through the MCP;
- evidence sanitizer compatible with `LOCAL-AGENT-INBOX.md`.

### P3 — autonomous BLT discovery

- Ribbon/menu/palette inventory;
- bounded visible-action exploration on disposable drawings;
- prompt/state recording;
- semantic action normalization;
- replayable BLT scenarios.

### P4 — differential parity engine

- paired BLT/QS3D runs;
- geometry/property/lifecycle/UI comparisons;
- parity matrix updates;
- issue/inbox-ready findings;
- regression replay after QS3D fixes.

### P5 — advanced native UX

Use evidence to close the real V25 gaps already identified in QS3D: DrawJig/transient preview, repeated authoring, OSNAP/ORTHO/dynamic input, native modify/grips, Undo, save/reopen, Ribbon/DPI and other runtime behaviors.

## Definition of MCP MVP

Do not call the MCP useful merely because it responds to a ping.

MVP is reached when an owner who does not know CAD can:

1. run one install/bootstrap command;
2. run `doctor` and receive `READY`;
3. ask an agent to compare one simple workflow, initially Wall Direct Draw;
4. the agent opens a disposable drawing and runs/captures the BLT reference without owner-provided command knowledge where discoverable;
5. the agent runs the corresponding QS3D workflow;
6. structured CAD + UI evidence is produced;
7. a deterministic comparison identifies at least one match/gap;
8. raw evidence remains local;
9. a sanitized finding can be used to modify `QS3D-BricsCAD`;
10. the same scenario can be replayed after the fix.

## First reference scenario

Start with Wall authoring because it exercises the most important common infrastructure while remaining bounded:

- create disposable empty drawing;
- discover/invoke BLT wall creation surface;
- record first point/next point/finish interaction;
- capture preview if any;
- record thickness/height/offset controls when exposed;
- capture resulting native entities/measurements;
- test cancel before commit;
- test one Undo;
- save/reopen;
- repeat against `QS3DDRAWWALL`;
- compare prompts, preview, object state, dimensions, ownership/Health, cancellation, Undo and persistence.

After this end-to-end path works, expand to Beam, Column and Slab before attempting broad automated exploration.

## Current repository implications

This plan complements, rather than replaces, the existing QS3D boundaries:

- QS3D remains a BricsCAD-hosted plugin.
- source-only agents still must not manufacture V25 runtime PASS.
- `LOCAL-AGENT-INBOX.md` remains the product runtime queue.
- GitHub Actions remain owner-controlled/manual-only.
- the MCP is intended to make those local gates executable by an agent even when the owner does not know BLT/BricsCAD/AutoCAD operation.
