#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawOpeningCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    capture_id = "var createdElementId = createdElement.Id;"
    auto_host = "new AutoHostLinkCommands().AutoLinkHosts();"
    canonical_lookup = "string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase)"
    missing_guard = 'if (createdElement == null)\n                    throw new InvalidOperationException(label + " vừa tạo không còn tồn tại sau Auto Host; operation được rollback.");'
    host_read = 'createdElement.Properties.TryGetValue("HostWallId", out hostId)'
    erase = "EraseSource(document, sourceId)"
    restore = "rollback.Restore(project)"

    required = (capture_id, auto_host, canonical_lookup, host_read, erase, restore)
    for needle in required:
        if needle not in text:
            errors.append("missing Direct Draw opening host-state contract: " + needle)

    id_pos = text.find(capture_id)
    auto_pos = text.find(auto_host)
    lookup_pos = text.find(canonical_lookup)
    host_pos = text.find(host_read)
    if min(id_pos, auto_pos, lookup_pos, host_pos) >= 0 and not (id_pos < auto_pos < lookup_pos < host_pos):
        errors.append("created element Id must be captured before AutoHost and canonical state must be re-resolved before HostWallId is read")

    if auto_pos >= 0 and lookup_pos >= 0:
        post_auto_pre_lookup = text[auto_pos + len(auto_host):lookup_pos]
        if 'createdElement.Properties.TryGetValue("HostWallId"' in post_auto_pre_lookup:
            errors.append("stale pre-AutoHost createdElement must not be used to read HostWallId")
        if "createdElement.SetProperty(" in post_auto_pre_lookup:
            errors.append("stale pre-AutoHost createdElement must not be mutated after AutoHost before canonical re-resolution")

    if missing_guard not in text:
        errors.append("missing fail-closed guard when the canonical created element disappears after AutoHost rollback")

    erase_pos = text.find(erase)
    restore_pos = text.find(restore)
    if min(erase_pos, restore_pos) >= 0 and erase_pos > restore_pos:
        errors.append("outer Direct Draw rollback must erase the operation-owned CAD source before restoring project state")

if errors:
    print("QS3D Direct Draw opening canonical host-state preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Door/Opening Direct Draw captures stable semantic identity before AutoHost, re-resolves canonical project state after AutoHost, rejects missing/unhosted canonical elements, and preserves outer CAD/project rollback ordering.")
