# Work claim — Semantic Selection raw relation canonicality fixture

- Status: `COMPLETED`
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

- Claim PR #1293 merged as `ba719557f7cf37290c6f2508946f569440b6948d` (claim head `d394c15ca1c2884147faee30cc5f1787e77623e2`).
- Implementation commit `a8c190e16d3003e1e5f5e91daf718733c980a594` merged through PR #1296 as `6b35e633c0091e67468c1ef93b545eed1476e57f`.
- The implementation changes exactly one smoke file, +25/-0: each padded public relation write is first proven canonical, then the matching private raw relation field is injected and proven visible before the existing fail-closed inspection assertion. Canonical resets and the whitespace-only blank control remain.
- Production `SemanticSelectionInspector`, domain setters, focused gates, native/LOCAL surfaces, workflows and release files are unchanged.
- No fresh full registered Core smoke, BricsCAD runtime, or GitHub Actions PASS is claimed by this closeout. The next blocker must come from fresh validation on a descendant of the merged implementation.