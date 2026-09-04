#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/DocumentBoundWindowLifetime.cs"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + ": missing " + token)


def forbid(text, token, label):
    if token in text:
        errors.append(label + ": forbidden " + token)


if not SOURCE.is_file():
    print("ERROR: missing DocumentBoundWindowLifetime source")
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")

# Modeless interaction affinity is document-scoped, not merely project-scoped. Native database
# addresses can be reused, so pointer equality may filter wrapper-drift candidates but can never
# authorize a different drawing by itself.
for token, label in (
    ("_drawingFingerprint", "registration must capture immutable drawing affinity"),
    ("ReferenceEquals(candidate, _lifecycleDocument)", "resolver must prefer the original managed document wrapper"),
    ("MatchesBoundDocumentAffinity(candidate)", "wrapper drift must prove semantic drawing affinity"),
):
    require(source, token, label)

resolver_start = source.find("private bool TryResolveLiveDocument")
resolver_end = source.find("private bool HasAnotherLiveDocument", resolver_start + 1)
resolver = source[resolver_start:resolver_end] if resolver_start >= 0 and resolver_end > resolver_start else ""
if not resolver:
    errors.append("TryResolveLiveDocument block not found")
else:
    managed_match = resolver.find("ReferenceEquals(candidate, _lifecycleDocument)")
    pointer_match = resolver.find("MatchesNativeDatabase(candidate)")
    affinity_match = resolver.find("MatchesBoundDocumentAffinity(candidate)")
    if managed_match < 0:
        errors.append("resolver must test exact managed wrapper identity")
    if pointer_match < 0:
        errors.append("resolver must retain native pointer only as wrapper-drift candidate filter")
    if affinity_match < 0:
        errors.append("resolver must require drawing affinity for wrapper drift")
    if pointer_match >= 0 and affinity_match >= 0 and pointer_match >= affinity_match:
        errors.append("drawing affinity must be checked after native pointer candidate filtering")

# Pin the actual semantic proof. ProjectId alone is insufficient because multiple drawings can belong
# to the same project and a recycled native address must not silently rebind a stale window.
affinity_start = source.find("private bool MatchesBoundDocumentAffinity")
affinity_end = source.find("private bool HasAnotherLiveDocument", affinity_start + 1)
affinity = source[affinity_start:affinity_end] if affinity_start >= 0 and affinity_end > affinity_start else ""
if not affinity:
    errors.append("MatchesBoundDocumentAffinity block not found")
else:
    for token, label in (
        ("string.IsNullOrWhiteSpace(_projectId)", "missing project token must fail closed for wrapper drift"),
        ("string.IsNullOrWhiteSpace(_drawingFingerprint)", "missing drawing token must fail closed for wrapper drift"),
        ("ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)", "wrapper-drift proof must remain read-only"),
        ("project.ProjectId", "candidate project id must be compared"),
        ("project.DrawingFingerprint", "candidate drawing fingerprint must be compared"),
        ("_drawingFingerprint", "original drawing fingerprint must participate in comparison"),
        ("StringComparison.OrdinalIgnoreCase", "affinity comparison must be deterministic"),
    ):
        require(affinity, token, label)

legacy = "if (!MatchesNativeDatabase(candidate)) continue;\n                        document = candidate;\n                        return true;"
forbid(source, legacy, "native pointer equality alone must not authorize modeless interaction")

# Affinity validation is read-only.
for token, label in (
    ("GetOrCreate(", "modeless affinity validation must not create project state"),
    ("Save(", "modeless affinity validation must not persist project state"),
):
    forbid(source, token, label)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: modeless window lifetime is bound to managed + project + drawing affinity; native pointer reuse cannot rebind interaction.")
