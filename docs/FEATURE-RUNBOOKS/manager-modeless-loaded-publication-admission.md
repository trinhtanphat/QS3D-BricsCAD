# Material Catalog / Project Tools loaded-publication admission — LOCAL_ONLY V25 qualification

This runbook qualifies issue #4849 after it reaches an exact source-ready SHA. Hosted source checks and compile validation prove ordering/compile safety only; they are never licensed BricsCAD runtime evidence.

## Source contract

- `QS3DMATERIALS` and `QS3DPROJECTTOOLS` retain one authoritative manager publication each.
- Existing exact native-database plus exact managed-wrapper reuse/activation remains unchanged.
- Wrapper drift or cross-DWG replacement must terminal-close the previous loaded owner; close exception/veto fails closed.
- The replacement candidate installs its exact-instance `Closed` release callback before host show.
- `Application.ShowModelessWindow(...)` must return with `window.IsLoaded == true` before `_published` is assigned.
- A non-loaded host return is treated as failed publication: no authoritative owner is recorded, the local candidate remains eligible for catch cleanup, and success status is not emitted.
- Material Catalog still requires an existing project and binds the exact admitted project into `MaterialCatalogWindow`.
- Project Tools still binds the exact source `Document` and preserves its existing document-bound command-hub semantics.

## Hosted/source validation

Run:

```text
python scripts/preflight-manager-modeless-loaded-publication-admission.py
python scripts/preflight-material-projecttools-manager-single-instance-veto-safe.py
```

Then require repository Shared CI. Green hosted CI is not licensed runtime evidence.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Execute only on one exact integrated SHA/product identity in a compatible licensed BricsCAD V25 Windows x64 host under the canonical local authorization flow.

1. **Material normal show** — with drawing A and an existing QS3D project active, run `QS3DMATERIALS`; require exactly one loaded Material Catalog and correct project/document affinity.
2. **Project Tools normal show** — run `QS3DPROJECTTOOLS`; require exactly one loaded Project Tools surface bound to drawing A.
3. **Repeated invocation** — repeatedly invoke each command for the same exact managed wrapper/native database; require reuse/activation and no duplicate loaded manager.
4. **Cross-DWG replacement** — open drawing B and invoke each manager; require drawing-A owner to reach terminal Closed before exactly one drawing-B replacement publishes.
5. **Managed-wrapper drift** — exercise only if an evidence-backed host path produces a new managed wrapper for the same live native database; old wrapper-bound owner must not be reused.
6. **Close veto/exception** — if the host/window can refuse or interrupt close, verify replacement fails closed and does not create a second loaded manager.
7. **Host return without Loaded** — execute only through an evidence-backed local harness/host condition that makes modeless show return without a loaded WPF window and without fabricating native behavior. Require no authoritative publication, no success status, and candidate cleanup. If the host cannot reproduce this state safely, record `NO_RESULT`; do not simulate a runtime PASS from source inspection.
8. **Document teardown** — close the bound drawing while each manager is open; require document-bound lifetime cleanup with no stale usable callbacks or retained authoritative owner.
9. **Material project admission regression** — invoke Material Catalog without an existing QS3D project; require the existing-project failure behavior and no manager publication.
10. **Cleanup** — close all test windows/drawings and verify no owned process/private-state residue beyond normal V25 qualification allowances.

## Evidence and verdict

Record exact SHA, plugin ProductVersion/hash, BricsCAD V25 version, sanitized drawing identities and one result per matrix row. Use only `PASS`, `FAIL`, or `NO_RESULT` for actually observed licensed-host rows. Never promote source preflight, compile success, mocks or hosted CI to `LOCAL_PASS`.
