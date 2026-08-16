#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden stale contract: {needle}")


def main():
    rel = "src/QS3D.BricsCAD.V25/ProjectFileUiService.cs"
    source = read(rel)

    for needle in (
        'using System.Xml;',
        'BricsCAD Drawing (*.dwg)|*.dwg',
        'string.Equals(extension, ".dwg", StringComparison.OrdinalIgnoreCase)',
        'OpenDrawing(fullProjectPath);',
        'if (!IsProjectFileExtension(extension))',
        'Hãy chọn tệp .blt3d, .qsdb hoặc .dwg.',
        'var importedProject = LoadSelectedProject(store, fullProjectPath);',
        'catch (XmlException ex)',
        'Tệp không đúng định dạng QS3D hoặc đã bị hỏng.',
        'nếu đây là bản vẽ BricsCAD, hãy chọn tệp .dwg.',
        'private static void OpenDrawing(string drawingPath)',
        'document = Application.DocumentManager.Open(drawingPath, false);',
        'Application.DocumentManager.MdiActiveDocument = document;',
    ):
        require(source, needle, rel)

    open_block = source.split('internal static void OpenProject(string projectPath)', 1)[1].split('private static ProjectState LoadSelectedProject', 1)[0]
    dwg_route = open_block.find('string.Equals(extension, ".dwg", StringComparison.OrdinalIgnoreCase)')
    store_create = open_block.find('var store = new QsdbProjectStore();')
    if dwg_route < 0 or store_create < 0 or dwg_route >= store_create:
        raise SystemExit("FAIL: DWG routing must happen before QsdbProjectStore construction/XML loading")

    require(open_block, 'return;', rel + '::OpenProject DWG route')
    forbid(open_block, 'store.Load(fullProjectPath)', rel + '::OpenProject')
    forbid(source, 'Data at the root level is invalid', rel)

    print("PASS: project open routes DWG before QSDB XML parsing, keeps .blt3d/.qsdb loading explicit, and translates XML parse failures into actionable QS3D format guidance.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
