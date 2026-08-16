# Contributing to QS3D-BricsCAD

QS3D-BricsCAD is a Windows/BricsCAD plugin with a host-neutral Core, a BricsCAD V25 adapter, and an isolated V26 adapter. Changes are expected to preserve deterministic Core behavior, host/version boundaries, and auditable release provenance.

## Before changing code

Repository policy is authoritative in this order for contribution workflow:

1. `docs/MAIN-WRITE-AUTHORIZATION.md`
2. `AGENTS.md`
3. `CI_POLICY.md`
4. `docs/AGENT-WORK-REGISTRATION.md`

Read the latest `main`, check existing Issues/PRs/active lanes, and reuse an existing task issue when it already owns the scope. Otherwise create a focused issue before implementation when practical.

## Branch and commit workflow

Normal work uses a dedicated branch from the latest valid `main`, normally `agent/<agent-id>/<scope>`. Put all task changes on that branch, including source, tests, scripts, workflows, docs, Markdown and handoff/claim updates.

Keep commits reviewable and request-scoped. Do not split one coherent fix into file-by-file commits, and do not mix unrelated fixes just because they were found in the same session.

`main` is read-only for normal agents and contributors. A green build or a request such as `fix bug`, `continue all`, `commit push git`, or `update docs` does not authorize a merge. Only explicit owner integration authorization may change `main`.

## Validation

For paths watched by `.github/workflows/ci.yml`, push the task branch and obtain successful Shared Branch CI on the exact current head SHA before treating the branch as PR-ready. Refresh `main` after validation and reconcile if it moved.

The protected-main merge candidate must satisfy the repository-required `preflight` and `core` checks. Do not weaken assertions, source guards, package verification, or provenance checks to make CI green; fix the underlying defect.

Licensed BricsCAD runtime evidence is separate from remote-safe compile/source validation. If a task requires native V25/V26 runtime testing and it has not actually run on an appropriate licensed host, report `PENDING_LOCAL` rather than PASS.

## Release-sensitive changes

Changes to release workflows, version identity, signing, package/update manifests, provenance, or BricsCAD reference acquisition require fail-closed validation. Preview and commercial release lanes must remain distinct, and release automation must not bypass protected `main`.

Do not commit BricsCAD runtime assemblies, private drawings, signing certificates/private keys, credentials, customer data, or generated release artifacts. `.gitignore` defines additional repository-specific exclusions.

## Pull requests

Use the pull-request template. Link the task issue, record the exact validated branch SHA, identify runtime/release impact, and keep the PR limited to its registered scope. A PR is a review/integration handoff; it does not self-authorize merge.

## Security reports

Do not open a public issue for a suspected exploitable vulnerability or leaked secret. Follow `SECURITY.md` for private reporting guidance.
