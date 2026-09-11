from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    'Project Tools': ROOT / 'src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs',
    'Material Catalog': ROOT / 'src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs',
}

failures = []

def require(label, condition, message):
    if not condition:
        failures.append(f'{label}: {message}')

for label, path in FILES.items():
    text = path.read_text(encoding='utf-8')
    body_start = text.find('public void Show')
    body = text[body_start:] if body_start >= 0 else text
    needle = 'IsActiveDocumentGeneration(document, nativeDatabaseIdentity)'
    calls = []
    pos = 0
    while True:
        found = body.find(needle, pos)
        if found < 0:
            break
        calls.append(found)
        pos = found + len(needle)

    require(label, 'IsActiveDocumentGeneration' in text,
            'missing exact active-document/native-database generation helper')
    require(label, 'nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);' in body,
            'must capture exact native database identity at command admission')
    require(label, len(calls) >= 5,
            'must revalidate before destructive replacement, after close, before show, after show, and before UI publication')
    require(label, 'ReferenceEquals(activeDocument, document)' in text,
            'generation helper must require exact managed active-document wrapper')
    require(label, 'database.UnmanagedObject == nativeDatabaseIdentity' in text,
            'generation helper must require exact native database identity')

    show_pos = body.find('Application.ShowModelessWindow')
    publish_token = '_published = published' if label == 'Project Tools' else '_published = reserved'
    publish_pos = body.find(publish_token)
    if show_pos >= 0 and publish_pos >= 0:
        require(label, any(show_pos < p < publish_pos for p in calls),
                'must revalidate after host show before published ownership transfer')
    require(label, ('CloseWindowOnAffinityDrift' in text if label == 'Project Tools' else 'CloseCandidateOnAffinityDrift' in text),
            'affinity drift must close/retain an unpublished candidate rather than orphan it')
    require(label, 'if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))' in body,
            'success/error status publication must be gated to the same exact generation')

material = FILES['Material Catalog'].read_text(encoding='utf-8')
material_body = material[material.find('public void Show'):]
require('Material Catalog', 'ExistingProjectMutationContext.TryGet(document, out var project)' in material_body,
        'project snapshot admission must remain explicit')
require('Material Catalog', material_body.find('ExistingProjectMutationContext.TryGet(document, out var project)') < material_body.find('new MaterialCatalogWindow(document, project)'),
        'project snapshot must be admitted before constructing the document-bound window')
require('Material Catalog', 'IsActiveDocumentGeneration(document, nativeDatabaseIdentity)' in material_body,
        'project snapshot must be fenced by exact document generation through publication')

if failures:
    for failure in failures:
        print('FAIL', failure)
    print(f'FAILED: {len(failures)} Project Tools/Material Catalog publication-affinity requirement(s) missing')
    sys.exit(1)

print('PASS: Project Tools and Material Catalog modeless publication is fenced to the exact active document generation')
