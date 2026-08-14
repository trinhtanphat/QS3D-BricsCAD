# Work claim — Licensed V25 runtime qualification closeout harness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T22:12:00+07:00`
- Baseline main SHA: `43622c5f98c9e897431088f886e7437a1f7fbe4a`

## Scope

Close the remote-safe qualification-infrastructure gap for `LOCAL-001` without manufacturing native evidence. The current runner can prove source/build/runtime smoke but hard-codes the full interactive matrix as `NOT_RUN`; this claim adds an exact-SHA/hash-bound sanitized matrix evidence contract and validation path so a licensed local V25 run can truthfully promote the qualification when every required family really passes.

Reserved surfaces:
- `scripts/run-local-v25-qualification.ps1`
- `scripts/test-local-v25-interactive-matrix-evidence.ps1` (new)
- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-V25-INTERACTIVE-MATRIX.example.json` (new)
- focused source guard for the evidence contract, if needed

Explicitly out of scope:
- V25 host lifecycle implementation files already reserved by other ACTIVE claims;
- changing any `LOCAL-001` runtime scenario from pending to PASS without licensed V25 evidence;
- proprietary BricsCAD DLLs, private/customer DWGs, screenshots, credentials or unsanitized machine paths;
- GitHub Actions dispatch/release publication.

## Acceptance

- Matrix evidence is rejected unless it is schema-valid, all required families are `PASS`, exact Git SHA matches the candidate, plugin SHA-256 matches the exact built DLL, BricsCAD major version is V25, Windows/x64 and licensed/interactive attestations are explicit, and no required family is omitted.
- `run-local-v25-qualification.ps1` may report full matrix PASS only after the validator passes; `-SkipRuntime` can never be combined with qualifying matrix evidence.
- The machine-readable `qualification.json` distinguishes automated runtime smoke from full licensed interactive qualification and does not infer stable signing/customer-release state from source/static checks.
- Docs provide a one-command exact-SHA closeout path and retain the rule that only a real licensed local run may populate PASS evidence.
