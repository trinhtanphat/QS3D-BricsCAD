# Work claim — V25 Quantity Insight dark selection

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-dark-selection-20260813`
- Registered: `2026-08-13T16:59:00+07:00`
- Released: `2026-08-13T17:01:00+07:00`
- Baseline main SHA: `3b3ad9e76789f9ab440c02fb4067cee7d5df333e`
- Priority: Follow-up to the screenshot-visible bright selection defect.

## Reserved scope

Originally intended to make `QuantityInsightPanel.QuantityTree` selection chrome host-independent by shadowing active and inactive WPF selection background/text resources at the panel and tree resource boundaries.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DarkHostTheme.cs`
- `scripts/preflight-quantity-insight-dark-selection.py`

## Excluded scope

- responsive header layout
- Quantity Insight handlers/view models/business calculations
- WorkspacePanel, RightPanel, shared `Theme.xaml` redesign, V26

## Validation / release reason

After the claim-only commit, the mandatory source recheck found that this exact lane had already been implemented and closed earlier on `main`:

- prior claim: `358c602d1c9fb00997588526d62e8b019817e030`;
- implementation: `c583a484399fa0c98f34bf6528950bd38eeecfaa` (`fix(v25): pin Quantity Insight dark selection chrome`);
- regression: `957746ce4250f1433aa904d3ecd6635c18d01275`;
- prior completion: `82b86a535ff2cc2c20442453097fcfe26c7df07a`.

Current `main` already contains `QuantityInsightPanel.DarkHostTheme.cs` with all four active/inactive `SystemColors` selection resource pins at the panel and `QuantityTree` boundaries. The attempted source create was therefore rejected because the file already exists. No product source/test change from this duplicate lane was made.

## Coordination

This reservation is released immediately to avoid duplicate ownership. The existing completed Quantity Insight dark-selection implementation remains canonical.

## Completion condition

Not applicable: lane released as already completed by prior work. No substantive source or test changes were made under this claim.
