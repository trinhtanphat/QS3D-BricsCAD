# Agent work claim — Locate preflight before prompts

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: source-safe low-click lifecycle for generic `QS3DLOCATE` and `QS3DEXCELLOCATE` in `Commands.cs`.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/Commands.cs`
  - `scripts/preflight-locate-before-user-input.py`
  - this claim file for close-out
- Problem: both commands require an existing QS3D project, but generic Locate asks for an Element Id first and Excel Locate asks the user to choose a workbook / row first; only afterward do they reject a projectless DWG.
- Intended contract:
  - require/read the existing project before any Locate text/file/row interaction;
  - preserve cancel behavior and all existing modern/legacy Excel provenance validation;
  - no project creation or mutation;
  - do not overlap the active quantity-locate stale-selection claim, which explicitly excludes Excel locate and reserves different files.
- Non-overlap: excludes `CadHandleService`, QuantitySummaryWindow, QuantityInsightPanel, viewport/selection semantics, Ribbon, updater, Reference Wall and LOCAL_ONLY V25 execution.
- Validation: exact diff/current-source review plus focused static preflight; no GitHub Actions under `continue all`.
- Completion condition: projectless Locate/Excel Locate fail before user input/dialog while valid-project behavior remains unchanged.