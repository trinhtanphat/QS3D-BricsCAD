#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"


def require(text, needle, message):
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def forbid(text, needle, message):
    if needle in text:
        raise SystemExit("ERROR: " + message + " (found: " + needle + ")")


def main():
    text = SOURCE.read_text(encoding="utf-8")

    # The BricsCAD solid kernel must receive the live database curve; cloning Circle/Polyline
    # before CreateExtrudedSolid reproduced Failed CreateExtrudedSolid on V25.
    require(text, "solid.CreateExtrudedSolid(source, new Vector3d(0d, 0d, height), new SweepOptions());",
            "cad_extrude must use the database-resident source curve")
    forbid(text, "var sourceClone = source.Clone() as Curve;",
           "cad_extrude must not clone the source curve before kernel extrusion")

    # Boolean operations follow the proven host pattern: mutate the database-resident target and
    # feed a transient tool clone. HandOverTo of a transient target reproduced eInvalidInput.
    require(text, "target.BooleanOperation(operation, operandClone);",
            "Solid3d boolean must mutate the database-resident target")
    forbid(text, "targetClone.BooleanOperation(operation, operandClone);",
           "Solid3d boolean must not execute on a cloned target")
    forbid(text, "target.HandOverTo(targetClone, true, true);",
           "Solid3d boolean must not hand a transient target back into the database")

    # Save As must leave the generic CAD callback before native QSAVE is queued, then use the same
    # event-driven QSAVE/DBMOD completion fence as cad_save.
    require(text, 'if (string.Equals(tool, "cad_save_as", StringComparison.Ordinal)) return SaveAs(body);',
            "cad_save_as must dispatch outside the generic CAD context callback")
    require(text, "McpNativeCurrentDocumentSave.SaveCurrentDocument(",
            "cad_save_as must finish through native QSAVE/DBMOD verification")

    # Direct-model failures must be visible in the unified diagnostic stream, not only audit/repair.
    require(text, '"cad-mutation-failed"',
            "direct CAD mutation failures must emit unified failure diagnostics")

    print("PASS: MCP CAD direct residual regression contract")


if __name__ == "__main__":
    main()
