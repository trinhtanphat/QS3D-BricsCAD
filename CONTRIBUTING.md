# Contributing to QS3D-BricsCAD

QS3D-BricsCAD is a Windows/BricsCAD plugin with a host-neutral Core, a BricsCAD V25 adapter, and an isolated V26 adapter. Changes are expected to preserve deterministic Core behavior, host/version boundaries, and auditable release provenance.

## Before changing code

Repository policy is authoritative in this order for contribution workflow:

1. `docs/MAIN-WRITE-AUTHORIZATION.md`
2. `AGENTS.md`
3. `docs/AGENT-RUNTIME-CONTRACT.md`
4. `CI_POLICY.md`
5. `docs/AGENT-WORK-REGISTRATION.md`
6. `docs/PR-CI-LIFECYCLE.md` for PR/branch-CI timing corrections

Read the latest `main`, check existing Issues/PRs/active lanes, and reuse an existing task issue when it already owns the scope. Otherwise create a focused issue before implementation when practical.

## Branch and commit workflow

Normal work uses a dedicated branch from the latest valid `main`, normally `agent/<agent-id>/<scope>`. Put all task changes on that branch, including source, tests, scripts, workflows, docs, Markdown and handoff/claim updates.

Keep commits reviewable and request-scoped. Do not split one coherent fix into file-by-file commits, and do not mix unrelated fixes just because they were found in the same session.

`main` is read-only for direct task writes by normal agents and contributors. Requests such as `fix bug`, `continue all`, `commit push git`, `update docs`, or `update md` never authorize a direct contents write/ref update to `main`.

For a normal repository-owner request, `docs/MAIN-WRITE-AUTHORIZATION.md` provides standing authorization to merge the **same task PR** once all current required checks are green, the candidate is current and mergeable, and the owner has not explicitly opted out. That standing authorization does not permit unrelated/bulk merges or any protection bypass.

## Validation

Validation is determined by changed paths, not by commit-message prefixes.

For paths watched by `.github/workflows/ci.yml`, push the task branch and obtain the exact-head Shared Branch CI evidence required by the current lifecycle policy. Preferred sequencing is branch CI success before opening a new watched-path PR when the admission gate applies. However, if the one canonical PR already exists while branch CI is queued/running or finishes later, timing alone does not invalidate that PR; do not close/recreate the carrier solely to reorder timestamps. Follow `docs/PR-CI-LIFECYCLE.md`.

Markdown-only work has two practical classes:

- ordinary docs/claim/handoff files outside policy/source-guard watched paths may use the lightweight non-build path and do not require Core/V25 build merely because a `.md` file changed;
- governance/policy Markdown explicitly classified by shared CI must run its policy/source guards, but still does not require Core/V25 build unless another build-relevant path changed.

The protected-main merge candidate must satisfy the repository-required `preflight` and `core` checks. Do not weaken assertions, source guards, package verification, or provenance checks to make CI green; fix the underlying defect.

Licensed BricsCAD runtime evidence is separate from remote-safe compile/source validation. If a task requires native V25/V26 runtime testing and it has not actually run on an appropriate licensed host, report `PENDING_LOCAL` rather than PASS.

## Release-sensitive changes

Changes to release workflows, version identity, signing, package/update manifests, provenance, or BricsCAD reference acquisition require fail-closed validation. Preview and commercial release lanes must remain distinct, and release automation must not bypass protected `main`.

Ordinary docs/Markdown/chore-only changes outside the V25 dispatcher's watched integration-relevant paths must not trigger the automatic V25 cloud release path. Changed paths are authoritative; prefixes such as `docs:`, `md:` or `chore:` are not sufficient evidence that release/CI behavior should be skipped.

Do not commit BricsCAD runtime assemblies, private drawings, signing certificates/private keys, credentials, customer data, or generated release artifacts. `.gitignore` defines additional repository-specific exclusions.

## Pull requests

Use the pull-request template. Link the task issue, record the exact validated branch SHA, identify runtime/release impact, and keep the PR limited to its registered scope.

A green PR does not create merge permission by itself. Merge the same owner-requested task PR only through the protected path when the standing same-task authorization in `docs/MAIN-WRITE-AUTHORIZATION.md` applies and every current gate is satisfied. Otherwise stop at the authorized boundary.

## Security reports

Do not open a public issue for a suspected exploitable vulnerability or leaked secret. Follow `SECURITY.md` for private reporting guidance.
