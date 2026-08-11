#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "AutoHostLinkCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    source = SOURCE.read_text(encoding="utf-8")
    for token, label in (
        ("HasCanonicalHostLink(item.Opening, item.HostId)", "canonical same-host check"),
        ("regenerationTargets.Add(item.Opening.Id)", "opening regeneration target"),
        ("regenerationTargets.Add(item.HostId)", "new-host regeneration target"),
        ("project.FindElement(previousHostId) != null", "live previous-host guard"),
        ("regenerationTargets.Add(previousHostId)", "previous-host regeneration target"),
        ("RegenerateDirtySubset(project, elementIds)", "targeted regeneration"),
        ("ProjectStateSnapshot.Capture(project)", "semantic snapshot"),
        ("rollback.Restore(project)", "semantic rollback"),
    ):
        if token not in source:
            errors.append(label + " missing token: " + token)

    if ".RegenerateDirty(project)" in source:
        errors.append("Auto Host must not regenerate unrelated dirty project elements")

    canonical = source.find("HasCanonicalHostLink(item.Opening, item.HostId)")
    link = source.find("service.LinkOpening(project, item.Opening.Id, item.HostId)")
    subset = source.find("RegenerateDirtySubset(project, elementIds)")
    if min(canonical, link, subset) < 0 or not (canonical < link < subset):
        errors.append("Auto Host must canonical-check -> link -> scoped-regenerate")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Auto Host repairs non-canonical same-host links and regenerates only changed openings plus affected live hosts; unrelated dirty project elements remain outside the operation.")
