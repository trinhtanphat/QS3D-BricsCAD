# QSDB Audit Action Canonicality Plan

## Goal

Align persisted audit provenance with the canonical runtime `AuditTrail.Record(...)` contract without changing audit vocabulary or unrelated payload fields.

## Evidence

- `AuditTrail.Record(...)` rejects blank actions and trims valid action names before mutation.
- `QsdbProjectXmlSchemaValidator.ValidateAudit(...)` currently validates only event attributes/children and does not require a canonical `action` value.
- `QsdbProjectStore.ValidateProject(...)` validates audit event timestamps but not action identity.
- `ProjectState.AuditEvents` is mutable, so direct in-memory insertion can bypass the runtime recorder and reach persistence.

## Implementation

1. In `QsdbProjectXmlSchemaValidator.ValidateAudit(...)`, require `action` through the existing required-canonical-attribute helper.
2. In `QsdbProjectStore.ValidateProject(...)`, validate each non-null audit action as non-empty and exactly trimmed before serialization.
3. Preserve current timestamp validation and all existing payload semantics for element id/detail/actor/correlation id.
4. Add isolated Core smoke coverage proving:
   - direct in-memory blank/padded actions fail before publication;
   - malformed current-schema files with blank/padded actions fail to load;
   - a canonical action still saves and loads unchanged.

## Safety

- No `AuditTrail.cs` edit.
- No schema-version bump: this tightens validation of an invariant already established by the runtime API.
- No Actions/release dispatch.
- Re-fetch every target immediately before update and verify ancestry after concurrent commits.
