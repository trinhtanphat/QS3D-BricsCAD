#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "p0": ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs",
    "p1": ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs",
    "opening": ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs",
    "reference": ROOT / "src/QS3D.BricsCAD.V25/DirectDrawReferenceWallCommands.cs",
}
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Direct Draw lifecycle source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing contract token: " + token)


def require_count(text, token, count, label):
    actual = text.count(token)
    if actual != count:
        errors.append(label + " expected %d occurrence(s) of %s, got %d" % (count, token, actual))


sources = {name: read(path) for name, path in FILES.items()}
get_or_create = "ProjectContextCoordinator.GetOrCreate(document)"
preview_capture = "DirectDrawProjectPreviewContext.Capture(document)"
preview_resolve = "projectPreview.ResolveForMutation(document, operation)"

commands = {
    "p0": ("QS3DDRAWWALL", "QS3DDRAWBEAM", "QS3DDRAWSLAB", "QS3DDRAWCOLUMN"),
    "p1": ("QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER", "QS3DDRAWSTRUCTWALL", "QS3DDRAWFOUNDATION"),
    "opening": ("QS3DDRAWDOOR", "QS3DDRAWOPENING"),
    "reference": ("QS3DDRAWWALLREF",),
}

for name, names in commands.items():
    text = sources[name]
    for command in names:
        require(text, command, name)

# P0/P1 entrypoints may read defaults from an existing project, but must not
# create/cache a project until their private execution helper begins.
for name, helper in (("p0", "private static void ExecuteDirect"), ("p1", "private static void Execute(")):
    text = sources[name]
    require(text, preview_capture, name)
    require(text, preview_resolve, name)
    require_count(text, get_or_create, 1, name)
    helper_index = text.find(helper)
    create_index = text.find(get_or_create)
    if helper_index < 0 or create_index < 0 or create_index < helper_index:
        errors.append(name + " must defer GetOrCreate until the private execution helper")
    if helper_index >= 0 and get_or_create in text[:helper_index]:
        errors.append(name + " command entrypoints must not create/cache a project before parameter prompts complete")

# Opening resolves a captured preview in its executor; it must not bypass prompt
# cancellation or identity freshness with command-local GetOrCreate.
opening = sources["opening"]
require(opening, "DirectDrawProjectPreviewContext.Capture(document)", "opening")
require(opening, "projectPreview.ResolveForMutation(document, operation)", "opening")
if get_or_create in opening:
    errors.append("opening must not bypass preview project freshness with direct GetOrCreate")
opening_resolve = opening.find("projectPreview.ResolveForMutation(document, operation)")
opening_snapshot = opening.find("ProjectStateSnapshot.Capture(project)")
if min(opening_resolve, opening_snapshot) < 0 or opening_resolve > opening_snapshot:
    errors.append("opening must resolve the preview project before semantic mutation")

# Reference-wall authoring resolves the captured preview after every numeric
# prompt and the execute-boundary guard.
# Use prompt labels rather than whitespace-sensitive whole call expressions.
reference = sources["reference"]
require(reference, "DirectDrawProjectPreviewContext.Capture(document)", "reference")
require(reference, "projectPreview.ResolveForMutation(document, operation)", "reference")
if get_or_create in reference:
    errors.append("reference must not bypass preview project freshness with direct GetOrCreate")
create_index = reference.find("projectPreview.ResolveForMutation(document, operation)")
boundary_index = reference.find('EnsureActive(document, operation + " / execute boundary")')
prompt_labels = (
    '"Chiều dài Tường (m)"',
    '"Bề dày Tường (m)"',
    '"Chiều cao Tường (m)"',
    '"Offset đáy Tường so với Z tham chiếu (m)"',
)
for token in prompt_labels:
    index = reference.find(token)
    if index < 0:
        errors.append("reference missing parameter prompt label: " + token)
    elif create_index >= 0 and index > create_index:
        errors.append("reference must not resolve a mutation project before prompt completes: " + token)
if create_index < 0 or boundary_index < 0 or create_index < boundary_index:
    errors.append("reference project resolution must occur only after the explicit execute-boundary active-DWG guard")

# Preserve clean-DWG fallback defaults when no project exists; read-only lookup must
# not turn a cancel into project creation merely to obtain Family defaults.
for name, tokens in {
    "p0": ("0.2d", "3.6d", "0.3d", "0.5d", "0.12d", "0.4d"),
    "p1": ("0.012d", "0.2d", "3.6d", "0.5d"),
    "opening": ("2.2d", "0.01d"),
    "reference": ("0.2d", "3.6d"),
}.items():
    for token in tokens:
        require(sources[name], token, name + " fallback")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Direct Draw captures non-creating Family defaults, resolves the guarded project preview only after prompts, and keeps fallback GetOrCreate inside the private execution helper.")
