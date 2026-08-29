#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CoordinationIncrementalCommands.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
text = SOURCE.read_text(encoding="utf-8")
v26 = V26.read_text(encoding="utf-8")
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


# Changed-only exact-clash cache validity depends on the live revision changing when
# native Solid3d geometry changes. AABB alone is insufficient: distinct solids can
# preserve the same handle/layer/bounds. EntitySnapshotReader already obtains these
# deterministic finite metrics from the same live Solid3d, so the revision must use them.
for token in (
    "entitySnapshot.SurfaceAreaDrawingUnitsSquared",
    "entitySnapshot.VolumeDrawingUnitsCubed",
    "SurfaceAreaDrawingUnitsSquared",
    "VolumeDrawingUnitsCubed",
):
    require(token in text, "changed-only live revision is missing geometry metric evidence: " + token)

require("QS3D_COORD_LIVE_V2" in text,
        "coordination live revision schema must be versioned when its geometry evidence changes")
require("component.AppendTo(text);" in text,
        "component evidence must remain part of deterministic coordination revision hashing")
require("OrderBy(item => item.Handle, StringComparer.Ordinal)" in text,
        "component revision ordering must remain deterministic by canonical handle")
require("ToString(\"R\", CultureInfo.InvariantCulture)" in text,
        "geometry metric serialization must remain round-trip and culture invariant")

# V26 intentionally compiles the V25 adapter source tree; do not create a drifting second implementation.
require("..\\QS3D.BricsCAD.V25\\**\\*.cs" in v26,
        "V26 no longer consumes the shared V25 adapter source tree")

if errors:
    print("Coordination incremental geometry revision preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Coordination changed-only revision includes same-AABB Solid3d geometry metrics with V25/V26 parity")
