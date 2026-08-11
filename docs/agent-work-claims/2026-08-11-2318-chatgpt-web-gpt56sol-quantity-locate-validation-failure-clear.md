# Agent Work Claim

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-20260811-quantity-locate-validation-failure-clear`
- Started (UTC): `2026-08-11T16:18:00Z`
- Last Updated (UTC): `2026-08-11T16:18:00Z`
- Expected Completion: `same session after source-safe implementation and repository-verifiable qualification`
- Task Key: `UI-QUANTITY-LOCATE-VALIDATION-FAILURE-CLEAR`
- Intended Work: Close the residual quantity-locate selection-staleness path where stale-row/project validation can fail before the explicit selection-replacement call, leaving a previous CAD implied selection highlighted.
- Scope: `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`; `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`; one focused auto-discovered preflight; this claim and planning documentation.
- Implementation Contract: A failed locate that is still bound to the same active DWG must best-effort clear implied selection before surfacing the stale/validation failure. Never clear a different active document. Preserve the existing explicit `CadHandleService.Select(...)` zero-live/zero-candidate behavior, document/project affinity checks, read-only project semantics, multi-object handling, statuses and positive-count-only zoom.
- Out of Scope: `CadHandleService` API semantics; reporting/grouping/math; project mutation/persistence; `Commands.cs`; viewport camera algorithms; cross-DWG selection mutation; native BricsCAD V25 runtime claims.
- Coordination: The prior `quantity-locate-stale-selection-clear` claim is `COMPLETE`. Recent Quantity commits concern revisions/rules and no newer Quantity-locate claim was found. This is a new residual lane rather than reopening completed work.
- Verification Plan: Register plan before source edit; verify exact active-document gating; require Summary catch/validation failures and Insight same-DWG project/row validation failures to clear best-effort before returning/reporting; preserve wrong-DWG fail-closed behavior; add focused source preflight; compare concurrent main changes before merge; re-fetch merged source and record GitHub check/workflow status honestly.
- Native V25 Disposition: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local interactive qualification queue.
