#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "DrawingUnitWorkflow.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


def slice_method(source, start_token, end_token):
    start = source.find(start_token)
    end = source.find(end_token, start)
    if start < 0 or end < 0:
        errors.append("cannot isolate method: " + start_token)
        return ""
    return source[start:end]


source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
inbox = INBOX.read_text(encoding="utf-8") if INBOX.is_file() else ""
if not source:
    errors.append("missing DrawingUnitWorkflow.cs")
if not inbox:
    errors.append("missing docs/LOCAL-AGENT-INBOX.md")

prompt = slice_method(source, "private static bool PromptAndPersist(", "private static void PersistLegacyBindingIfNeeded(")
legacy = source[source.find("private static void PersistLegacyBindingIfNeeded(") :]

# QS3DUNITS is an explicit configuration/bootstrap operation and may create/save project state.
require(prompt, "ProjectContextCoordinator.GetOrCreate(document)", "explicit unit bootstrap")
require(prompt, "ProjectContextCoordinator.Save(document)", "explicit unit persistence")

for token, label in [
    ("ProjectContextCoordinator.TryGetReadOnly(document, out var observedProject)", "legacy non-creating probe"),
    ("if (observedProject.Elements.Count == 0) return;", "empty-project no-op"),
    ('ExistingProjectMutationContext.Require(document, "Legacy drawing-unit binding")', "legacy canonical mutation boundary"),
    ("ProjectStateSnapshot.Capture(project)", "legacy rollback snapshot"),
    ("ProjectContextCoordinator.Save(document)", "legacy migration persistence"),
]:
    require(legacy, token, label)
if "ProjectContextCoordinator.GetOrCreate(document)" in legacy:
    errors.append("automatic legacy unit binding must not create/cache a replacement project")

probe = legacy.find("ProjectContextCoordinator.TryGetReadOnly(document, out var observedProject)")
empty = legacy.find("if (observedProject.Elements.Count == 0) return;")
bind = legacy.find('ExistingProjectMutationContext.Require(document, "Legacy drawing-unit binding")')
snapshot = legacy.find("ProjectStateSnapshot.Capture(project)")
if min(probe, empty, bind, snapshot) < 0 or not probe < empty < bind < snapshot:
    errors.append("legacy unit binding must probe non-creating state and bind canonical project before mutation")

for token, label in [
    ("LOCAL-001 — exact V25 build/load baseline", "canonical local baseline item"),
    ("legacy unit binding", "local legacy unit scenario"),
    ("QS3DUNITS", "explicit unit bootstrap scenario"),
    ("no replacement project", "local no-replacement evidence"),
]:
    require(inbox, token, label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: automatic legacy unit binding is non-creating and canonical-before-write, while explicit QS3DUNITS retains intentional project bootstrap/persistence; LOCAL-001 owns native V25 proof.")
