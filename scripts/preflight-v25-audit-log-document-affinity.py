#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/AuditLogWindow.xaml.cs"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + ": missing " + token)


def forbid(text, token, label):
    if token in text:
        errors.append(label + ": forbidden " + token)


if not SOURCE.is_file():
    print("ERROR: missing AuditLogWindow source")
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")

# A native Database pointer is not a durable document identity: BricsCAD can reuse the address after
# teardown. Audit Log must retain managed/semantic affinity and use the pointer only as a wrapper-drift
# candidate filter, never as sufficient authorization to project another document's audit rows.
for token, label in (
    ("WeakReference<Document>", "audit log must retain weak managed lifecycle affinity"),
    ("_boundProjectId", "audit log must capture immutable project affinity"),
    ("_boundDrawingFingerprint", "audit log must capture immutable drawing affinity"),
    ("ReferenceEquals(candidate, lifecycleDocument)", "exact managed wrapper must be preferred"),
    ("MatchesBoundProjectAffinity(candidate)", "wrapper drift must prove semantic/drawing affinity"),
    ("ProjectContextCoordinator.TryGetReadOnly", "affinity validation must remain read-only"),
):
    require(source, token, label)

resolver_start = source.find("private bool TryResolveBoundDocument")
resolver_end = source.find("private static IntPtr GetNativeDatabaseIdentity", resolver_start + 1)
resolver = source[resolver_start:resolver_end] if resolver_start >= 0 and resolver_end > resolver_start else ""
if not resolver:
    errors.append("TryResolveBoundDocument block not found")
else:
    managed_match = resolver.find("ReferenceEquals(candidate, lifecycleDocument)")
    pointer_match = resolver.find("database.UnmanagedObject != _nativeDatabaseIdentity")
    affinity_match = resolver.find("MatchesBoundProjectAffinity(candidate)")
    if managed_match < 0:
        errors.append("resolver must test exact managed lifecycle wrapper")
    if pointer_match < 0:
        errors.append("resolver must retain native pointer only as wrapper-drift candidate filter")
    if affinity_match < 0:
        errors.append("resolver must require semantic affinity after native pointer candidate match")
    if pointer_match >= 0 and affinity_match >= 0 and pointer_match >= affinity_match:
        errors.append("semantic affinity must be checked only after native pointer candidate filtering")

# Regression: raw pointer equality alone was previously enough to accept a candidate.
legacy = "if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;\n                        document = candidate;\n                        return true;"
forbid(source, legacy, "raw native pointer equality must not authorize audit projection")

# Audit Log is a reader. Affinity checks must never create or mutate project state.
for token, label in (
    ("GetOrCreate(", "audit affinity validation must not create project state"),
    ("Save(", "audit affinity validation must not persist project state"),
):
    forbid(source, token, label)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: Audit Log uses managed plus semantic/drawing affinity; native pointer reuse alone cannot rebind projection.")
