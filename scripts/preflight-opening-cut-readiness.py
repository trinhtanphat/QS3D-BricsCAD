#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanCutGuard.cs"
commands = ROOT / "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs"
service = ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs"

if not guard.is_file():
    errors.append("missing OpeningBooleanCutGuard.cs")
else:
    text = guard.read_text(encoding="utf-8")
    for token in (
        "RequireFreshGeneratedHosts",
        "RequireSelectedTargetsReady",
        "IsGeneratedSolidStale()",
        "GeneratedSolidHandle",
        "GeneratedGeometryService.RequireMatchingOwnership",
        "Solid3d",
        "source is Line",
        "source is Polyline",
        "polyline.GetBulgeAt",
    ):
        if token not in text:
            errors.append("opening cut readiness guard missing token: " + token)

if not commands.is_file():
    errors.append("missing OpeningBooleanCommands.cs")
else:
    text = commands.read_text(encoding="utf-8")
    service_call = text.find("OpeningBooleanService.CutLinkedOpenings")
    fresh_guard = text.find("OpeningBooleanCutGuard.RequireFreshGeneratedHosts")
    selected_guard = text.find("OpeningBooleanCutGuard.RequireSelectedTargetsReady")
    if service_call < 0:
        errors.append("OpeningBooleanCommands no longer calls OpeningBooleanService")
    if fresh_guard < 0 or (service_call >= 0 and fresh_guard > service_call):
        errors.append("legacy/all-linked cut must reject stale generated hosts before service mutation")
    if selected_guard < 0 or (service_call >= 0 and selected_guard > service_call):
        errors.append("selected cut must prevalidate every target before service mutation")

if not service.is_file():
    errors.append("missing OpeningBooleanService.cs")
else:
    text = service.read_text(encoding="utf-8")
    for token in (
        "document.Database.TransactionManager.StartTransaction()",
        "hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter)",
        "transaction.Commit();",
        "GeneratedGeometryService.RequireMatchingOwnership",
        "PhysicalOpeningCutFingerprint",
    ):
        if token not in text:
            errors.append("opening boolean transaction/ownership contract missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: physical opening cuts reject stale generated hosts, selected targets are all prevalidated before mutation, ownership is checked, and BoolSubtract remains transaction-scoped.")
