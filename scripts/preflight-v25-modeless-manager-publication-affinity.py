from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    'Family Manager': ROOT / 'src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs',
    'Level Manager': ROOT / 'src/QS3D.BricsCAD.V25/FloorLevelCommands.cs',
    'Zone Manager': ROOT / 'src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs',
}

failures = []

def require(label, condition, message):
    if not condition:
        failures.append(f'{label}: {message}')

for label, path in FILES.items():
    text = path.read_text(encoding='utf-8')
    helper = 'IsActiveDocumentGeneration'
    require(label, helper in text, 'missing exact active-document/native-database generation helper')
    if helper not in text:
        continue

    body_start = text.find('public void Show')
    body = text[body_start:] if body_start >= 0 else text
    close_positions = [p for token in ('CloseOwnerBeforeReplacement(pending', 'CloseOwnerBeforeReplacement(previous') if (p := body.find(token)) >= 0]
    show_pos = body.find('Application.ShowModelessWindow')
    publish_pos = body.find('_published = owner')
    calls = []
    pos = 0
    needle = 'IsActiveDocumentGeneration(document, nativeDatabaseIdentity)'
    while True:
        found = body.find(needle, pos)
        if found < 0:
            break
        calls.append(found)
        pos = found + 1

    require(label, len(calls) >= 3, 'must fence before destructive replacement, before ShowModelessWindow, and after ShowModelessWindow before publication')
    if close_positions and calls:
        require(label, calls[0] < min(close_positions), 'first generation fence must precede destructive close/replacement')
    if show_pos >= 0 and len(calls) >= 2:
        require(label, calls[1] < show_pos, 'second generation fence must immediately protect host publication')
    if show_pos >= 0 and publish_pos >= 0 and len(calls) >= 3:
        require(label, show_pos < calls[2] < publish_pos, 'post-publication fence must run before _published authority transfer')

    require(label, 'var nativeDatabaseIdentity = document.Database.UnmanagedObject;' in body,
            'must capture exact native database identity at command admission')
    require(label, 'CloseCandidateOnAffinityDrift(candidate);' in body,
            'must close unpublished candidate instead of retaining a stale modeless generation')

if failures:
    for failure in failures:
        print('FAIL', failure)
    print(f'FAILED: {len(failures)} modeless manager publication-affinity requirement(s) missing')
    sys.exit(1)

print('PASS: V25 manager modeless publication is fenced to the exact active document generation')
