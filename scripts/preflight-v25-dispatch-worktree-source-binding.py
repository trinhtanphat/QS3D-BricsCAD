from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW_PATH = ROOT / ".github/workflows/dispatch-v25-cloud-after-main-integration.yml"
WORKFLOW = WORKFLOW_PATH.read_text(encoding="utf-8")

source_select = 'source_sha="${GITHUB_SHA,,}"'
source_validate = 'if [[ ! "${source_sha}" =~ ^[0-9a-f]{40}$ ]]; then'
release_inputs = "release_relevant_pathspecs=("
version_read = 'version_project="src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"'
gate_run = 'python "${gate_args[@]}"'

for token, label in (
    (source_select, "push source selection"),
    (source_validate, "source SHA validation"),
    (release_inputs, "release-relevant path classification"),
    (version_read, "committed version read"),
    (gate_run, "release batch gate execution"),
):
    if token not in WORKFLOW:
        raise SystemExit(f"FAIL v25 dispatcher worktree binding: missing {label}: {token}")

validate_at = WORKFLOW.index(source_validate)
release_inputs_at = WORKFLOW.index(release_inputs)
version_at = WORKFLOW.index(version_read)
gate_at = WORKFLOW.index(gate_run)
if not (validate_at < release_inputs_at < version_at < gate_at):
    raise SystemExit("FAIL v25 dispatcher worktree binding: unable to establish canonical release-source ordering")

binding_window = WORKFLOW[validate_at:release_inputs_at]
required_binding_tokens = (
    "dispatch_source_ref='refs/remotes/origin/qs3d-dispatch-source-admission'",
    'git fetch --no-tags origin "+refs/heads/main:${dispatch_source_ref}"',
    'fetched_dispatch_main="$(git rev-parse "${dispatch_source_ref}^{commit}")"',
    'git merge-base --is-ancestor "${source_sha}" "${fetched_dispatch_main}"',
    'git checkout --detach "${source_sha}"',
    'bound_worktree_sha="$(git rev-parse HEAD)"',
    'if [[ "${bound_worktree_sha,,}" != "${source_sha}" ]]; then',
)
for token in required_binding_tokens:
    if token not in binding_window:
        raise SystemExit(
            "FAIL v25 dispatcher worktree binding: release-relevant source inspection can run before exact admitted-source worktree binding: "
            + token
        )

if binding_window.index('git checkout --detach "${source_sha}"') > binding_window.index('bound_worktree_sha="$(git rev-parse HEAD)"'):
    raise SystemExit("FAIL v25 dispatcher worktree binding: worktree identity is checked before exact-source checkout")

# The workflow_run path deliberately rebinds source_sha to current protected main; this makes
# exact worktree rebinding mandatory rather than optional. Preserve that intent while preventing
# a stale event checkout from supplying release/version/gate bytes for a newer source SHA.
workflow_run_rebind = 'source_sha="${current_main,,}"'
if workflow_run_rebind not in WORKFLOW:
    raise SystemExit("FAIL v25 dispatcher worktree binding: workflow_run current-main source rebinding disappeared")
if WORKFLOW.index(workflow_run_rebind) >= validate_at:
    raise SystemExit("FAIL v25 dispatcher worktree binding: workflow_run source must be selected before exact-source admission")

print("PASS V25 cloud dispatcher binds its worktree to the exact admitted source SHA before release-relevant inspection")
