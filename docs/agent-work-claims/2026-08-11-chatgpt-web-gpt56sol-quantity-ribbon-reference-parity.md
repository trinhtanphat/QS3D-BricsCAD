# Work claim — ĐỊNH LƯỢNG Ribbon reference parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-ribbon-reference-parity`
- Registered: `2026-08-11T21:26:00+07:00`
- Completed: `2026-08-11T21:37:00+07:00`
- Baseline main SHA: `95649da0c5d423105cf66eaa4ab3282f5e22e685`
- Priority: P1 screenshot/reference workflow parity

## Implemented

- `311b20a7db5dcfcaf206cfc99f8c24bf80ddae3a` — added isolated `QuantityReferenceRibbonAugmenter` for the existing `QS3D_QTY` / `ĐỊNH LƯỢNG` tab. It finds-or-creates one stable panel `QS3D_QTY_REFERENCE_PANEL_SOURCE`, reconciles button state by stable IDs, and never clears/removes existing Ribbon panels/items.
- The new reference-oriented `Tính khối lượng` panel exposes eight real existing workflows:
  - `Cài đặt tính toán` -> `QS3DQUANTITYSETTINGS`
  - `Tính khối lượng` -> `QS3DREGEN`
  - `Xuất ED2` -> `QS3DED2`
  - `Xem khối lượng` -> `QS3DBQ`
  - `Diễn giải` -> `QS3DBQ` (current full summary/detail BQ workflow)
  - `Khối lượng tường` -> `QS3DWALLQTY`
  - `Excel → CAD` -> `QS3DEXCELLOCATE`
  - `Đối chiếu Cũ/Mới` -> `QS3DREVDIFF`
- `b191cb39bc334aa9351ee4f07afd9c90d97a8f16` — wired augmenter initialize/reset into `PluginEntry` after the canonical Ribbon bootstrap. Existing Project, Reference Wall and Quick Workflow augmenters remain present.
- `669264e7f1acb6c8b04d5108926cd75789e57ad5` — added `scripts/preflight-ribbon-quantity-reference-parity.py`, checking stable IDs/labels/commands, find-or-create reconciliation, plugin lifecycle hooks, source-wide command registration, preservation of canonical `QS3D_QTY` bootstrap panels and absence of clear/remove behavior.
- `RibbonBootstrapper.cs` was deliberately left unchanged in this lane so concurrent bootstrap/reconciliation winners remain intact.

## Source validation

- Re-fetched current `main` after implementation. `QuantityReferenceRibbonAugmenter.cs` still contains all eight stable button specs, fails closed if `QS3D_QTY` is absent, reconciles current text/command/handler, and dispatches through the active BricsCAD document.
- Re-fetched `PluginEntry.cs`; bootstrap remains first and `QuantityReferenceRibbonAugmenter.TryInitialize()` / `.Reset()` are present without removing other augmenters.
- Re-fetched canonical `RibbonBootstrapper.cs`; the existing Quantity, Excel ↔ CAD, Cửa & lỗ mở, BBS, Cốt thép 3D and Health cốt thép panels remain present.
- Confirmed dedicated command registrations for `QS3DQUANTITYSETTINGS`, `QS3DWALLQTY` and `QS3DREVDIFF`; the other referenced commands are existing canonical QS3D adapter commands already used by the base Ribbon/BQ workflows.
- `311b20a7db5dcfcaf206cfc99f8c24bf80ddae3a` is an ancestor of current `main`; subsequent concurrent updater/quantity/formula/direct-draw work was preserved. No force push was used.
- GitHub exposes no combined status checks for the focused preflight commit, and no GitHub Actions were dispatched.
- A local container checkout could not be used because that runtime had no DNS route to GitHub; validation in this remote lane is therefore connector-based source/preflight review only, not a fabricated local execution result.

## LOCAL_ONLY disposition

- Physical BricsCAD V25 Ribbon rendering/click-through remains under the existing local Ribbon/runtime qualification boundary. No duplicate local inbox item was created.
- No remote native runtime PASS is claimed.

## Completion evidence

The screenshot-oriented quantity workflows are now directly discoverable from the `ĐỊNH LƯỢNG` Ribbon through a reconciliation-safe additive panel while preserving the repo's existing Ribbon information architecture and real command implementations.
