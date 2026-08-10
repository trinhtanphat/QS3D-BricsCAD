# Interchange import policy selector

`QS3DINTERCHANGEIMPORT` is the user-facing policy router for the current `QS3D.SemanticSnapshot` import slices.

It does not implement a new merge engine. It delegates to already bounded importers so each mutation path keeps its own validation/rollback contract.

## Routing

1. The selected file is read with the same guarded size limit and strict UTF-8 boundary used by the specialist import commands.
2. `ProjectInterchangeImportPreview.Plan(...)` validates the snapshot and classifies semantic identity collisions.
3. If `CollisionCount == 0`, the command offers **Append-only**.
4. If collisions exist, the selector independently asks the guarded Element and Catalog UseSource planners whether their scope is executable.
5. If neither UseSource scope is executable, only **KeepTarget** is offered.
6. If exactly one UseSource scope is executable, the prompt is **UseSource / KeepTarget / Cancel**.
7. If both scopes are executable, the first prompt chooses **UseSource / KeepTarget / Cancel** and a second explicit prompt chooses one UseSource scope:
   - **Replace Element semantic** — same-category Element collisions use source portable semantic data; Zone/Floor/Family collisions stay target-authoritative.
   - **Replace Catalog semantic** — Zone/Floor/Family collisions use source semantic definitions; Element collisions stay target-authoritative.
   - **Cancel** — no mutation.

The selector deliberately does **not** execute Element UseSource and Catalog UseSource sequentially. Those services each own a native CAD transaction, so sequencing them as one apparent operation would create a partial-commit window. A future combined policy must use one planner, one affected-closure calculation, one project snapshot and one native transaction.

Each underlying importer re-plans before mutation. The dialog is not trusted as stale authorization if project state changed while the user was choosing.

## Ownership boundary

All currently executable semantic import policies discard incoming source CAD handles as ownership. A snapshot cannot claim native entities in the active target DWG.

The guarded Element replacement path preserves target `SourceHandles` and target drawing fingerprint, invalidates target-owned generated outputs for the affected closure inside a native CAD transaction, and requires explicit rebuild afterward.

The guarded Catalog replacement path invalidates existing target elements referencing replaced Zone/Floor/Family identities, expands through semantic dependents and linked opening hosts, performs catalog replacement inside the same rollback-capable native transaction, and also requires explicit rebuild afterward.

Append-only and KeepTarget remain semantic-only import paths. They do not silently trigger generated 3D/rebar/curtain/opening rebuild or cut operations.

## Provenance is a separate authorization

`QS3DINTERCHANGEPROVENANCE` can store imported drawing-local source handles as project metadata provenance only. It is intentionally separate from semantic import authorization:

- it does not write `ProjectElement.SourceHandles`;
- it does not create generated/native owner slots;
- it does not mutate imported semantic identities;
- its metadata is not re-exported by `QS3D.SemanticSnapshot` v1 as active drawing ownership.

The generic selector does not silently enable provenance retention.

## Specialist commands remain available

- `QS3DINTERCHANGEAPPEND` — deterministic append-only qualification/debug path.
- `QS3DINTERCHANGEUSESOURCE` — guarded Element replacement qualification/debug path.
- `QS3DINTERCHANGEUSESOURCECATALOG` — guarded Zone/Floor/Family replacement qualification/debug path.
- `QS3DINTERCHANGEPROVENANCE` — provenance-only source-handle retention.
- `QS3DINTERCHANGEIMPORT` — normal explicit policy-selection entry point.

Keeping specialist commands separate makes runtime qualification and fault isolation deterministic.

## Not yet implied

The selector does not mean all conflict policies are implemented. A single-transaction combined Element+Catalog UseSource policy, rename/remap, per-property merge precedence, source-handle rebinding, automatic physical rebuild/cut, IFC/Revit/BCF/vendor/cloud interchange, and exact BricsCAD V25 runtime qualification remain separate work.
