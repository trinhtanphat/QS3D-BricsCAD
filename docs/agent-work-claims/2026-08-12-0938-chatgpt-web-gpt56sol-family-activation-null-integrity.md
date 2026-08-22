# Work claim — Family activation null integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-activation-null-integrity-20260812-0938`
- Registered: `2026-08-12T09:38:00+07:00`
- Completed: `2026-08-12T09:41:00+07:00`
- Baseline main SHA: `91c1754d03c340e395045ad8b6dcace0a3af7d35`
- Priority: evidence-driven Core malformed-project fail-closed integrity

## Confirmed defect

`ProjectFamilyActivationService.ValidateUniqueFamilyIds(...)` skipped `null` entries in `project.Families`. As a result, `GetActive(...)`, `SetActive(...)` and `ClearIfMissing(...)` could continue reading or mutating active-family metadata on a malformed project that the persistence/global family-integrity contracts reject.

## Implemented fix

Family activation validation now fails closed with the existing canonical Family-service diagnostic when the collection contains a null entry. The completed duplicate-family guard, case-insensitive canonical family resolution, valid lookup/no-op behavior, `Touch()` semantics and metadata key/value format remain unchanged.

## Integration evidence

- Claim registration: `957d233ca4e2a53d0a7a86db05234f81638dc71c`.
- Source fix: `5047316d257de8c57baaa1952c55b9f9a6300673`.
- Focused smoke: `6f659c2d030760362f42045e90c70680c9199fc0`.
- Moving-main source readback confirmed null Family entries throw `Project family collection contains a null family.` before activation resolution or mutation.
- Smoke readback confirmed `GetActive`, `SetActive` and `ClearIfMissing` all reject without changing `ActiveFamilyId`, `ChangeVersion` or `UpdatedUtc`, while a valid active-Family lookup remains unchanged.

## Coordination

The immediately preceding family activation duplicate-integrity lane is `COMPLETED`. This lane did not edit `ProjectFamilyService`, template application, family property-key canonicality, Workspace UI, persistence or local V25/native paths.

## Validation boundary

Source-safe focused regression + exact readback were committed. No GitHub Actions were dispatched; no full Core build/smoke PASS or BricsCAD V25/V26 runtime PASS is claimed.