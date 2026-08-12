# Work claim — Semantic selection inspector nullability build blocker

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-inspector-nullability-build-20260812-0955`
- Registered: `2026-08-12T09:55:00+07:00`
- Baseline main SHA observed immediately before claim: current moving `main`
- Priority: P0 Core strict Release compile blocker
- Task Key: `CORE-SELECTION-INSPECTOR-CS8602`

## Evidence

The completed XLSX negative-preflight lane recorded an actually executed strict Core Release build blocked by `CS8602` at `SemanticSelectionInspector.cs:177`. In `ValidateSemanticReferences(...)`, `familyIndex.TryGetValue(...)` proves key existence but nullable analysis still treats the retrieved `ProjectFamily` as potentially null before `family.Category` / `family.Id` dereference.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs`
- focused regression only if behavior changes
- this claim file

## Intended fix

Make the family lookup explicitly fail closed if a null value is ever observed, preserving the existing missing-family diagnostic path, family/category mismatch semantics, canonical relation validation, selection freshness and all valid inspection outputs. This is a nullable-flow/defensive integrity fix, not a semantic redesign.

## Coordination

Recent semantic-selection relation-canonicality and handle-ownership lanes are completed. Do not touch bulk edit, Workspace/native UI, semantic handle ownership, or LOCAL-003 V25 scope.

## Validation boundary

Exact source readback and ancestry verification. No GitHub Actions dispatch and no claim of full Core/BricsCAD runtime PASS unless actually executed.