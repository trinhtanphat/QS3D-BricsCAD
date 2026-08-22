# Work claim — Semantic Selection raw relation canonicality fixture

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T16:48:00+07:00`
- Baseline main SHA: `a902bb5e54347e7ff17a0ba72594b1eb3e801efa`
- Priority: first independent full Core smoke blocker after completed Revision Capture relation fixture

## Confirmed fixture drift

`SemanticSelectionRelationCanonicalitySmoke` intends to prove that `SemanticSelectionInspector` rejects raw noncanonical Family/Floor/Zone relation ids while allowing canonical and blank optional references. Current public `ProjectElement` relation setters canonicalize padded values before the inspector runs, so the three padded rejection cases no longer construct the raw state they are meant to exercise.

Production remains fail-closed: `SemanticSelectionInspector.CanonicalOptionalReference(...)` compares each nonblank raw relation with its trimmed value and throws `InvalidOperationException` before materializing selection relations when they differ.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/SemanticSelectionRelationCanonicalitySmoke.cs`
- this claim document only

For each Family/Floor/Zone padded case, keep a supported public write and assert its canonical stored value first, then use test-local reflection to inject only the corresponding private raw backing field and assert the public getter sees the padded value. Retain `SemanticSelectionInspector.Inspect(...)` rejection, reset via the supported canonical setter, and preserve the whitespace-only blank-reference success case.

## Explicit exclusions

- no changes to `SemanticSelectionInspector`, `ProjectElement`, project/family/floor/zone services, other selection/reporting tests, persistence, native BricsCAD, LOCAL runners/probes, workflows, release, private data, or GitHub Actions;
- do not replace raw noncanonical rejection with successful normalization;
- report the next independent full-smoke blocker without expanding this claim.

## Validation

- exact one-smoke diff/readback;
- Core and SmokeTests Release builds plus selection-focused preflights when available through the owner runner;
- full deterministic Core smoke must be used only to identify the next blocker; no full-suite PASS is inferred until actually observed.

## Completion record

Pending implementation after this claim is merged to `main`.