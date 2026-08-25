from pathlib import Path
import re
import sys
ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "run-local-v25-local-011.ps1"
RUNBOOK = ROOT / "docs" / "LOCAL-011-NATIVE-QUALIFICATION.md"
SHAS = ("761b9b92f5dd3638b18d281c273a406e41069511","ffd26294f3f27d03de1050643aa0aeb894dcb0f2","1850f02382c8ccf71f04e3ea9daa28455aaae08f","b22eacd681230f231e0f970fb670e8f89769c35e")
CASES = ("native.before_commit_abort","native.during_commit_abort","native.after_commit_ui_failure","native.document_lock_multi_dwg","recognition.stale_apply_no_project","modeless.door_detached","modeless.room_detached","modeless.bbs_detached","modeless.bq_canonical_write","modeless.rebar_mesh_stale_save","palette.unavailable_project_teardown_rebind","generated.grid_stale_handle","generated.curtain_line_stale_handle","generated.curtain_path_stale_handle","generated.rebar_stale_handle","generated.rebar_malformed_metadata","generated.rebar_duplicate_canonical","generated.full_live_exact_replacement","generated.foreign_object_protection","generated.undo_save_reopen","isolation.other_dwg_untouched")
def fail(msg): print(f"ERROR: LOCAL-011 native runner preflight failed: {msg}", file=sys.stderr); raise SystemExit(1)
if not RUNNER.is_file(): fail("missing runner")
if not RUNBOOK.is_file(): fail("missing runbook")
r=RUNNER.read_text(encoding="utf-8"); d=RUNBOOK.read_text(encoding="utf-8")
for x in SHAS:
    if x not in r: fail(f"missing source-ready ancestor {x}")
for x in CASES:
    if f'"{x}"' not in r or f'`{x}`' not in d: fail(f"missing matrix case {x}")
if "run-local-v25-qualification.ps1" not in r: fail("canonical V25 baseline is required")
if re.search(r"run-local-v25-qualification\.ps1[\s\S]{0,300}-SkipRuntime", r): fail("licensed baseline must not use -SkipRuntime")
for token in ('runtimeSmokeStatus -ne "PASS"','git status --porcelain','working tree must be clean','git merge-base --is-ancestor','localPassClaimedByRunner=$false','exactSha=$HeadSha'):
    if token not in r: fail(f"missing fail-closed token: {token}")
if 'Get-Process -Name "bricscad"' not in r or "never kills unrelated sessions" not in r: fail("must guard unrelated BricsCAD sessions")
if "Stop-Process" in r or ".Kill(" in r: fail("must never terminate BricsCAD")
if "SECURELOAD" in r: fail("must not weaken BricsCAD security policy")
for bad in ("QS3D_FAULT_INJECT","QS3D_PRODUCTION_FAULT","SetEnvironmentVariable(\"QS3D_FAULT"):
    if bad in r: fail(f"forbidden production fault switch: {bad}")
if 'PASS {0}, FAIL {0}, or BLOCKED {0}' not in r or "Evidence note is too short" not in r: fail("per-case evidence is not fail-closed")
if "exit 2" not in r or "exit 3" not in r: fail("BLOCKED/NO_RESULT must be nonzero")
for token in ("one command","run-local-v25-local-011.ps1","does not add a production fault switch","PASS","BLOCKED","NO_RESULT"):
    if token.lower() not in d.lower(): fail(f"runbook missing token: {token}")
print("LOCAL-011 native runner preflight PASS")