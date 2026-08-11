# Work claim — sanitized local V25 evidence allowlist

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-sanitized-summary-step-labels`
- Registered: `2026-08-12T00:12:00+07:00`
- Expanded: `2026-08-12T00:16:00+07:00`
- Baseline main SHA: `1f4fc6979b95a8efb2b00d49d1b59a40756b6630`
- Priority: owner-requested continue-all review; close verified sanitized-evidence fail-open paths where arbitrary raw qualification metadata can be copied into the shareable Markdown even though the handoff contract says only allow-listed result data may be carried.

## Reserved scope

Harden the local V25 sanitized-summary exporter so:

- step labels emitted into shareable Markdown come only from the canonical qualification runner's fixed safe step-name set; unknown/untrusted step names become deterministic generic labels;
- qualification scope is restricted to the finite values produced by the canonical runner;
- branch disclosure is restricted to public-neutral identities (`main`, `master`, detached `HEAD`) and otherwise redacted because branch names may contain usernames/customer/project identifiers;
- release tags use the release boundary's strict `v<SemVer>` shape rather than a broad path-capable token class.

Strengthen the existing sanitized-evidence regression with malicious path/private-DWG/customer/Markdown cases across these fields and align the local result handoff documentation.

## Expected surfaces

- `scripts/export-local-v25-sanitized-summary.py`
- `scripts/preflight-local-v25-sanitized-evidence.py`
- `docs/LOCAL-V25-RESULT-TEMPLATE.md`
- this claim file for close-out

## Excluded scope

- `scripts/run-local-v25-qualification.ps1` execution semantics or step ordering.
- raw local evidence schema, BricsCAD runtime behavior, private/customer DWGs, signing/package policy, updater/release workflows, `src/**`, `tests/**`, active product lanes, GitHub Actions dispatch/re-run and licensed V25 runtime qualification.

## Validation plan

- Re-fetch exact exporter/preflight blobs before writes and inspect resulting diffs.
- Preserve canonical fixed step labels from the current qualification runner; unknown labels become deterministic ordinal redactions.
- Regression fixture injects Windows/POSIX paths, private DWG/customer text and Markdown/link payloads into `steps[].name`, branch/scope/tag fields and proves none reach sanitized output while known canonical values remain readable.
- Source regression checks that every current canonical runner step and scope remains represented by the sanitizer allowlist.
- Execute the Python regression/model with `python -S` in a synthetic/current-source fixture where practical.
- No GitHub Actions dispatch/re-run.

## Coordination

Recent `main` claims are in updater generation-safe publication, V26 compatibility, quantity, room diagnostics, sheet planning and product/model lanes. No current sanitizer/exporter claim was found; the historical sanitized-evidence implementation is completed. This scope expansion was recorded after discovering the same broad-token fail-open while validating the already-reserved exporter.

## Completion condition

The sanitized exporter no longer echoes unknown step labels or path/customer-bearing branch/scope/tag text, leakage regression/docs are on `main`, and this claim is marked `COMPLETED` with validation evidence.
