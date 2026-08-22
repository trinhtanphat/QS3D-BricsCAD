# Interchange import policy selector

`QS3DINTERCHANGEIMPORT` is the user-facing policy router for the current `QS3D.SemanticSnapshot` import slices.

It does not implement a new merge engine. It delegates to bounded importers so each mutation path keeps its own validation/rollback contract.

## Routing

1. The selected file is read with the same guarded size limit and strict UTF-8 boundary used by the specialist import commands.
2. `ProjectInterchangeImportPreview.Plan(...)` validates the snapshot and classifies semantic identity collisions.
3. If `CollisionCount == 0`, the command offers **Append-only**.
4. If collisions exist, the selector independently asks the guarded Element, Catalog and ALL UseSource planners whether their scopes are executable.
5. If neither partial UseSource scope is executable, only **KeepTarget** is offered.
6. If exactly one partial UseSource scope is executable, the prompt is **UseSource / KeepTarget / Cancel**.
7. If both Element and Catalog scopes are executable, the first prompt chooses **UseSource / KeepTarget / Cancel**.
8. When atomic ALL is executable, the next prompt chooses:
   - **Replace ALL semantic (atomic)** — Zone/Floor/Family/Element executable collisions use source semantic state in one `ProjectStateSnapshot` + one native CAD transaction.
   - **Partial scope** — continue to a final explicit Element-vs-Catalog choice.
   - **Cancel** — no mutation.
9. Partial scope then chooses exactly one:
   - **Replace Element semantic** — same-category Element collisions use source portable semantic data; Zone/Floor/Family collisions stay target-authoritative.
   - **Replace Catalog semantic** — Zone/Floor/Family collisions use source semantic definitions; Element collisions stay target-authoritative.
   - **Cancel** — no mutation.

If ALL is blocked while both partial plans are individually executable, the selector shows the ALL block reason and only continues to the partial choice after explicit confirmation.

The selector never implements combined replacement by sequentially invoking the Element and Catalog importers. `QS3DINTERCHANGEUSESOURCEALL` owns its own union affected-closure calculation, one semantic rollback snapshot and one native transaction specifically to avoid a split-transaction partial-commit window.

Every selected importer re-plans before mutation. Dialog state is not trusted as stale authorization if project state changed while the user was choosing.

## Ownership boundary

All currently executable semantic import policies discard incoming source CAD handles as target ownership. A snapshot cannot claim native entities in the active target DWG.

The guarded Element replacement path preserves target `SourceHandles` and target drawing fingerprint, invalidates target-owned generated outputs for the affected closure inside a native CAD transaction, and requires explicit rebuild afterward.

The guarded Catalog replacement path invalidates existing target elements referencing replaced Zone/Floor/Family identities, expands through semantic dependents and linked opening hosts, performs catalog replacement inside the same rollback-capable native transaction, and also requires explicit rebuild afterward.

The guarded ALL path unions both concerns before mutation: replacement Elements, all existing Elements referencing replaced catalogs, their transitive dependents, old opening hosts and accepted incoming opening hosts are prepared for ownership-safe generated-output invalidation before any Catalog or Element semantic replacement occurs. Existing target source CAD handles and target element drawing fingerprints remain target-authoritative.

Append-only and KeepTarget remain semantic-only import paths. None of the import policies silently trigger generated 3D/rebar/curtain/opening/grid rebuild or physical cut operations.

## Provenance is a separate authorization

`QS3DINTERCHANGEPROVENANCE` can store imported drawing-local source handles as project metadata provenance only. It is intentionally separate from semantic import authorization:

- it does not write `ProjectElement.SourceHandles`;
- it does not create generated/native owner slots;
- it does not mutate imported semantic identities;
- its metadata is not re-exported by `QS3D.SemanticSnapshot` v1 as active drawing ownership.

The generic selector does not silently enable provenance retention.

## Specialist commands remain available

- `QS3DINTERCHANGEAPPEND` — deterministic append-only qualification/debug path.
- `QS3DINTERCHANGEUSESOURCEALL` — atomic all-scope Zone/Floor/Family/Element replacement path.
- `QS3DINTERCHANGEUSESOURCE` — guarded Element-only replacement qualification/debug path.
- `QS3DINTERCHANGEUSESOURCECATALOG` — guarded Zone/Floor/Family-only replacement qualification/debug path.
- `QS3DINTERCHANGEPROVENANCE` — provenance-only source-handle retention.
- `QS3DINTERCHANGEIMPORT` — normal explicit policy-selection entry point.

Keeping specialist commands separate makes runtime qualification and fault isolation deterministic.

## Not yet implied

The selector does not mean every conflict policy is implemented. Rename/remap, per-property merge precedence, source-handle rebinding, automatic physical rebuild/cut, provenance+semantic combined authorization, IFC/Revit/BCF/vendor/cloud interchange, and exact BricsCAD V25 runtime qualification remain separate work.
