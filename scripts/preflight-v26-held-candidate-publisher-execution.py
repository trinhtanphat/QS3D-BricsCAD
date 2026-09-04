from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"

text = WORKFLOW.read_text(encoding="utf-8")
admitted = r"-AdmittedScript '.\scripts\publish-v26-release.ps1'"
direct_publish = r"& .\scripts\publish-v26-release.ps1"
identity_guard = "if ($null -eq $candidateIdentity)"
release_job = "  release:\n"
needs_qualify = "    needs: qualify\n"
write_permission = "      contents: write\n"
exact_checkout = "          ref: ${{ github.sha }}\n"

if text.count(admitted) != 2:
    raise SystemExit(
        "V26 held-candidate publication must admit publish-v26-release.ps1 in both signed and unsigned candidate branches."
    )
if text.count(direct_publish) != 1:
    raise SystemExit(
        "V26 held-candidate publication must invoke publish-v26-release.ps1 exactly once after semantic admission."
    )

release_index = text.find(release_job)
needs_index = text.find(needs_qualify, release_index)
permission_index = text.find(write_permission, release_index)
checkout_index = text.find(exact_checkout, release_index)
guard_index = text.find(identity_guard, release_index)
publish_index = text.find(direct_publish, release_index)

if min(release_index, needs_index, permission_index, checkout_index, guard_index, publish_index) < 0:
    raise SystemExit("V26 held-candidate release topology is incomplete.")
if not (release_index < needs_index < permission_index < checkout_index < guard_index < publish_index):
    raise SystemExit(
        "V26 publisher execution must remain in the release job, after qualify dependency, write permission, exact-SHA checkout, and candidate-identity fail-closed guard."
    )
if text.rfind(admitted, release_index, guard_index) < 0:
    raise SystemExit("V26 publisher must be admitted before the candidate identity guard.")

print("PASS: V26 held candidate is admitted before exactly one publisher execution")
