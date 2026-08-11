#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUTO_HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "AutoHostLinkCommands.cs"
OPENING = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawOpeningCommands.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawWindowCommands.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


auto_host = read(AUTO_HOST)
opening = read(OPENING)
window = read(WINDOW)

for token, label in (
    ("HasCanonicalHostLink(item.Opening, item.HostId)", "canonical same-host check"),
    ("regenerationTargets.Add(item.Opening.Id)", "opening regeneration target"),
    ("regenerationTargets.Add(item.HostId)", "new-host regeneration target"),
    ("project.FindElement(previousHostId) != null", "live previous-host guard"),
    ("regenerationTargets.Add(previousHostId)", "previous-host regeneration target"),
    ("RegenerateDirtySubset(project, elementIds)", "targeted Auto Host regeneration"),
    ("ProjectStateSnapshot.Capture(project)", "Auto Host semantic snapshot"),
    ("rollback.Restore(project)", "Auto Host semantic rollback"),
    ('opening.SetProperty("AutoHostGapM", gap);', "Auto Host gap canonical property mutation"),
    ('opening.SetProperty("AutoHostMatched", "true");', "Auto Host matched canonical property mutation"),
    ("ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)", "single-opening canonical project lookup"),
    ("ReferenceEquals(currentProject, project)", "single-opening exact project identity guard"),
):
    if token not in auto_host:
        errors.append(label + " missing token: " + token)

for forbidden, label in (
    (".RegenerateDirty(project)", "Auto Host must not regenerate unrelated dirty project elements"),
    ('opening.Properties["AutoHostGapM"]', "Auto Host gap metadata must not bypass ProjectElement.SetProperty"),
    ('opening.Properties["AutoHostMatched"]', "Auto Host matched metadata must not bypass ProjectElement.SetProperty"),
):
    if forbidden in auto_host:
        errors.append(label)

canonical = auto_host.find("HasCanonicalHostLink(item.Opening, item.HostId)")
link = auto_host.find("service.LinkOpening(project, item.Opening.Id, item.HostId)")
subset = auto_host.find("RegenerateDirtySubset(project, elementIds)")
if min(canonical, link, subset) < 0 or not (canonical < link < subset):
    errors.append("Auto Host must canonical-check -> link -> scoped-regenerate")

for token in (
    ".RegenerateDirtySubset(project, new[] { createdElementId })",
    "AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)",
    ".RegenerateDirtySubset(project, new[] { createdElementId, hostId })",
    "rollback.Restore(project)",
):
    if token not in opening:
        errors.append("Door/Opening exact-project scoped-authoring token missing: " + token)
if ".RegenerateDirty(project)" in opening or "new AutoHostLinkCommands().AutoLinkHosts()" in opening:
    errors.append("Door/Opening Direct Draw must use exact-project Auto Host and must not regenerate unrelated dirty project elements")

opening_before = opening.find(".RegenerateDirtySubset(project, new[] { createdElementId })")
opening_auto = opening.find("AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)")
opening_after = opening.find(".RegenerateDirtySubset(project, new[] { createdElementId, hostId })")
if min(opening_before, opening_auto, opening_after) < 0 or not (opening_before < opening_auto < opening_after):
    errors.append("Door/Opening must scoped-regenerate created element -> exact-project Auto Host -> scoped-regenerate opening+host")

for token in (
    "regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id })",
    "AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id)",
    "regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id })",
    "rollback.Restore(project)",
):
    if token not in window:
        errors.append("Window exact-project scoped-authoring token missing: " + token)
if ".RegenerateDirty(project)" in window or "new AutoHostLinkCommands().AutoLinkHosts()" in window:
    errors.append("Window Direct Draw must use exact-project Auto Host and must not regenerate unrelated dirty project elements")
window_before = window.find("regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id })")
window_auto = window.find("AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id)")
window_after = window.find("regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id })")
if min(window_before, window_auto, window_after) < 0 or not (window_before < window_auto < window_after):
    errors.append("Window must scoped-regenerate opening -> exact-project Auto Host -> scoped-regenerate opening+host")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Auto Host uses canonical metadata mutation and exact-project single-opening authoring; Door/Opening/Window Direct Draw keeps regeneration inside the authored opening+host scope, leaving unrelated dirty elements untouched.")
