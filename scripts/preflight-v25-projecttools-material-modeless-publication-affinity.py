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
    publish_token = '_published = reserved'
    publish_pos = body.find(publish_token)
    if show_pos >= 0 and publish_pos >= 0:
        require(label, any(show_pos < p < publish_pos for p in calls),
                'must revalidate after host show before published ownership transfer')
    require(label, ('ClosePendingOnAffinityDrift' in text if label == 'Project Tools' else 'CloseCandidateOnAffinityDrift' in text),
            'affinity drift must close/retain an unpublished candidate rather than orphan it')
    require(label, 'if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))' in body,
            'success/error status publication must be gated to the same exact generation')

project_tools = FILES['Project Tools'].read_text(encoding='utf-8')
project_tools_body = project_tools[project_tools.find('public void Show'):]
require('Project Tools', 'private static PublishedManager? _pending;' in project_tools,
        'Project Tools must retain pending ownership before host publication')
require('Project Tools', '_pending = reserved;' in project_tools_body,
        'Project Tools must reserve pending ownership before ShowModelessWindow')
require('Project Tools', 'if (!ReferenceEquals(_pending, reserved))' in project_tools_body,
        'Project Tools must verify exact pending ownership after host show')
require('Project Tools', '_pending = null;\n                _published = reserved;' in project_tools_body,
        'Project Tools must transfer pending ownership to published only after loaded exact-head admission')
require('Project Tools', 'ClosePendingAfterFailure(candidate, window);' in project_tools_body,
        'Project Tools failure cleanup must retain a loaded residue instead of forgetting it')
require('Project Tools', 'if (!window.IsLoaded && candidate != null && ReferenceEquals(_pending, candidate)) _pending = null;' in project_tools,
        'Project Tools must clear pending ownership only after terminal candidate close')
require('Project Tools', 'const string message = "QS3DPROJECTTOOLS không thể mở Project Tools an toàn; trạng thái hiện tại được giữ nguyên.";' in project_tools_body,
        'failure UI must use one constant redacted message')
require('Project Tools', 'ex.Message' not in project_tools_body,
        'failure UI must not expose exception details through palette/editor status')

material = FILES['Material Catalog'].read_text(encoding='utf-8')
material_body = material[material.find('public void Show'):]
require('Material Catalog', 'ExistingProjectMutationContext.TryGet(document, out var project)' in material_body,
        'project snapshot admission must remain explicit')
require('Material Catalog', material_body.find('ExistingProjectMutationContext.TryGet(document, out var project)') < material_body.find('new MaterialCatalogWindow(document, project)'),
        'project snapshot must be admitted before constructing the document-bound window')
require('Material Catalog', 'IsActiveDocumentGeneration(document, nativeDatabaseIdentity)' in material_body,
        'project snapshot must be fenced by exact document generation through publication')
require('Material Catalog', 'private static bool IsActiveProjectGeneration(Document document, IntPtr nativeDatabaseIdentity, ProjectState project)' in material,
        'missing exact cached-project/backing-store generation helper')
require('Material Catalog', 'ProjectContextCoordinator.TryGetCached(document, out var cachedProject)' in material,
        'project-generation helper must resolve the current canonical cached ProjectState')
require('Material Catalog', 'ReferenceEquals(cachedProject, project)' in material,
        'project-generation helper must require exact cached ProjectState reference identity')
require('Material Catalog', 'ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Material Catalog publication")' in material,
        'project-generation helper must require unchanged backing-store generation')
project_generation_calls = material_body.count('IsActiveProjectGeneration(document, nativeDatabaseIdentity, project)')
require('Material Catalog', project_generation_calls >= 4,
        'must revalidate exact project generation before construction/show, after host show, and before status/error publication')
show_pos = material_body.find('Application.ShowModelessWindow')
publish_pos = material_body.find('_published = reserved;', show_pos)
post_show_project = material_body.find('IsActiveProjectGeneration(document, nativeDatabaseIdentity, project)', show_pos)
require('Material Catalog', show_pos >= 0 and post_show_project > show_pos and publish_pos > post_show_project,
        'must revalidate exact project generation after host show before ownership publication')
require('Material Catalog', 'CloseCandidateAfterFailure(candidate, window);' in material_body,
        'failure cleanup must use residue-aware candidate close instead of forgetting pending ownership before native close')
require('Material Catalog', 'private static void CloseCandidateAfterFailure(PublishedManager? candidate, MaterialCatalogWindow? window)' in material,
        'missing residue-aware failure cleanup helper')
require('Material Catalog', 'if (!window.IsLoaded && candidate != null && ReferenceEquals(_pending, candidate)) _pending = null;' in material,
        'failed/vetoed close must retain pending ownership while the candidate remains loaded')
require('Material Catalog', 'if (candidate != null && ReferenceEquals(_pending, candidate))\n                    _pending = null;\n\n                if (window != null)' not in material_body,
        'catch path must not clear pending ownership before attempting candidate close')

if failures:
    for failure in failures:
        print('FAIL', failure)
    print(f'FAILED: {len(failures)} Project Tools/Material Catalog publication-affinity requirement(s) missing')
    sys.exit(1)

print('PASS: Project Tools and Material Catalog publication is fenced to the exact document generation; Material Catalog also retains the exact cached project/backing-store generation')
