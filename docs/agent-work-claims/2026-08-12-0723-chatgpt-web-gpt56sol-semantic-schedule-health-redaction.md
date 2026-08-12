# Work claim — semantic schedule health exception redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-schedule-health-redaction`
- Registered: `2026-08-12T07:23:00+07:00`
- Baseline main SHA: `6d00dad3d8caafbcc677bc9abb22feae1cbaa930`
- Priority: P1 — persisted schedule diagnostics must not reflect raw parser/template exception detail.
- Task Key: `CORE-SEMANTIC-SCHEDULE-HEALTH-REDACTION`

## Confirmed defect

`SemanticScheduleHealthService.Inspect(...)` catches catalog data failures and emits `ex.Message` inside `SEMANTIC_SCHEDULE_CATALOG_INVALID`. `InspectTemplates(...)` likewise appends each template-validation `ex.Message` beside the persisted column header before emitting `SEMANTIC_SCHEDULE_TEMPLATE_INVALID`. Those exceptions can be derived from persisted/imported schedule payloads and semantic-tag templates. Because the provider handles the exceptions internally, aggregate `ComprehensiveModelHealthService` provider-failure redaction cannot sanitize them.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SemanticScheduleHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` gate for exception-detail redaction
- this claim file

Do not modify `SemanticScheduleCatalog`, `SemanticTagRenderer`, schedule definition limits/snapshots, native schedule Tables, or BricsCAD runtime code. The concurrent semantic-schedule definition snapshot lane is a different Documentation surface.

## Intended contract

- Preserve existing catalog/template Error codes and bounded diagnostic behavior.
- Keep existing bounded exception filters (`IsCatalogDataFailure`, `IsTemplateFailure`) unchanged unless required for compilation.
- Remove raw `Exception.Message` reflection from health output; retain schedule/column identity that is intentionally part of the diagnostic domain model.
- Preserve inspection as read-only and all missing/ambiguous reference diagnostics.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Catalog/template failures remain fail-visible with stable redacted messages, exception detail is no longer reflected, focused regression coverage pins the contract, and this claim is closed after merged-main readback.
