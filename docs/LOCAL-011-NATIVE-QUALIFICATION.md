# LOCAL-011 native rollback/modeless qualification

This is the executable LOCAL_ONLY handoff for `LOCAL-011`. It finishes the repository-safe preparation so a compatible local agent should **pull/sync the intended SHA and run one command**, not redo source engineering. The runner does not add a production fault switch, does not weaken BricsCAD security, and does not claim `LOCAL_PASS` by itself.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-local-v25-local-011.ps1
```

Pass `-BricsCadDir <V25 install directory>` only when V25 is not found from `BRICSCAD_V25_DIR` or the standard install locations. Use a clean checkout, licensed interactive BricsCAD V25 x64, disposable/sanitized DWGs only, and zero pre-existing BricsCAD processes. Do not patch source to make a matrix row pass; return an ordinary source-safe defect to its source lane.

The runner first verifies the exact Git SHA, clean tree, the four source-ready ancestors recorded in `docs/LOCAL-AGENT-INBOX.md`, and the canonical `run-local-v25-qualification.ps1` **licensed runtime** baseline. It then NETLOADs the exact Release V25 plugin into one dedicated BricsCAD session and records the following 21 rows. For every row record the smallest sanitized proof described by `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`, `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`, and the `LOCAL-011` inbox acceptance text.

| Case ID | Required proof |
|---|---|
| `native.before_commit_abort` | Pre-commit failure aborts native work and restores exact semantic/native state. |
| `native.during_commit_abort` | Mid-transaction failure leaves no partial native/semantic replacement. |
| `native.after_commit_ui_failure` | Post-commit UI failure leaves committed model coherent and isolated. |
| `native.document_lock_multi_dwg` | DocumentLock/focus drift fails closed; other DWG unchanged. |
| `recognition.stale_apply_no_project` | Stale Recognition Apply refuses without creating/caching a replacement project. |
| `modeless.door_detached` | Door read-only regeneration/export uses detached state; live project unchanged. |
| `modeless.room_detached` | Room read-only regeneration/export uses detached state; live project unchanged. |
| `modeless.bbs_detached` | BBS read-only regeneration/export uses detached state; live project unchanged. |
| `modeless.bq_canonical_write` | BQ preference write rebinds canonical same-ProjectId state or refuses. |
| `modeless.rebar_mesh_stale_save` | Rebar Mesh stale Save refuses before semantic/native mutation or replacement project creation. |
| `palette.unavailable_project_teardown_rebind` | Unloadable-project activation disables stale callbacks and later valid activation rebinds cleanly. |
| `generated.grid_stale_handle` | Grid one-missing expected handle refuses before erase/create and preserves survivors/metadata. |
| `generated.curtain_line_stale_handle` | Curtain LINE one-missing expected handle refuses atomically. |
| `generated.curtain_path_stale_handle` | Curtain PATH one-missing expected handle refuses atomically. |
| `generated.rebar_stale_handle` | Representative longitudinal/stirrup/tie/shape/mesh slots refuse stale expected sets. |
| `generated.rebar_malformed_metadata` | Malformed Rebar handle metadata refuses with no mutation. |
| `generated.rebar_duplicate_canonical` | Duplicate-canonical Rebar metadata refuses with no mutation. |
| `generated.full_live_exact_replacement` | Complete live sets replace exactly once with complete new ownership metadata. |
| `generated.foreign_object_protection` | Foreign/unmarked objects are never erased or re-owned. |
| `generated.undo_save_reopen` | Undo is coherent and final ownership persists through save/cold reopen. |
| `isolation.other_dwg_untouched` | A second open DWG remains semantically/native unchanged throughout. |

For each row the console accepts only exact `PASS <case-id>`, `FAIL <case-id>`, or `BLOCKED <case-id>` plus a non-trivial sanitized evidence note. The final machine-readable report is `artifacts/local-v25-local-011/qualification.json` and carries the exact tested SHA, V25/Windows identity, baseline report, all case outcomes, and `localPassClaimedByRunner=false`.

Final result is `PASS` only when all 21 rows pass. Any product failure returns `FAIL`/exit 1; an environment/runtime blocker returns `BLOCKED`/exit 2; missing/incomplete evidence returns `NO_RESULT`/exit 3. A local agent may update `docs/LOCAL-AGENT-INBOX.md` to `PASS` only from sanitized evidence tied to that exact tested SHA.