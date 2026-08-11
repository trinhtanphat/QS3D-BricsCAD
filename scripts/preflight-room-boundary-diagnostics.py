#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Geometry" / "RoomBoundaryDiagnostics.cs"
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomBoundaryCommands.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomBoundaryDiagnosticsSmoke.cs"
DOC = ROOT / "docs" / "ROOM-BOUNDARY-DIAGNOSTICS.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


core = read(CORE)
command = read(COMMAND)
smoke = read(SMOKE)
doc = read(DOC)
inbox = read(INBOX)

for token, label in [
    ("public enum RoomBoundaryDiagnosticReason", "diagnostic reason model"),
    ("NoInput", "no-input reason"),
    ("InsufficientSegments", "insufficient-segment reason"),
    ("NoClosedFace", "no-closed-face reason"),
    ("BelowMinimumArea", "minimum-area reason"),
    ("new RoomBoundaryEngine().Discover(segments, tolerance, 0d)", "canonical topology engine delegation"),
    ("Fingerprint(x.SourceIds)", "privacy-safe source provenance fingerprint"),
    ("AcceptedBoundaries", "accepted boundary handoff"),
]:
    require(core, token, label)

if core.count("new RoomBoundaryEngine().Discover") != 1:
    errors.append("RoomBoundaryDiagnosticService must delegate to RoomBoundaryEngine exactly once")
if "public IReadOnlyList<string> SourceIds" in core or "public string BoundaryKey" in core:
    errors.append("diagnostic presentation model must not expose raw source IDs or geometry boundary keys")

for token, label in [
    ("new RoomBoundaryDiagnosticService().Analyze(segments, tolerance, minimumArea)", "QS3DROOMAUTO diagnostic analysis"),
    ("diagnostic.AcceptedBoundaries", "QS3DROOMAUTO accepted boundary handoff"),
    ("FormatRoomBoundaryDiagnostic(diagnostic)", "QS3DROOMAUTO reason formatter"),
    ("RoomBoundaryDiagnosticReason.NoInput", "NoInput command message"),
    ("RoomBoundaryDiagnosticReason.InsufficientSegments", "InsufficientSegments command message"),
    ("RoomBoundaryDiagnosticReason.NoClosedFace", "NoClosedFace command message"),
    ("RoomBoundaryDiagnosticReason.BelowMinimumArea", "BelowMinimumArea command message"),
]:
    require(command, token, label)

if "new RoomBoundaryEngine().Discover" in command:
    errors.append("QS3DROOMAUTO must consume RoomBoundaryDiagnosticService instead of running a second direct topology path")

analyze = command.find("new RoomBoundaryDiagnosticService().Analyze(segments, tolerance, minimumArea)")
bind = command.find("ExistingProjectMutationContext.Require(document, \"Room Auto\")")
create = command.find("ProjectContextCoordinator.GetOrCreate(document)")
if min(analyze, bind, create) < 0 or not (analyze < bind and analyze < create):
    errors.append("Room diagnostics/no-face exit must occur before canonical project bind or project creation")

for token, label in [
    ("ClassifiesNoInputAndInsufficientSegments", "no-input smoke"),
    ("ClassifiesOpenNetwork", "open-network smoke"),
    ("ExplainsMinimumAreaRejection", "minimum-area smoke"),
    ("AcceptedFaceProvenanceIsDeterministicAndPrivacySafe", "deterministic privacy smoke"),
]:
    require(smoke, token, label)

for token, label in [
    ("single topology engine", "single-engine documentation"),
    ("raw CAD handles", "privacy documentation"),
    ("LOCAL-010", "existing local performance/UX gate reference"),
]:
    require(doc, token, label)

require(inbox, "LOCAL-010 — large-model performance and UI matrix", "canonical local performance item")
require(inbox, "rooms", "existing Room performance scope")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Room Auto diagnostics reuse the canonical RoomBoundaryEngine once, classify no-input/open/minimum-area failures before project mutation, expose privacy-safe provenance only, and keep exact V25 UX/performance proof under LOCAL-010.")
