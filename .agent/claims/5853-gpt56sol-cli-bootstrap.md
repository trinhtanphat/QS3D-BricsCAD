# Agent reservation — issue #5853

Status: ACTIVE / REMOTE_SAFE / IMPLEMENTING
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260905t1558-cli-bootstrap
Canonical carrier: agent/gpt56sol-20260905t1558-cli-bootstrap/issue-5853-cli-bootstrap
Lane-Key: issue-5853
Ownership-Key: qs3d-code.cli-bootstrap-v1
Branch: agent/gpt56sol-20260905t1558-cli-bootstrap/issue-5853-cli-bootstrap
Expected-Paths: tests/QS3D.Code.Cli.SmokeTests/QS3D.Code.Cli.SmokeTests.csproj; scripts/preflight-qs3d-code-cli-bootstrap.py; .agent/claims/5853-gpt56sol-cli-bootstrap.md

Scope: isolate the QS3D Code CLI smoke/bootstrap project from the production CLI -> full QS3D.Core -> external/QS3D-Platform ProjectReference chain so feature source validation can execute before core-job submodule hydration. Production `src/QS3D.Code.Cli/QS3D.Code.Cli.csproj`, #5539/#5540, host bridge code, MCP transport runtime, and BricsCAD runtime are explicitly out of scope.

TDD: first add a focused preflight that fails while the smoke project has a ProjectReference, then replace only that smoke-project dependency with bounded source links and require the smoke executable to pass under the feature-source-guard environment.
