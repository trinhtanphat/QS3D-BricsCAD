# Work claim — Direct Draw Create Similar

- Status: `BLOCKED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T19:31:00+07:00`
- Updated: `2026-08-11T21:24:00+07:00`
- Baseline main SHA: `0296f6f31e28a598474875805b934edc26c98e60`
- Priority: reduce owner-reported authoring interactions by reusing an existing QS3D element's Family/Type for the next Direct Draw operation

## Reserved scope

Implement a source-safe **Create Similar / Vẽ Tương Tự** Direct Draw lane: select one existing QS3D semantic source or generated object, resolve its canonical semantic owner and Family/Type without creating a second model, activate that existing Family through the repository's current project/family mutation contract, then delegate to the existing `QS3DDRAWACTIVE` or `QS3DDRAWACTIVEADV` workflow. Add static contract coverage, one idempotent Quick Workflow Ribbon entry for the primary command, and a focused handoff documenting exact LOCAL_ONLY BricsCAD V25 qualification.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CreateSimilarCommands.cs`
- `src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs` only for one shared read-only support predicate
- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs` for the stable/idempotent **Vẽ Tương Tự** primary action, grouped-Ribbon compatibility and stable-button hot-reload reconciliation
- existing semantic ownership / Family activation services only as canonical dependencies; no parallel ownership model
- `scripts/preflight-create-similar.py`
- `docs/DIRECT-DRAW-CREATE-SIMILAR-2026-08-11.md`
- `docs/LOCAL-AGENT-INBOX.md` — existing `LOCAL-008` still needs the command-specific runtime delta
- this claim file for close-out status

## Excluded scope

- No DrawJig/transient preview implementation or runtime PASS claim.
- No continuous/repeated authoring implementation owned by `LOCAL-008`.
- No geometry-builder rewrite, second semantic model, or broad Workspace redesign.
- No broad `RibbonBootstrapper.cs` information-architecture rewrite; the completed Ribbon reconciliation lane is authoritative there.
- No GitHub Actions dispatch, release operation, signing, installer, or live BricsCAD V25 qualification.

## Source implementation already pushed

- `9ddf194482632d0200ff80bfe2aade341a87521f` — shared `ActiveFamilyQuickDrawCommands.SupportsFamily(...)` guard, reused by the existing dispatcher.
- `32916a602d2436435cd024ebae2896e4e671a7c0` — `QS3DCREATESIMILAR` / `QS3DCREATESIMILARADV` with selection-first non-creating preview, immutable project/owner/Family snapshot, canonical existing-project rebind, source/generated owner re-resolution, exact Family activation and synchronous Active-Family delegation.
- `80be0a78f41c5785c32ab996219850986b9e1994` + `8a6fbd195028250482b24ccde8cc35ef6b93f33c` — source/runtime contract documentation including the exact LOCAL-008 matrix and Ribbon qualification delta.
- `466f19f7291fdd7a71258fba165dbb0725e8bac8`, `214c276d0d0d3c743e5efdd09f69274c6971eb7d`, `c71d9a2589007bbbd02be5fb6c6066d63841f6ca` — auto-discovered static gate, route-support parity check and initial Ribbon contract guard.
- `ec9868a2139776b628479cfe2c042d4fd3e838db` — stable/idempotent `Vẽ Tương Tự` Quick Workflow Ribbon action mapped to `QS3DCREATESIMILAR`.
- `43308f46f901550237a69708ed274f3c59375b37` — `fix(ribbon): isolate quick workflow panel`; Quick Workflow now creates/reuses exact `QS3D_AUTHOR_QUICK_PANEL_SOURCE` / `Tác vụ nhanh` instead of falling back to an unrelated grouped authoring panel.
- `bc37e7ccf411ab97e9963ae1f125317417d2b6ff` — focused guard for the dedicated Quick Workflow panel and no-first-panel-fallback rule.
- `2f61ef490bcd357560cae52c007a568851cdda77` — documentation for deterministic grouped-Ribbon placement.
- `87cf340b8cbf4fc560a51fc40c5b0cbade28f5ca` — `fix(ribbon): reconcile quick workflow button state`; every stable Quick Workflow button is find-or-create by ID and then has Name/Text/visibility/CommandParameter/CommandHandler reconciled on reinitialization instead of being skipped merely because the ID already exists.
- `e39bc5deb3e3137961561acca3145f58bf5f19d9` — strengthened Create Similar/Ribbon preflight requiring find-or-create followed by stable-button state reconciliation and rejecting the former `CollectionContainsId(...); continue` path.
- `e157ef48c5212cdcbc3fb80975ce1c9a9aa70167` — updated Create Similar documentation and LOCAL_ONLY matrix for stale stable-button repair during Ribbon reinitialize.

The source lane deliberately does not duplicate Direct Draw category dispatch, generated-handle parsing, geometry builders, project bootstrap, semantic capture or regeneration logic.

## Remote/source status

Create Similar command behavior, generated/source ownership resolution, supported-Family routing, deterministic dedicated Quick Workflow panel placement and stable-button reinitialization are now **REMOTE_DONE** at source/contract level.

Ancestry verification against later `main` showed `87cf340b...`, `e39bc5de...` and `e157ef48...` each with `behind_by: 0` and itself as merge base. Concurrent Direct Draw, Quantity/BQ, updater and reporting work remained intact.

The focused Python/static gates are committed but were **not executed in this connector-only lane**. No GitHub Actions, local checkout/build, BricsCAD V25 launch, installer, signing or release was dispatched.

## Only remaining blocker

The **only remaining claim close-out blocker** is the canonical `docs/LOCAL-AGENT-INBOX.md` update under existing `LOCAL-008`.

The inbox is large and actively changing, while the available GitHub content update replaces the entire file rather than applying a bounded line patch. Replacing that canonical queue from partial/truncated remote reads risks deleting concurrent local evidence and violates the repository no-overwrite rule. The exact Create Similar V25 qualification delta is already recorded in `docs/DIRECT-DRAW-CREATE-SIMILAR-2026-08-11.md`, but that supporting document is not a second queue and cannot substitute for the canonical inbox edit.

A patch-capable writer or local agent must update only `LOCAL-008`, preserving current evidence, to add: sample-picker cancel/no-bootstrap behavior; source/generated owner resolution; fail-closed ambiguity/missing-Family/category/unsupported cases; project/owner/ownership/DWG drift; Quick/Advanced cancellation/no-residue behavior with intentional Active Family selection allowed to remain; exact `Vẽ Tương Tự` Ribbon idempotence/active-document routing, dedicated Quick Workflow panel and stable-button reinitialize repair; sanitized evidence only.

After that bounded canonical inbox edit, this claim can be marked `COMPLETED`. Exact V25 execution remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until real evidence exists.

## Coordination

Workspace multi-selection/property-origin, grouped Ribbon IA, RibbonBootstrapper reconciliation and Reference/Project augmenter reconciliation are completed. Other concurrent claims should not modify Create Similar command/route/QuickWorkflow surfaces while this blocked handoff remains reserved unless coordination is recorded first.

## Completion condition

`LOCAL-008` carries the exact Create Similar interactive qualification delta on current `main`, this claim is marked `COMPLETED`, and all live BricsCAD V25-only evidence remains explicitly unclaimed/LOCAL_ONLY.
