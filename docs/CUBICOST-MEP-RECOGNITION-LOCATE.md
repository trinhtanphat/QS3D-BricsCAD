# Cubicost-style MEP recognition and clash Locate — BricsCAD V25

Updated: 2026-08-15 (UTC+7)
Issue: #1636
Upstream adapter: #1619 / PR #1635

## Scope

This lane removes the first-wave adapter's private hard-coded classification methods and moves recognition policy into host-neutral `QS3D.Core`.

Delivered surfaces:

- `MepRecognitionRule` — configurable token rule with explicit priority, source scope, discipline/category and optional MEP kind.
- `MepRecognitionProfile` — deterministic rule evaluation with fail-closed `Unmatched` and `Ambiguous` outcomes.
- `MepRecognitionProfiles.CreateDefault()` — canonical compatibility profile for the first V25 adapter conventions.
- `QS3DMEPTAKEOFF` and `QS3DMEPCLASH` now consume the Core profile instead of owning private classification tables.
- `QS3DMEPCLASHLOCATE` recomputes the current read-only clash review, lets the operator choose a pair, resolves the two live handles and places only those entities into the BricsCAD implied selection.

No project/DWG semantic mutation, exact-solid boolean interference, modeless palette, issue persistence, V26, OCR or RVT work is introduced by this lane.

## Recognition contract

A rule contains:

- stable `Id`;
- integer `Priority`;
- `Mep`, `Structure` or `Architecture` discipline;
- category;
- one or more case-insensitive tokens;
- source scope: layer, block name, or either;
- MEP kind for MEP rules.

Evaluation is deterministic:

1. matching rules are considered in descending priority order;
2. only rules at the highest matching priority participate in the decision;
3. if those top rules resolve to the same semantic classification, the result is `Matched`;
4. if top rules disagree, the result is `Ambiguous` and no discipline/category/MEP kind is returned;
5. if no rule matches, the result is `Unmatched`.

The adapter accepts only `Matched`. `Ambiguous` and `Unmatched` are skipped instead of guessed.

## Default profile compatibility

The default profile preserves the first adapter's precedence, including:

- CableTray before the embedded `CABLE` token;
- Conduit, Duct, Pipe, Cable, Fitting, Accessory, Equipment and Fixture conventions;
- Structure before Architecture;
- Beam, Column, Foundation and generic Structure categories;
- Wall, Slab/Floor, Ceiling, Roof and generic Architecture categories.

The default profile is a compatibility baseline, not a universal BIM/CAD naming standard. Project-specific profiles can construct different `MepRecognitionRule` sets without adding BricsCAD dependencies to Core.

## Clash Locate flow

`QS3DMEPCLASHLOCATE` is intentionally read-only with respect to DWG/project data:

1. consume PICKFIRST or interactive source selection through `EntitySnapshotReader`;
2. prompt for the same non-negative clearance used by `QS3DMEPCLASH`;
3. resolve live handles and read native `GeometricExtents` `ForRead`;
4. run Core broad-phase hard/clearance clash detection;
5. keep pairs with at least one recognized MEP participant;
6. display at most the first 200 deterministic review pairs to avoid unbounded command-line output;
7. prompt for one pair index;
8. call `CadHandleService.SelectIfAny` for the two handles.

Changing the implied selection is transient editor state. The command does not open entities `ForWrite`, append/erase/transform geometry, create a sidecar, or mutate QS3D semantic state.

## Remote-safe validation

Deterministic Core smoke coverage verifies:

- case-insensitive default recognition;
- CableTray priority over embedded Cable;
- block-name recognition;
- explicit custom priority;
- same-priority conflicting rules return `Ambiguous` with no guessed classification;
- unmatched content returns `Unmatched` with no guessed classification.

The feature source guard is `scripts/preflight-cubicost-mep-recognition-locate.py`, automatically discovered by `scripts/preflight-all.py`.

## LOCAL_ONLY qualification

Final V25 interaction truth remains local-only because a licensed BricsCAD V25 runtime is required.

On a disposable DWG, validate the exact integrated SHA with:

1. case-insensitive layer and block names for each default MEP kind;
2. an overlap token such as `CABLETRAY` proving CableTray priority;
3. unknown naming proving fail-closed skip;
4. `QS3DMEPCLASHLOCATE` with known hard clash and clearance pairs;
5. pair selection proving exactly the two live handles become implied selection;
6. one stale/deleted handle control proving partial/zero live selection is reported safely;
7. two DWGs proving no cross-document handle/selection leakage;
8. no sidecar/project/DWG mutation attributable to the commands.

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`. Source review or Core smoke coverage must not be reported as licensed V25 runtime PASS.
