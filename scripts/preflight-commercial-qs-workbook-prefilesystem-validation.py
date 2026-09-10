#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CommercialQsWorkbook.cs"
TEXT = SOURCE.read_text(encoding="utf-8")

required_existing = [
    "MaxWorksheetEntryBytes",
    "MaxAggregateXmlBytes",
    "MaxArchiveBytes",
    "StrictUtf8",
    "Directory.CreateDirectory(directory)",
    "AtomicFileCommit.CreateTempPath(fullPath)",
    "MaxCellCharacters",
]
for token in required_existing:
    if token not in TEXT:
        raise SystemExit(f"Commercial QS pre-filesystem guard: missing existing contract token: {token}")

# The hardened contract deliberately requires a full payload-validation phase before
# any path resolution, directory creation or temp-file ownership begins.  This is
# RED on the vulnerable implementation and becomes GREEN only when production owns
# that sequencing; the guard does not accept merely moving filesystem lines around.
required_hardening = [
    "ValidateWorkbookPayload",
    "ValidateStrictUtf8Cell",
    "ValidateWorkbookRows",
]
for token in required_hardening:
    if token not in TEXT:
        raise SystemExit(f"Commercial QS pre-filesystem guard RED: missing hardening token: {token}")

export_pos = TEXT.find("public static void Export")
validate_pos = TEXT.find("ValidateWorkbookPayload", export_pos)
full_path_pos = TEXT.find("Path.GetFullPath", export_pos)
dir_pos = TEXT.find("Directory.CreateDirectory", export_pos)
temp_pos = TEXT.find("AtomicFileCommit.CreateTempPath", export_pos)
if min(export_pos, validate_pos, full_path_pos, dir_pos, temp_pos) < 0:
    raise SystemExit("Commercial QS pre-filesystem guard: unable to locate export validation/filesystem sequence")
if not (validate_pos < full_path_pos < dir_pos < temp_pos):
    raise SystemExit("Commercial QS pre-filesystem guard RED: payload validation must precede all filesystem side effects")

# Defense in depth must remain in the write path even after the preflight validator
# is introduced; do not trade pre-filesystem fail-close for weaker publication.
append_row_pos = TEXT.find("AppendRow")
if append_row_pos < 0 or "MaxCellCharacters" not in TEXT[append_row_pos:]:
    raise SystemExit("Commercial QS pre-filesystem guard: write-path Excel cell bound was weakened")
if "new UTF8Encoding(false, true)" not in TEXT:
    raise SystemExit("Commercial QS pre-filesystem guard: strict UTF-8 encoder was weakened")

print("PASS Commercial QS workbook pre-filesystem payload validation guard")
