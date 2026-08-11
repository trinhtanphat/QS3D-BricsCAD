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
):
    if token not in auto_host:
        errors.append(label + " missing token: " + token)

if ".RegenerateDirty(project)" in auto_host:
    errors.append("Auto Host must not regenerate unrelated dirty project elements")

canonical = auto_host.find("HasCanonicalHostLink(item.Opening, item.HostId)")
link = auto_host.find("service.LinkOpening(project, item.Opening.Id, item.HostId)")
subset = auto_host.find("RegenerateDirtySubset(project, elementIds)")
if min(canonical, link, subset) < 0 or not (canonical < link < subset):
    errors.append("Auto Host must canonical-check -> link -> scoped-regenerate")

for source, label in ((opening, "Door/Opening"),):
    for token in (
        ".RegenerateDirtySubset(project, new[] { createdElementId })",
        ".RegenerateDirtySubset(project, new[] { createdElementId, hostId })",
        "new AutoHostLinkCommands().AutoLinkHosts()",
        "rollback.Restore(project)",
    ):
        if token not in source:
            errors.append(label + " scoped-authoring token missing: " + token)
    if ".RegenerateDirty(project)" in source:
        errors.append(label + " Direct Draw must not regenerate unrelated dirty project elements")

    before = source.find(".RegenerateDirtySubset(project, new[] { createdElementId })")
    auto = source.find("new AutoHostLinkCommands().AutoLinkHosts()")
    after = source.find(".RegenerateDirtySubset(project, new[] { createdElementId, hostId })")
    if min(before, auto, after) < 0 or not (before < auto < after):
        errors.append(label + " must scoped-regenerate created element -> Auto Host -> scoped-regenerate opening+host")

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

print("PASS: Auto Host repairs non-canonical same-host links and scopes regeneration to changed openings/affected hosts; Door/Opening/Window Direct Draw keeps both regeneration passes inside the authored opening+host scope, leaving unrelated dirty elements untouched.")
