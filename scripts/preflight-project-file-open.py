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
        'ProjectContextCoordinator.Reload(document);',
        'var rootedCandidate = Path.GetFullPath(stored);',
        'if (File.Exists(rootedCandidate)) return rootedCandidate;',
        'var sameStem = Path.ChangeExtension(projectPath, ".dwg");',
        'if (File.Exists(sameStem)) return Path.GetFullPath(sameStem);',
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

    resolve_block = source.split('private static string ResolveDrawingPath(string projectPath, ProjectState project)', 1)[1].split('private static Document RequireActiveDocument()', 1)[0]
    rooted = resolve_block.find('var rootedCandidate = Path.GetFullPath(stored);')
    rooted_exists = resolve_block.find('if (File.Exists(rootedCandidate)) return rootedCandidate;')
    same_stem = resolve_block.find('var sameStem = Path.ChangeExtension(projectPath, ".dwg");')
    if rooted < 0 or rooted_exists < 0 or same_stem < 0 or not (rooted < rooted_exists < same_stem):
        raise SystemExit("FAIL: rooted stored DWG must be existence-checked before the same-stem fallback")
    forbid(resolve_block, 'if (Path.IsPathRooted(stored)) return Path.GetFullPath(stored);', rel + '::ResolveDrawingPath')

    print("PASS: project open routes DWG before QSDB XML parsing, translates malformed project errors, and falls back from a stale rooted stored DWG path to the co-located same-stem drawing without bypassing canonical reload identity validation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
