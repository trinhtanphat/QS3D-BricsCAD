# Work claim — QSDB negative element quantity persistence integrity

- Status: `COMPLETED`
- State: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-20260814-qsdb-negative-quantity`
- Registered: `2026-08-14T20:52:00+07:00`
- Last Updated: `2026-08-14T21:03:00+07:00`
- Baseline main SHA recorded at discovery: `0ea20bdc09359a286270f97a567eea9b180b2a6e`
- Claim publication parent after race reconciliation: `9f2f7e58ab1f81ad652823c6eada18646ad61f8e`
- Claim publication commit: `00ddbc64ab2bc5a10c903e93321dcbdb30d2dabb`
- Priority: Core P1 persistence integrity defect found during owner-requested full-project audit
- Task Key: `PERSISTENCE-QSDB-NEGATIVE-ELEMENT-QUANTITY`
- Implementation branch: `agent/chatgpt-web-gpt56sol-20260814-qsdb-negative-quantity/qsdb-negative-quantity-persistence`
- Implementation source commit: `3fdb408c9267ce9b5cb119935411dca8bd88466f`
- Regression commit: `baee7e1700cf7c2daa65a25762d9c47ff6e6bf39`
- Integration branch: `integration/chatgpt-web-gpt56sol-qsdb-negative-quantity-persistence-20260814`
- Initial integration commit: `d37e9a3394bff24aad838b9802da1e3267e64c74`
- Final reconciled integration / main landing commit: `40a20146134e9b12602a8cbc5e0b145082c3cd80`

## Confirmed defect

`ProjectElement.SetQuantity(...)` rejects negative physical quantities, but the public `Quantities` dictionary can still be mutated directly. Before this lane, a directly inserted negative value could reach QSDB serialization. On a later `Load(...)`, a negative persisted `<q value="...">` could reach `SetQuantity(...)` and throw `ArgumentOutOfRangeException`; `LoadWithBackupFallback(...)` intentionally treats persistence/data-shape failures such as `InvalidDataException` as recoverable, so that domain exception could bypass an otherwise valid `.bak` recovery path.

The key call-order finding was that both publication and loading already share the current-schema XML validator: save validates the staged QSDB before atomic publication, while `ProjectSchemaMigrator.MigrateToCurrent(...)` invokes `QsdbProjectXmlSchemaValidator.ValidateCurrent(...)` before domain materialization. Therefore the smallest complete fix is at that shared persistence semantic boundary.

## Completed scope

- `QsdbProjectXmlSchemaValidator.ValidateCurrent(...)` now rejects parseable negative `<element>/<quantities>/<q value="...">` values with `InvalidDataException`.
- The guard is scoped only to element quantities; negative floor elevations and unrelated numeric fields are untouched.
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs` now pins direct negative dictionary mutation failing before primary publication and corrupted-primary recovery through a validated `.bak`.
- The regression also pins positive `2d` primary round-trip and zero `0d` backup round-trip.
- `QsdbProjectStore.cs` was reserved during discovery but intentionally left unchanged after call-order review proved the existing validator is already the common fail-closed boundary for both paths. No catch broadening, domain-model change, or schema-version change was needed.

## Final changed surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim file

## Concurrency / integration evidence

- Claim-first publication initially raced with another agent; the stale fast-forward was rejected by GitHub with `422`, then the claim-only commit was rebuilt on refreshed `main` and published without force.
- Agent implementation remained exactly two files relative to claim commit: validator `+13/-0`, smoke `+49/-0`.
- Before first integration, concurrent `main` changes were claim-docs only and did not touch the reserved implementation surfaces.
- A first final landing attempt also raced and was rejected as non-fast-forward. The integration branch was reconciled without force using a two-parent commit that preserved concurrent `main` claim changes and the reviewed integration result.
- Final landing `40a20146134e9b12602a8cbc5e0b145082c3cd80` has current-main parent `e91a20d0de7fa2c807b77a81aaf451dabbfe998a` and integration parent `d37e9a3394bff24aad838b9802da1e3267e64c74`, so both histories are preserved.

## Validation evidence

- Remote readback at `40a20146134e9b12602a8cbc5e0b145082c3cd80` resolves validator blob `2ecad1bd26614c7b98efe5fe11b345eadb98d218` with the non-negative quantity guard present.
- Remote readback at the same landing resolves smoke blob `b7297ec5d874d56af72af601d140bdb7ec7fb7e4` with `NegativeQuantityFailsClosed()` registered in the smoke runner.
- Regression source explicitly covers: direct `Quantities["AreaM2"] = -1d` save rejection with no primary file, valid primary `2d`, valid backup `0d`, tampered primary `-1`, backup fallback activation, expected backup source, and retained primary failure message.
- At post-landing inspection time, GitHub connector returned no combined commit statuses and no workflow runs for `40a20146134e9b12602a8cbc5e0b145082c3cd80`.
- This connector-only environment did not execute the .NET/Core smoke binary and did not execute BricsCAD native/runtime validation. No managed/native PASS is claimed.
- No manual GitHub Actions dispatch/rerun and no force-push were performed.

## Completion

The remote-safe persistence fix and deterministic regression source are landed on `main` at `40a20146134e9b12602a8cbc5e0b145082c3cd80`, remote source/test readback is verified, concurrent work was preserved, and this claim is released. Any later automatic CI or LOCAL_ONLY BricsCAD qualification is separate evidence under repository policy.
