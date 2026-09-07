from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def method_body(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        fail(f"missing method signature: {signature}")
    brace = source.find("{", start)
    if brace < 0:
        fail("missing method body")
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[brace:index + 1]
    fail("unterminated method body")
    return ""


source = SOURCE.read_text(encoding="utf-8")
body = method_body(source, "public void SavePreservingValidatedBackup(ProjectState project, string path)")

required = [
    "using (var backupFence = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read))",
    "PersistencePathSafety.RequireExclusiveOpenStillBound(backupFence, backupPath, \"validated project backup\")",
    "var validatedBackup = Load(backupPath);",
    "if (!string.Equals(validatedBackup.ProjectId, project.ProjectId, StringComparison.Ordinal))",
    "SaveCore(project, fullPath, SaveMode.ReplacePrimaryOnly, MaxProjectFileBytes);",
    "Load(fullPath);",
    "var persistedBackup = Load(backupPath);",
    "if (!string.Equals(persistedBackup.ProjectId, project.ProjectId, StringComparison.Ordinal))",
]
for token in required:
    if token not in body:
        fail(f"SavePreservingValidatedBackup is missing required generation-fence token: {token}")

ordered = [body.index(token) for token in required]
if ordered != sorted(ordered):
    fail("SavePreservingValidatedBackup generation-fence operations are not in fail-closed transaction order")

bind_token = "PersistencePathSafety.RequireExclusiveOpenStillBound(backupFence, backupPath, \"validated project backup\")"
if body.count(bind_token) < 2:
    fail("validated backup generation must be rebound after primary publication before success")

save_index = body.index("SaveCore(project, fullPath, SaveMode.ReplacePrimaryOnly, MaxProjectFileBytes);")
second_bind = body.find(bind_token, body.find(bind_token) + len(bind_token))
post_load = body.index("var persistedBackup = Load(backupPath);")
if not (save_index < second_bind < post_load):
    fail("post-publication backup generation rebind must occur after SaveCore and before final backup Load")

print("PASS: QSDB preserved-backup validation is fenced to one canonical filesystem generation across primary publication.")
