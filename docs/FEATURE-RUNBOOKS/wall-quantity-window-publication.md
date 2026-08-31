# Wall Quantity modeless publication lifecycle

Issue: #4804  
Lane-Key: `issue-4804`

## Source contract

`QS3DWALLQTY` owns at most one published `WallQuantityWindow` process-wide. The publication records both the exact managed `Document` wrapper and its non-zero native database identity.

For an invocation against the same native database and the exact same managed wrapper, the loaded published window is activated and reused. For wrapper drift or a different drawing, the existing document-bound window must reach terminal unloaded/Closed state before a replacement can be shown. A close exception or veto fails closed and no duplicate is published.

A candidate is not published until `Application.ShowModelessWindow` returns and `IsLoaded` is true. Only the exact matching `Closed` callback may release the publication. A stale unloaded publication may be cleared defensively before creating a new candidate.

The lifecycle change must not alter Wall Quantity's read-only project lookup, detached snapshot regeneration, filtering, 3D locate behavior, or XLSX export semantics.

## Hosted validation

Run the auto-discovered source guard:

```text
python scripts/preflight-wall-quantity-window-publication.py
```

Then run the repository-required shared CI for the exact candidate, including `preflight` and `core`.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Run only on a real licensed BricsCAD V25 x64 host using the exact candidate SHA. Do not promote source/build evidence to runtime PASS.

1. Open drawing A with an existing QS3D project and invoke `QS3DWALLQTY`; verify one Wall Quantity window loads and shows rows.
2. Invoke `QS3DWALLQTY` again without changing drawings; verify the same window is activated and no second window appears.
3. Change filter/selection in the existing window, invoke the command again, and verify reuse rather than replacement.
4. Open/switch to drawing B and invoke `QS3DWALLQTY`; verify drawing A's window closes terminally before a B-bound window is published.
5. Switch back to A and repeat; verify no stale B-bound locate/export context remains active.
6. Close the active Wall Quantity window manually, invoke again, and verify a fresh window can publish.
7. During each drawing-close lifecycle, verify `DocumentBoundWindowLifetime` closes the document-bound surface and later commands do not retain a stale publication.
8. Exercise Refresh, Locate 3D and XLSX export after each valid publication to confirm read-only quantity behavior is unchanged.

Record exact SHA, BricsCAD build, DWG identity, PASS/FAIL per row and sanitized failure evidence. Until that matrix is actually executed, runtime status remains `PENDING_LOCAL` / `LOCAL_ONLY`.
