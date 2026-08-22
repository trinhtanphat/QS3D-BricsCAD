#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs"
regen = ROOT / "src/QS3D.Core/Services/SemanticRegenerators.cs"
errors = []

if not path.is_file():
    errors.append("missing WallJunctionSnapCommands.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "var updatedLengthsM = BuildUpdatedSourceLengths(plan, touchedHandles, touchedOwners);",
        "BuildUpdatedSourceLengths(SnapPlan plan",
        "ResolvePlannedEndpoint",
        'element.Properties["LengthM"] = updatedLengthsM[element.Id].ToString("R", CultureInfo.InvariantCulture);',
        "semantic owner có đúng một authoritative source handle",
        "Wall Snap would create a zero/non-finite source segment",
    )
    for token in required:
        if token not in text:
            errors.append("Wall Snap source-metric contract missing: " + token)

    metric_plan = text.find("var updatedLengthsM = BuildUpdatedSourceLengths")
    transaction = text.find("using (document.LockDocument())")
    commit = text.find("transaction.Commit();")
    assign = text.find('element.Properties["LengthM"] = updatedLengthsM')
    if min(metric_plan, transaction, commit, assign) >= 0 and not (metric_plan < transaction < commit < assign):
        errors.append("Wall Snap must validate/precompute source lengths before CAD mutation and persist LengthM only after transaction commit")

if not regen.is_file():
    errors.append("missing SemanticRegenerators.cs")
else:
    text = regen.read_text(encoding="utf-8")
    if 'SemanticNumber.Get(element, "LengthM")' not in text:
        errors.append("WallRegenerator contract changed: preflight expects quantities to consume semantic LengthM")

print("QS3D Wall Snap source metrics preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: reviewed Wall Snap plans precompute authoritative post-snap source length and synchronize semantic LengthM before later regeneration/BQ.")
