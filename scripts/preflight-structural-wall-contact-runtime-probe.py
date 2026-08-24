#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallContactProbeCommands.cs"
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/StructuralWallConcreteContactService.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall contact runtime-probe preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


probe = PROBE.read_text(encoding="utf-8")
service = SERVICE.read_text(encoding="utf-8")

for token in (
    '[CommandMethod("QS3DWALLCONTACTPROBE", CommandFlags.UsePickSet)]',
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "SemanticReferenceHandles.MatchesSelection(element, handles)",
    "StructuralWallConcreteContactService.TryMeasureM2(",
    '"target_solids="',
    '"candidates="',
    '"face_seeds="',
    '"volume_cuts="',
    '"contact_cuts="',
    '"failed_native="',
    '"gross_m2="',
    '"residual_m2="',
    '"deduction_m2="',
):
    if token not in probe:
        fail("missing sanitized probe contract: " + token)

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    "TransactionManager.StartTransaction",
    "ForWrite",
    "ProjectId",
    "SourceHandles=",
):
    if forbidden in probe:
        fail("read-only/sanitized probe contains forbidden mutation/private token: " + forbidden)

for token in (
    "public int CandidateSolidCount { get; internal set; }",
    "public int VerticalFaceSeedCount { get; internal set; }",
    "public int PositiveVolumeCutCount { get; internal set; }",
    "public int FailedNativeCutCount { get; internal set; }",
    "public double ResidualVerticalAreaM2 { get; internal set; }",
):
    if token not in service:
        fail("measurement diagnostics are incomplete: " + token)

print("PASS: QS3DWALLCONTACTPROBE is read-only, sanitized, and exposes candidate/face/cut/residual stages for exact-SHA V25 rerun")
