# QS3D local qualification — Preview / Diagnostic workflow

Updated: 2026-08-10 (UTC+7).

Status: **LOCAL_ONLY runtime qualification**. Source implementation exists, but this document is the required exact-SHA Windows + licensed BricsCAD V25 handoff before these commands are called runtime-qualified.

Read together with `docs/LOCAL-V25-QUALIFICATION.md`, `docs/COMMANDS-PREVIEW-DIAGNOSTICS.md` and the exact current `main` source.

## 1. Before testing

Record:

```text
Exact SHA: <40-char SHA>
Windows build: <value>
BricsCAD V25 edition/build: <value>
Adapter DLL path: <value>
DWG fixture: synthetic/private-local only
```

Do not commit private DWGs, customer paths, proprietary BricsCAD DLLs or unsanitized screenshots.

Run the normal exact-SHA local build/NETLOAD flow first. These scenarios do not replace the broader qualification matrix.

## 2. `QS3DRULEPREVIEW` read-only proof

Prepare a project with at least two semantic elements and one QuantityRule whose current outputs are dirty/stale.

Before command execution record locally:

- `ProjectState.ChangeVersion`;
- `ProjectState.UpdatedUtc`;
- target element properties/quantities/rule provenance;
- native entity count/owned generated handles;
- DWG modified flag if available.

Run `QS3DRULEPREVIEW`.

PASS requires:

- command reports expected changed element/output counts;
- Added / Changed / Removed descriptions match the known fixture;
- live `ProjectState.ChangeVersion` is unchanged;
- live `UpdatedUtc` is unchanged;
- live properties/quantities/provenance are unchanged;
- no native entities are created/erased/modified;
- current CAD selection/view is not destructively changed;
- running the command twice on unchanged state produces equivalent semantic results.

Repeat after switching to a second open DWG. The command must use only the active document's project context.

## 3. `QS3DREGENPREVIEW` read-only proof

Prepare a dirty semantic element whose normal Core regeneration will change one or more quantities.

Record the same state markers as above, then run `QS3DREGENPREVIEW`.

PASS requires:

- reported changed elements/fields agree with a controlled fixture;
- new Health Error count agrees with the known fixture;
- live semantic state remains unchanged;
- live `ProjectState.ChangeVersion` and `UpdatedUtc` remain unchanged;
- generated/native CAD remains unchanged;
- no save/sidecar write occurs solely because preview was run;
- repeated unchanged preview is stable;
- active-document switching does not leak results across DWGs.

This command previews **Core semantic regeneration**, not native Solid3d creation. Do not claim native geometry preview from this result.

## 4. `QS3DDIAGSUMMARY` privacy proof

Use a deliberately synthetic fixture containing obvious unique markers in:

- Project ID/name;
- DWG path/fingerprint;
- Zone/Floor/Family/Element IDs and names;
- source/generated CAD handles;
- properties/quantities;
- a health issue message.

Export with `QS3DDIAGSUMMARY` to a local file.

PASS requires:

- file parses as JSON;
- `format` is `QS3D.DiagnosticSummary` and supported version is present;
- schema/count/category/health-code aggregates are present and correct;
- none of the synthetic secret markers appear anywhere in the exported bytes;
- no raw path/fingerprint/handle/semantic payload/message is present;
- overwrite of an existing target file produces a complete valid new file rather than a partial/truncated file;
- canceling the Save dialog creates no file and does not mutate the project.

If a real customer project is used locally, inspect the output before sharing it outside the customer environment even though the source contract is privacy-minimized.

## 5. Future guarded Apply UX qualification

Core source currently exposes guarded mutation APIs but the adapter intentionally does not expose an automatic production Apply command from these previews.

Before adding/exposing that UX, local V25 implementation must prove:

1. preview is shown before mutation;
2. Apply requires explicit user confirmation;
3. Cancel performs zero semantic/native mutation;
4. any `ProjectState.ChangeVersion` change after preview invalidates Apply, even when recomputed deltas happen to look equivalent;
5. stale preview gives a clear user-visible reason and asks for a new preview;
6. batch semantic failure restores the complete project snapshot;
7. a newly introduced Model Health Error blocks/rolls back guarded Apply;
8. BricsCAD Undo/session behavior is coherent with the semantic transaction;
9. save/close/reopen preserves only committed semantic state;
10. multi-DWG switching cannot apply a preview created for another project/document;
11. post-commit locate/highlight/palette failure does not undo an otherwise valid committed semantic transaction;
12. native geometry is not silently rebuilt unless the UX explicitly enters the established native regeneration/build transaction.

Do not expose one-click auto-apply merely because the Core API exists.

## 6. Performance qualification

Test representative sizes such as approximately 1k, 10k and the largest supported local semantic element count.

Measure locally:

- Rule Preview elapsed time and peak memory;
- Regen Preview elapsed time and peak memory;
- Health baseline/diff elapsed time;
- Diagnostic Summary generation/export time and output size;
- UI responsiveness while a modeless workspace remains open.

If preview becomes too expensive for large projects, optimize deterministic Core traversal first. Do not make the preview mutate live state as a performance shortcut.

## 7. Sanitized result template

```text
Exact SHA: <sha>
QS3DRULEPREVIEW read-only: PASS/FAIL
QS3DREGENPREVIEW read-only: PASS/FAIL
QS3DDIAGSUMMARY privacy: PASS/FAIL
Multi-DWG affinity: PASS/FAIL
Cancel zero-mutation: PASS/FAIL
ChangeVersion stale guard: PASS/FAIL/NOT EXPOSED
Guarded Apply confirmation: PASS/FAIL/NOT EXPOSED
Guarded Apply rollback: PASS/FAIL/NOT EXPOSED
Undo/session behavior: PASS/FAIL/NOT EXPOSED
Performance: PASS/FAIL/NOT RUN
Known blockers: <sanitized>
```

Source review or static preflight alone cannot change these runtime fields to PASS.
