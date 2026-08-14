# Work claim — QSDB Family/element category reference integrity

- Status: `COMPLETED`
- State: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-qsdb-family-category-reference`
- Registered: `2026-08-14T22:55:00+07:00`
- Scope amended: `2026-08-14T22:57:00+07:00`
- Baseline main SHA: `07ec0cf3a4718854fe064b6197f3129d0fab0b16`
- Claim publication SHA: `1329315641c39c4c3e0f71e10fb1e936017bddbd`
- Claim amendment SHA: `5f78adec71f292bf04014283bcb5b7825ef3bbae`
- Implementation branch: `agent/chatgpt-web-gpt56sol/qsdb-family-category-reference-20260814`
- Implementation commit: `fdfec2603b638e834d0f30b499e46393b5cb9649`
- Integration branch: `integration/chatgpt-web-gpt56sol-qsdb-family-category-reference-20260814`
- Initial integration candidate: `5a3f808b137427b4d3a803e63c2c517f8e11cc7f`
- Reconciled integration candidates: `19cddba2a825c9989c4a7241bba2651e4757ebe0`, `09abd41e80671bb003f8516c47b3af42c063e451`
- Final source landing on main: `09abd41e80671bb003f8516c47b3af42c063e451`
- Priority: Core P1 persistence / semantic relation integrity found during owner-requested continue-all audit

## Confirmed defect

`ProjectFamilyService.Assign(...)` treats a referenced Family whose `Category` differs from the element category as invalid state that must be repaired before reassignment. However `ProjectFamily.Category` can be changed to another defined `ElementCategory`, while the QSDB semantic boundary previously validated only individual category tokens and Family-id existence. A contradictory element-to-Family relation could therefore reach persisted state and later load without category-parity validation.

## Completed implementation

- `QsdbProjectXmlSchemaValidator.ValidateElementReferences(...)` now resolves the persisted Family category catalog and rejects a non-empty `familyId` whose Family category differs from the element category.
- Existing missing-Family validation still runs first; empty/unbound Family ids remain valid.
- Existing named/defined category-token validation remains authoritative; no second category grammar was introduced.
- `QsdbProjectStore.cs`, domain models, schema version and migrator were left unchanged after call-order review proved the current-schema validator is already shared by staged-save validation and load-before-materialization.
- `QsdbFamilyCategoryReferenceSmoke` covers matching-category round-trip, staged-save rejection after a valid Family is changed to another category, tampered current-schema load rejection, and unbound Family round-trip.
- `SmokeTestRegistration` registers the focused smoke in the deterministic Core smoke suite.

## Final changed implementation surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbFamilyCategoryReferenceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file for coordination/close-out

## Concurrency / integration evidence

- Implementation was one coherent agent commit and changed exactly the three reserved implementation files.
- While the lane was active, `main` advanced repeatedly through generic-metadata, QuantityRule, UI, Rebar, WPF and ProjectElement-property claims/implementations. Each final gate compared those deltas before reconciliation; none modified or reserved this validator/category-reference lane.
- Integration was repeatedly rebuilt on refreshed `main` rather than force-updating a stale candidate.
- Final source landing `09abd41e80671bb003f8516c47b3af42c063e451` was applied to `main` with `force:false` and preserved current-main ancestry plus the reviewed integration history.
- Later commercial-signing merge `67854db0bd996f32a7c4c206c390fb2ed74c921f` has `09abd41e80671bb003f8516c47b3af42c063e451` as a parent, and its claim-close descendant `3a25e068804ef37587f32ff5f41a241278d0763c` preserves that lineage.

## Validation evidence

- Remote readback at `09abd41e80671bb003f8516c47b3af42c063e451` resolves validator blob `0fff4ac2e67713d28a4a39ac3dc89b85653551fc` with Family/element category-parity validation present.
- Remote readback at the same landing resolves smoke blob `8bb1b7f09d141cf3aefcab78daf03fe6d8173a9e` and registration blob `8a0a68ed301405a67db64c071bd7f2615a3e5d55` with `QsdbFamilyCategoryReferenceSmoke.Run()` registered.
- The owner-approved automatic dispatcher started for exact source landing `09abd41e80671bb003f8516c47b3af42c063e451` as workflow run `31818877410`; at observation time it was still `in_progress`.
- Before that run produced a terminal result, newer integration-relevant commercial-signing source reached `main`, so any later automatic V25 evidence must be attributed to its exact dispatched/current source tree rather than retroactively called a PASS for `09abd41e...`.
- This connector session did not execute a local .NET/Core smoke binary or licensed BricsCAD runtime. No managed smoke/native PASS is claimed.
- No manual GitHub Actions dispatch/rerun/cancel and no force-push were performed.

## Completion

The remote-safe QSDB semantic-integrity fix and focused registered regression are landed and remain reachable from current `main`; remote source/test readback and ancestry are verified. The claim is released. Automatic cloud CI and LOCAL_ONLY BricsCAD qualification remain separate exact-SHA evidence under repository policy.
