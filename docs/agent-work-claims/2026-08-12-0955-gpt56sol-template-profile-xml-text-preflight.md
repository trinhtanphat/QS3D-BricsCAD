# Work claim — template profile XML text preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-profile-xml-text-preflight-20260812-0955`
- Registered: `2026-08-12T09:55:00+07:00`
- Completed: `2026-08-12T10:27:14+07:00`
- Baseline main SHA recorded at registration: `de81a936d5125654bd44176dead1c0a658781234`
- Actual claim parent SHA: `f571c3b49c7280858ca6a1a409841ff0d73898aa`
- Claim commit: `0cec28420216258b4446b92356ad233bd9746bed`
- Superseded PR: `#747` — closed without merge after its moving-base diff became noisy
- Integrated PR: `#751`
- Final reviewed PR head SHA: `030a0072f98a341b81daebd50cbcfccd28bad2a6`
- Integration SHA: `5617b29f78092d519e6d62c6b04b59070046d07c`
- Priority: evidence-driven remote-safe template persistence integrity

## Completed scope

`TemplateProfileStore.Validate(...)` now preflights the exact XML attribute values produced by `Serialize(...)`. XML-invalid control characters and malformed surrogate sequences fail closed with `InvalidDataException` before `Save(...)` resolves/creates the destination path or temp-file workflow. The implementation does not sanitize or rewrite semantic content.

The validation automatically covers serializer-emitted profile identity/name, family identity/name/property keys and values, quantity-rule strings, layer-mapping strings, and visible BQ column names. Existing null family-property value → empty-string serialization semantics are preserved, and valid supplementary Unicode remains round-trippable.

## Implemented surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateProfileXmlTextPreflightSmoke.cs`
- this claim file

## Integration / concurrency evidence

- Initial feature work was based on `main@8bb3539395046e081330d8570029184645499708`.
- While the branch was open, another agent added the empty-BQ apply behavior `else project.Metadata.Remove(VisibleBqColumnsKey);` in the same source file but a non-overlapping method region. The feature source was refreshed to preserve that behavior exactly.
- PR `#747` was deliberately closed without merge when its stale base snapshot made the PR diff noisy; no source integration came from that PR.
- A replacement branch was refreshed with two-parent commits using current-main trees plus the exact reviewed source/smoke blobs, always via fast-forward ref updates with `force: false`.
- PR `#751` final patch contained only the 19-line Template XML-text preflight change plus the focused 117-line smoke file.
- Immediately before integration, compare from PR base `e1134b3b15e912a73ca7e7ddf5f5a9be9b988612` through then-current `main@83a779dacdc877c2613d4d32ab87fecac551b5e5` showed no later `TemplateProfileStore.cs` or smoke overlap.
- PR `#751` integrated as `5617b29f78092d519e6d62c6b04b59070046d07c`, whose parent is `83a779dacdc877c2613d4d32ab87fecac551b5e5` and whose changed files are exactly the reserved source and smoke.
- Remote `main` readback after integration confirms both the XML preflight helper and the concurrent empty-BQ behavior are present, and the focused smoke is present unchanged.

## Validation actually performed

- Reviewed exact PR patch and integration commit.
- Regression source covers XML-invalid template ID, XML-invalid family property value, lone surrogate, failure before destination-directory creation, valid supplementary Unicode round-trip, and retained null-property empty-string semantics.
- `fetch_commit_workflow_runs` for final PR head `030a0072f98a341b81daebd50cbcfccd28bad2a6` returned no workflow runs.
- No GitHub Actions were dispatched.
- No force-push was used.
- No local .NET build/smoke execution PASS is claimed from this connector-only lane.
- No licensed BricsCAD V25/V26 runtime PASS or release qualification is claimed.

## Excluded scope honored

Template XML schema shape/order, category-token policy, family/rule semantic policy, import freshness, UI/native runtime, release/signing and LOCAL_ONLY qualification were not changed by this lane.

## Completion condition

Satisfied. The focused fix and regression are integrated on `main`, remote source/test readback is complete, exact integration evidence is recorded, and this reservation is released.
