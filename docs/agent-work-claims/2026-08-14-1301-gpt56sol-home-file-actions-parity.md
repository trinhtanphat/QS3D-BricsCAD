# Work claim — BLT reference Home file actions parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-home-file-actions`
- Registered: `2026-08-14T13:01:00+07:00`
- Baseline main SHA: `babe450f826d74922239dd15c44306d9a0af6067`
- Owner request: continue all, then review the whole project/session and complete remaining non-overlapping remote-safe gaps.
- Implementation SHA: `344f6466d985ba58336c41379289af03141a4cff`
- Focused guard SHA: `a308152009bc0feb74bfab41c514f09715c77462`
- Plan update SHA: `3d2aa78a8a486459c3a1c5aa3ae5ad1ca53c8ccf`

## Concrete gap completed

The BLT reference screenshot exposes top-level Home actions for Open, Save, Save As and Settings. QS3D already had equivalent behaviors in Start Center (`OPEN`, `QSAVE`, `SAVEAS`) and project configuration in `QS3DPROJECTTOOLS`, but the canonical Home Ribbon did not surface the complete file/settings cluster. The existing `Lưu` button is `QS3DSAVE` semantic-project persistence, so it was deliberately not repurposed as native DWG save.

## Implemented scope

- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs`
  - added one idempotent `Tệp` panel on existing `QS3D_HOME`;
  - `Mở…` → `_.OPEN`;
  - `Lưu bản vẽ` → `_.QSAVE`;
  - `Lưu thành…` → `_.SAVEAS`;
  - `Cài đặt` → `QS3DPROJECTTOOLS`;
  - existing `QS3DSAVE` semantic persistence remains untouched and separately visible.
- `scripts/preflight-blt-reference-ui-parity.py`
  - now guards all four Home mappings, existing-Home augmentation, separate `QS3DSAVE`, and the existing Project Tools settings implementation.
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
  - records the Home parity gap, implementation and exact local/native acceptance boundary.

## Review/validation boundary

Source/commit readback confirms the runtime augmenter uses the existing Home tab and creates only a bounded `Tệp` panel; it does not create a duplicate top-level tab or touch Ribbon startup scheduling. The focused guard was updated consistently with that contract. Container-side execution was unavailable because the sandbox cannot resolve GitHub, and no GitHub Actions run is claimed for these commits. Licensed BricsCAD V25 visibility/clickability, dark-theme/DPI and host command behavior remain local/runtime acceptance; no `LOCAL_PASS` is claimed.

No startup/lifecycle, RightPanel, Source Reconcile, Curtain, Level/rebar, release/signing or LOCAL_ONLY runtime surfaces were modified by this claim.
