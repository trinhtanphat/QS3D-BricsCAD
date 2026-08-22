# Work claim — sanitized local V25 evidence allowlist

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-sanitized-summary-step-labels`
- Registered: `2026-08-12T00:12:00+07:00`
- Expanded: `2026-08-12T00:16:00+07:00`
- Completed: `2026-08-12T00:20:00+07:00`
- Baseline main SHA: `1f4fc6979b95a8efb2b00d49d1b59a40756b6630`
- Priority: owner-requested continue-all review; close verified sanitized-evidence fail-open paths where arbitrary raw qualification metadata could be copied into the shareable Markdown even though the handoff contract says only allow-listed result data may be carried.

## Completed changes

- `c2a78f20da75aeb78db3761dcfd325d185de6579` — `scripts/export-local-v25-sanitized-summary.py` now emits only canonical qualification-runner step names; unknown/untrusted `steps[].name` values become deterministic `Step N (redacted label)` text instead of being echoed.
- `748bd8849dff4bdd9d5c8461aa7bd9160e35b422` — existing sanitized-evidence preflight now injects Windows/POSIX/private-DWG/Markdown payloads into step names, verifies they never reach Markdown, and checks every current `Invoke-QualificationStep` name remains represented in the exporter allowlist.
- `608988b562cf19936a1fa7dac01c98b8a54140a0` — documented the step-label allowlist/redaction boundary.
- `9b9b28a59ce3bdcaae6b8364290ccf0f6e4ebaa3` — expanded this active claim after validation exposed the same fail-open in the generic path-capable token helper.
- `345b99343cce302ba8cc2d1f5626e3f935ad765e` — removed broad `safe_token` use for shareable metadata; qualification scope is a finite runner-derived allowlist, non-neutral branch identities are redacted, and release tags must pass strict semantic parsing.
- `dc46391a1e05eb87c4009c46c9d21f34e12c0b68` — further restricted shareable release-tag text to neutral prerelease channels (`preview`, `alpha`, `beta`, `rc`) with numeric suffix/build identifiers, so arbitrary customer/project identifiers are not echoed merely because they are syntactically valid SemVer.
- `9e6da3de86b3f26ece6dd99de1b5f07a9199ed9a` — regression now covers hostile branch/scope/release-tag values, bans the broad path-capable token helper, checks all canonical runner scopes/steps against the exporter allowlists, and preserves valid `main` / `source-build` / `v0.1.0-preview.2` output.
- `fa73d76c8d76de5c53ebaa458a492d4b1716f0f0` — aligned the shareable-result documentation with branch/scope/tag/step redaction behavior.

## Validation evidence

- Inspected the exact implementation diff for `c2a78f20...`; it only introduced the canonical step-name allowlist/sanitizer and routed step-table labels through it.
- Inspected the exact release-tag diff for `dc46391a...`; it only tightened sanitized release-tag disclosure and did not change package/release semantics.
- Executed the exact authored exporter plus exact authored `preflight-local-v25-sanitized-evidence.py` with `python -S` in a deterministic synthetic repository fixture containing all current canonical runner step names/scopes and required handoff-document tokens. Exit code `0`; output ended with `PASS`.
- Baseline fixture proves canonical `Core deterministic smoke suite`, `Licensed V25 NETLOAD / Ribbon / Palette runtime probe`, `main`, `source-build` and `v0.1.0-preview.2` remain readable.
- Hostile fixtures prove Windows/POSIX paths, private-DWG names, Markdown/file URLs, customer-bearing step labels, non-neutral branch names, path-like qualification scope and customer-bearing prerelease labels are redacted rather than echoed.
- No GitHub Actions were dispatched/re-run. No BricsCAD runtime, private DWG, signing operation, package publication or customer-release qualification was performed or claimed.

## Coordination / exclusions respected

No change was made to `scripts/run-local-v25-qualification.ps1` execution semantics, source/product code under `src/**`, tests under `tests/**`, updater/release workflow behavior, active product lanes or LOCAL_ONLY V25 qualification. Concurrent `main` work was preserved through SHA-guarded Contents API updates with no force-push.

## Result

The shareable local V25 evidence exporter now fails closed on unreviewed text-bearing metadata rather than relying on a character-only sanitizer. Canonical runner step/scope information and neutral public release identifiers remain useful, while unknown step labels, branch/customer identifiers, path-like scopes and non-neutral release tags cannot leak into the generated Markdown handoff. This lane is complete.
