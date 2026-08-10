# Interchange import policy selector

`QS3DINTERCHANGEIMPORT` is the user-facing policy router for the current `QS3D.SemanticSnapshot` import slices.

It does not implement a new merge engine. It delegates to already bounded importers so each mutation path keeps its own validation/rollback contract.

## Routing

1. The selected file is read with the same guarded size limit and strict UTF-8 boundary used by the specialist import commands.
2. `ProjectInterchangeImportPreview.Plan(...)` validates the snapshot and classifies semantic identity collisions.
3. If `CollisionCount == 0`, the command offers **Append-only**.
4. If collisions exist and executable same-category Element replacement is unavailable, the command offers **KeepTarget** only.
5. If executable Element replacement is available, the explicit prompt is:
   - **Yes** — Replace Element semantic (`UseSourceSemanticData` for Element collisions; Zone/Floor/Family collisions stay target-authoritative).
   - **No** — KeepTarget for every collision and append only new identities.
   - **Cancel** — no mutation.

The underlying importer re-plans before mutation. The dialog is not trusted as stale authorization if project state changed while the user was choosing.

## Ownership boundary

All currently executable policies discard incoming source CAD handles as ownership. A snapshot cannot claim native entities in the active target DWG.

The guarded Element replacement path preserves target `SourceHandles` and target drawing fingerprint, invalidates target-owned generated outputs for the affected closure inside a native CAD transaction, and requires explicit rebuild afterward.

Append-only and KeepTarget remain semantic-only import paths. They do not silently trigger generated 3D/rebar/curtain/opening rebuild or cut operations.

## Specialist commands remain available

- `QS3DINTERCHANGEAPPEND` — deterministic append-only qualification/debug path.
- `QS3DINTERCHANGEUSESOURCE` — guarded Element replacement qualification/debug path.
- `QS3DINTERCHANGEIMPORT` — normal explicit policy-selection entry point.

Keeping the specialist commands separate makes runtime qualification and fault isolation deterministic.

## Not yet implied

The selector does not mean all future conflict policies are implemented. Rename/remap, catalog-definition UseSource, provenance-only incoming handles, source-handle rebinding, automatic physical rebuild/cut, IFC/Revit/BCF/vendor/cloud interchange, and exact BricsCAD V25 runtime qualification remain separate work.
