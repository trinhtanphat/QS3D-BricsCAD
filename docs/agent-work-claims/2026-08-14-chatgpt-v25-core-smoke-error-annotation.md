# Agent work claim — V25 Core smoke failure annotation

- Agent: `chatgpt-github-integration`
- Date: 2026-08-14
- Status: `ACTIVE`
- Scope: make `release-v25-cloud.yml` expose deterministic Core smoke stderr/stack trace as a GitHub Actions error annotation when the smoke process exits nonzero.
- Expected implementation branch: `agent/chatgpt/v25-core-smoke-error-annotation-20260814`
- Expected implementation file: `.github/workflows/release-v25-cloud.yml`

## Reason

Fresh V25 cloud run `31795377920` passes source guards and managed builds but fails at `Deterministic Core smoke tests`. The GitHub job-log endpoint available to the integration returns no decoded stdout, while check annotations expose only the generic exit-code annotation. The workflow therefore needs a bounded diagnostic bridge that preserves the existing fail-closed result while surfacing the captured smoke failure through `::error` so the exact next deterministic blocker can be read remotely.

## Constraints

- Do not weaken, skip, `continue-on-error`, or reinterpret the Core smoke gate.
- Preserve the exact `dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release --no-build --no-restore` command semantics.
- Capture combined process output, print it normally, and on nonzero exit emit a bounded/single-line escaped GitHub error annotation before exiting with the original nonzero code.
- Do not touch Core/product behavior, release preparation safety, installer acquisition, packaging, tags, or publishing semantics.
- Source/workflow implementation must land through an agent branch and integration branch; no force-push.

## Completion condition

The diagnostic-only workflow patch is reachable from current `main` through the required integration flow, a fresh automatic V25 cloud run starts, and a failed Core smoke exposes its exact failure via check annotations while remaining failed; if the smoke instead passes, continue through the remaining V25 build/package/release gates.