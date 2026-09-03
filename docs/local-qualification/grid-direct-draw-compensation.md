# Grid Direct Draw compensation / UI-isolation qualification

Carrier: #5319

This matrix is **LOCAL_ONLY**. Hosted source guards, Core tests and V25/V26 compilation do not prove licensed BricsCAD runtime behavior and must never be reported as `LOCAL_PASS`.

Use an exact built candidate containing #5319 on licensed BricsCAD V25 and, where the local lane supports it, V26. Record host version, exact Git SHA, plugin DLL SHA-256, DWG fixture identity and sanitized evidence.

## Matrix

- **GD01 — happy path:** activate a straight Grid Family, run `QS3DGRIDDRAW`, create two non-degenerate LINE axes, finish with Enter. Confirm two live native LINE sources and exactly one semantic Grid per source handle.
- **GD02 — start cancel:** cancel/Enter at the next start prompt. Confirm no additional native or semantic source is created.
- **GD03 — end cancel:** pick a start point then cancel the end point. Confirm no native/semantic residue for the incomplete segment.
- **GD04 — semantic capture rejection:** induce a supported capture-preflight rejection after source creation. Confirm the exact just-created LINE is compensated and no semantic residue remains.
- **GD05 — compensation identity drift:** with a controlled diagnostic fixture/reactor, cause the created source identity/type/owner to differ before compensation. Confirm destructive erase is refused unless exact ObjectId + handle + LINE type + owner are all proven; no unrelated entity is erased.
- **GD06 — owner-space proof:** successful authoring remains ModelSpace-only and compensated source owner equals the owner recorded at creation.
- **GD07 — active-DWG drift:** change active document during a prompt using the supported local harness. Confirm freshness failure before a new source commit and no cross-DWG mutation.
- **GD08 — UCS/project/family drift:** alter each captured authoring context independently before commit. Confirm fail-closed behavior and no incomplete source/semantic pair.
- **GD09 — UI failure isolation:** force selection/regen/palette/status failure after a successful semantic commit. Confirm source + semantic Grid remain committed and user output contains only the stable UI-sync warning, not raw exception/path/stack detail.
- **GD10 — operation error redaction:** trigger a controlled host/capture failure. Confirm command/palette output uses the stable `QS3DGRIDDRAW` failure message and does not expose raw exception text, file paths or stack traces.
- **GD11 — Undo/Redo:** after two successful axes, exercise host Undo/Redo according to the product-supported sequence. Record native/semantic convergence; do not infer behavior if host semantics are ambiguous.
- **GD12 — save/reopen + multi-DWG isolation:** save, close, reopen and verify source-handle semantic identity. Repeat in a second DWG and confirm no source/family/context leakage between documents.

## Acceptance

Pass requires all applicable GD01–GD12 observations on the exact candidate with no unrelated entity erased during failed-capture compensation, no raw host exception detail surfaced by the hardened paths, and no cross-DWG/context leakage. Any unavailable or unexecuted cell remains `NO_RESULT`; never convert hosted/static evidence into runtime PASS.
