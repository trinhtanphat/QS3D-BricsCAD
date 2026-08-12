from pathlib import Path

FILES = {
    "src/QS3D.BricsCAD.V25/BqNativeTableCommands.cs": ("BqNativeTableBuilder.Build", "RegenerateSemantic(project)"),
    "src/QS3D.BricsCAD.V25/BbsNativeTableCommands.cs": ("BbsNativeTableBuilder.Build", "RegenerateSemantic(project)"),
    "src/QS3D.BricsCAD.V25/DoorOpeningNativeTableCommands.cs": ("DoorOpeningNativeTableBuilder.Build", None),
    "src/QS3D.BricsCAD.V25/MaterialUsageNativeTableCommands.cs": ("MaterialUsageNativeTableBuilder.Build", None),
    "src/QS3D.BricsCAD.V25/RoomFinishNativeTableCommands.cs": ("RoomFinishNativeTableBuilder.Build", None),
    "src/QS3D.BricsCAD.V25/SemanticElementTableCommands.cs": ("SemanticElementTableBuilder.Build", None),
}

for filename, (builder_token, regen_token) in FILES.items():
    text = Path(filename).read_text(encoding="utf-8")
    marker = text.find("GetPoint(")
    if marker < 0:
        raise SystemExit(f"FAIL {filename}: placement GetPoint not found")

    build_end = text.find("[CommandMethod", marker + 1)
    body = text[0:build_end if build_end >= 0 else len(text)]

    tokens = [
        "TryGetReadOnly(document, out var previewProject)",
        "var expectedProjectId = previewProject.ProjectId;",
        "var expectedChangeVersion = previewProject.ChangeVersion;",
        "GetPoint(",
        "RequireExistingProject(document",
        "project.ChangeVersion != expectedChangeVersion",
    ]
    positions = []
    for token in tokens:
        pos = body.find(token)
        if pos < 0:
            raise SystemExit(f"FAIL {filename}: missing {token}")
        positions.append(pos)
    if positions != sorted(positions):
        raise SystemExit(f"FAIL {filename}: freshness ordering regressed")

    builder_pos = body.find(builder_token)
    if builder_pos < 0 or positions[-1] >= builder_pos:
        raise SystemExit(f"FAIL {filename}: freshness guard must precede native build")

    if regen_token:
        regen_pos = body.find(regen_token)
        if regen_pos < 0 or positions[-1] >= regen_pos:
            raise SystemExit(f"FAIL {filename}: freshness guard must precede semantic regeneration")

    predicate_start = body.rfind("if (", 0, positions[-1] + 1)
    predicate = body[predicate_start:positions[-1] + len("project.ChangeVersion != expectedChangeVersion")]
    if "expectedProjectId" not in predicate or "expectedChangeVersion" not in predicate:
        raise SystemExit(f"FAIL {filename}: ProjectId + ChangeVersion must be checked together")

print("PASS: all six fixed native Table placement commands fail closed on ProjectId/ChangeVersion drift before mutation")
