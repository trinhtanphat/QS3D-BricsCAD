# Work claim — Direct Draw Create Similar

- Status: `BLOCKED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T19:31:00+07:00`
- Updated: `2026-08-11T20:45:00+07:00`
- Baseline main SHA: `0296f6f31e28a598474875805b934edc26c98e60`
- Priority: reduce owner-reported authoring interactions by reusing an existing QS3D element's Family/Type for the next Direct Draw operation

## Reserved scope

Implement a source-safe **Create Similar / Vẽ Tương Tự** Direct Draw lane: select one existing QS3D semantic source or generated object, resolve its canonical semantic owner and Family/Type without creating a second model, activate that existing Family through the repository's current project/family mutation contract, then delegate to the existing `QS3DDRAWACTIVE` or `QS3DDRAWACTIVEADV` workflow. Add static contract coverage, one idempotent Quick Workflow Ribbon entry for the primary command, and a focused handoff documenting exact LOCAL_ONLY BricsCAD V25 qualification.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CreateSimilarCommands.cs`
- `src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs` only for one shared read-only support predicate
- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs` for the stable/idempotent **Vẽ Tương Tự** primary action and grouped-Ribbon compatibility
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
- `43308f46f901550237a69708ed274f3c59375b37` — `fix(ribbon): isolate quick workflow panel`
  - grouped-Ribbon integration defect is now fixed;
  - Quick Workflow creates/reuses exact `QS3D_AUTHOR_QUICK_PANEL_SOURCE` / `Tác vụ nhanh` under `QS3D_AUTHOR` instead of searching the removed flat panel or falling back to the first grouped panel;
  - existing Quick Workflow button IDs and click-time active-DWG routing remain intact.
- `bc37e7ccf411ab97e9963ae1f125317417d2b6ff` — focused static guard for the dedicated Quick Workflow panel and no-first-panel-fallback rule.
- `2f61ef490bcd357560cae52c007a568851cdda77` — documentation updated for deterministic grouped-Ribbon placement and exact local qualification delta.

The source lane deliberately does not duplicate Direct Draw category dispatch, generated-handle parsing, geometry builders, project bootstrap, semantic capture or regeneration logic.

## Grouped Ribbon integration status

The previously confirmed flat-panel compatibility defect is **REMOTE_DONE**. Current `QuickWorkflowRibbonAugmenter` uses the dedicated `QS3D_AUTHOR_QUICK_PANEL_SOURCE` / `Tác vụ nhanh` panel and no longer contains the removed `QS3D_AUTHOR_PANEL_SOURCE` or `if (source == null) source = candidate;` fallback.

The later `RibbonBootstrapper` reconciliation work also preserves unknown/dedicated augmenter panels while reconciling grouped QS3D-owned panels, so hot reconciliation does not intentionally delete the Quick Workflow panel.

This source integration is therefore no longer a blocker for Create Similar.

## Remaining blocker

The **only remaining claim close-out blocker** is the canonical `docs/LOCAL-AGENT-INBOX.md` update under existing `LOCAL-008`.

In this connector session that inbox is large and actively changing, while the available GitHub content write replaces the entire file rather than applying a bounded line patch. Replacing the whole inbox from partial/truncated remote reads would risk deleting concurrent local evidence and violates the repository no-overwrite rule.

The exact Create Similar V25 qualification delta is already recorded in `docs/DIRECT-DRAW-CREATE-SIMILAR-2026-08-11.md`, but that supporting document is **not** a second queue and does not substitute for the canonical inbox edit. This claim therefore stays `BLOCKED` rather than being falsely marked complete.

## Required unblock / successor action

A patch-capable writer or local agent should update only `LOCAL-008` on the newest intended SHA, preserving all current evidence, to add:

- `QS3DCREATESIMILAR` / `QS3DCREATESIMILARADV` sample-picker cancel with no project bootstrap/cache/Family mutation;
- live semantic-source and generated-output owner resolution;
- ambiguity/non-semantic/missing-Family/category-mismatch/unsupported-Family refusal before Active Family changes;
- project reload/replacement, owner remap, source/generated ownership drift and active-DWG switch refusal;
- Quick/Advanced delegated cancel/no-residue behavior while intentional sampled Family activation may remain;
- exact one-button Ribbon idempotence/active-document routing and deterministic dedicated Quick Workflow panel placement;
- sanitized evidence only, with no private path/raw Handle list.

After that bounded inbox change is pushed, this same claim can be marked `COMPLETED`; exact V25 execution remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until real evidence exists.

## Coordination

The generated-native source-recognition lane explicitly excludes Create Similar command-side ownership. Workspace multi-selection/property-origin, grouped Ribbon IA, legacy augmenter compatibility and RibbonBootstrapper reconciliation lanes are completed. Other concurrent claims should not modify the Create Similar command/route/QuickWorkflowRibbonAugmenter surfaces while this blocker remains reserved unless the split is coordinated in both claims.

## Completion condition

`LOCAL-008` carries the exact Create Similar interactive qualification delta on current `main`, this claim is marked `COMPLETED`, and all live BricsCAD V25-only evidence remains explicitly unclaimed/LOCAL_ONLY.
