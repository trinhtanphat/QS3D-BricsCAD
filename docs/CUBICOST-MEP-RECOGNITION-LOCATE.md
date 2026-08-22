# Cubicost-style MEP recognition profiles and clash Locate

Updated: 2026-08-15 (UTC+7)
Issue: #1636
Upstream adapter: #1619 / PR #1635

## Purpose

The first V25 Cubicost-style MEP adapter proved selected-DWG takeoff and broad-phase clash detection. This follow-up removes adapter-owned hard-coded recognition policy and adds a read-only Locate workflow for reviewing a specific clash pair.

## Configurable Core recognition

`QS3D.Core.Mep.MepRecognitionRule` defines:

- stable rule id;
- explicit priority;
- MEP / Structure / Architecture discipline;
- semantic category;
- one or more case-insensitive tokens;
- source scope: Layer, BlockName or both;
- required `MepElementKind` for MEP rules.

`MepRecognitionProfile.Recognize(layer, blockName)` returns `Matched`, `Unmatched` or `Ambiguous`.

Rules are ordered by descending priority. Only highest-priority matches participate in the final classification. If highest-priority matches disagree on discipline/category/MEP kind, the result is `Ambiguous` and no classification is guessed.

`MepRecognitionProfiles.CreateDefault()` provides the compatibility profile used by the V25 adapter. Specific classes such as CableTray outrank broader embedded tokens such as Cable. The contract is host-neutral, so future company/project profiles can replace the default without changing quantity or clash math.

## Adapter behavior

`MepTakeoffCommands` delegates both MEP and building-discipline classification to the shared Core profile. Unknown and ambiguous results are skipped fail-closed.

All first-wave integrity rules remain:

- native snapshot curve length is the quantity-length source;
- bounding-box diagonals are never quantity lengths;
- `CadUnitService` owns drawing-unit conversion;
- `GeometricExtents` are broad-phase clash envelopes only;
- CAD entities are opened read-only on the document thread;
- no project bootstrap, sidecar write, semantic mutation or CAD geometry mutation.

## `QS3DMEPCLASHLOCATE`

The command:

1. reads PICKFIRST/interactive candidate selection;
2. asks for non-negative clash clearance;
3. runs the same recognition + Core broad-phase clash calculation as `QS3DMEPCLASH`;
4. prints at most 200 numbered MEP-relevant clash pairs per review pass;
5. asks for a pair number using BricsCAD `Editor.GetInteger(PromptIntegerOptions)`;
6. re-resolves both stable Handles immediately before selection;
7. changes implied selection only when **both** pair members still resolve live.

The two-live-object check is deliberate. A stale pair must not partially replace PICKFIRST with one surviving object.

This is Locate/Select, not exact-solid interference. Zoom, transient highlighting, modeless clash palette, issue persistence and Solid3d narrow-phase verification remain later scopes.

## Regression coverage

`MepRecognitionSmoke` covers:

- specific-over-generic default priority and case-insensitive matching;
- BlockName recognition;
- explicit custom priority;
- equal-priority conflicting matches -> `Ambiguous`;
- unmatched content -> no guessed discipline/category.

The smoke is registered directly in `SmokeTestRegistration`.

## LOCAL_ONLY handoff

Core recognition is host-neutral. V25 Locate still needs licensed runtime evidence on the exact integrated SHA:

1. representative sanitized layer and BlockName matches for each supported MEP/building class;
2. deliberately unknown and deliberately ambiguous names fail closed;
3. `QS3DMEPCLASH` and `QS3DMEPCLASHLOCATE` return the same pair order for an unchanged selection and clearance;
4. selecting pair N produces exactly two PICKFIRST objects;
5. stale/deleted pair member before final selection leaves the previous selection unchanged instead of partially selecting one object;
6. cancellation at selection, clearance and pair-number prompts causes no project/sidecar/CAD/audit mutation;
7. two-DWG runs do not leak Handle/ObjectId state;
8. existing mm/m unit controls continue to match native quantities.

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until licensed BricsCAD V25 evidence is tied to the exact integrated SHA.
