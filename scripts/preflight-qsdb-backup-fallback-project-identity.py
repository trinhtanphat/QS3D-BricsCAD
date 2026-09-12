#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsdbBackupFallbackProjectIdentitySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsdbBackupFallbackProjectIdentitySmokeRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

method_start = source.index("public ProjectLoadResult LoadWithBackupFallback")
method_end = source.index("private static XDocument Serialize", method_start)
method = source[method_start:method_end]

required_source = [
    "var document = LoadDocument(fullPath);",
    "primaryProjectId = TryGetCanonicalProjectIdentity(document);",
    "LoadProjectDocument(document)",
    "!string.Equals(project.ProjectId, primaryProjectId, StringComparison.Ordinal)",
    'throw new InvalidDataException("Validated QSDB backup project identity does not match the failed primary project identity.", primary);',
]
for snippet in required_source:
    if snippet not in method:
        raise SystemExit(f"missing QSDB backup project-identity contract: {snippet}")

if method.index("var document = LoadDocument(fullPath);") > method.index("LoadProjectDocument(document)"):
    raise SystemExit("primary identity is not derived from the admitted primary document generation")
if method.index("!string.Equals(project.ProjectId, primaryProjectId, StringComparison.Ordinal)") > method.index("return new ProjectLoadResult(project, backupPath, true, BackupRecoveryReason)"):
    raise SystemExit("backup project identity is checked only after successful recovery publication")

required_smoke = [
    "CrossProjectBackupIsRejectedWhenPrimaryIdentityIsRecoverable();",
    'new ProjectState("primary-a", "Primary A")',
    'new ProjectState("backup-b", "Backup B")',
    "SameProjectBackupStillRecovers();",
    "MalformedPrimaryWithoutIdentityStillRecovers();",
]
for snippet in required_smoke:
    if snippet not in smoke:
        raise SystemExit(f"missing deterministic QSDB backup project-identity regression: {snippet}")

if "[ModuleInitializer]" not in registration or "QsdbBackupFallbackProjectIdentitySmoke.Run()" not in registration:
    raise SystemExit("QSDB backup project-identity smoke is not auto-registered")

print("PASS QSDB backup fallback is fenced by recoverable primary project identity")
