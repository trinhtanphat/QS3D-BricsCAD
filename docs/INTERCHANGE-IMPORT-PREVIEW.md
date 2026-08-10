# QS3D Semantic Snapshot — read-only import preview

Updated: 2026-08-10 (UTC+7)

`ProjectInterchangeImportPreview` is a **read-only collision/provenance preview** for a valid `QS3D.SemanticSnapshot` v1 file against an existing `ProjectState`.

It does not import, merge, restore, rebind CAD handles, reconstruct native ownership, write `.qsdb`, touch DWG entities or mutate the target project.

The purpose is to make future import policy explicit before any mutating importer is allowed to exist.

## Processing order

The preview always starts with the existing `ProjectInterchangeJsonValidator` contract.

1. validate the JSON structure, format/version, SI units, identities, references, dependency graph and generated/native-property exclusions;
2. if validation has an Error, stop before collision planning;
3. parse only the minimum identity manifest needed for preview: project ID/fingerprint, Zone/Floor IDs, Family ID/category and element ID/category;
4. inspect target IDs read-only;
5. classify identities and provenance relation;
6. return a bounded detail list plus complete counts.

A validator PASS is still **not import permission**. A preview with zero collisions is also **not import permission**.

## Identity classifications

Each source identity is classified as one of:

- `New` — no target identity currently uses the same semantic ID;
- `ExistingNeedsPolicy` — the target already uses the same ID and a future importer needs an explicit merge/replace/skip/rename policy;
- `ExistingIncompatible` — Family/element ID collides with a target object of a different `ElementCategory`; automatic merge must fail closed.

Zone and Floor collisions are always `ExistingNeedsPolicy` because name/elevation/rename/replace semantics have not been approved.

Family and element collisions with the same category are also `ExistingNeedsPolicy`; the preview deliberately does not compare properties and then silently decide which side wins.

Target duplicate IDs are rejected. A future importer must not operate on an already ambiguous target identity graph.

## Project and drawing provenance

The preview reports:

- source project ID;
- target project ID;
- whether the project IDs match case-insensitively;
- drawing fingerprint relation: `Match`, `Different` or `Unknown`.

These are review signals only.

A matching project ID or drawing fingerprint does not authorize overwrite. A different fingerprint does not automatically mean import is forbidden. The eventual product policy must decide what these signals mean for copy/merge/template/coordination workflows.

## Drawing-local CAD handles remain non-portable

Semantic Snapshot v1 declares `sourceRefScope = drawing-local`. The preview intentionally does not parse or rebind `sourceHandles` as target ownership.

Future import work must choose an explicit source-provenance policy, for example:

- discard source handles and import semantic-only detached data;
- require a separately proven same-DWG mapping step;
- map by an explicit external/reference identity contract.

It must never assume that a source DWG Handle identifies the same CAD entity in the target drawing.

## Generated/native ownership remains excluded

The existing validator rejects generated/native ownership fields such as generated solid/rebar/mesh/frame handle slots and `PhysicalOpeningCut*` state. The preview does not reconstruct any of them.

If a future semantic import creates or changes target semantic data, generated native output must start from a deliberately unowned/stale/rebuild-required state unless a separately reviewed ownership reconstruction protocol exists. Portable JSON must not become native CAD ownership authority.

## Bounds

Validation retains the v1 file/object/collection limits. Import-preview detail output is additionally bounded by:

```text
ProjectInterchangeImportPreview.MaxDetailedItems = 10000
```

Counts remain complete even when detailed identity rows are truncated.

## Source checks

```text
python scripts/preflight-interchange-import-preview.py
```

`ProjectInterchangeImportPreviewSmoke` covers:

- all-new identities without target mutation;
- same-category collisions requiring policy;
- incompatible Family/element category collisions;
- invalid snapshots stopping before collision planning;
- drawing fingerprint Match/Different/Unknown reporting;
- ambiguous target IDs failing closed.

The repository-wide smoke-registration guard must continue to discover this smoke.

## Still required before any mutating importer

Do **not** add `QS3DINTERCHANGEIMPORT` or mutate live project state until the following contracts are separately reviewed and guarded:

1. import mode: new project, semantic copy, merge or update;
2. per-kind ID collision policy for Zone/Floor/Family/element;
3. project ID and drawing-fingerprint policy;
4. property/quantity precedence and validation;
5. Floor elevation, Zone and Family catalog merge behavior;
6. dependency ordering and missing-reference behavior;
7. schema migration beyond exact v1;
8. drawing-local source provenance/rebinding policy;
9. generated ownership clearing/rebuild strategy;
10. target snapshot/rollback and file/native transaction boundaries;
11. preview/confirmation UX with no hidden overwrite;
12. audit events for accepted import decisions;
13. save/reopen, multi-DWG and exact licensed BricsCAD V25 qualification for the adapter/UI boundary.

Until those decisions exist, this feature is **REMOTE_DONE as read-only import planning only**. JSON round-trip/import remains intentionally incomplete.
