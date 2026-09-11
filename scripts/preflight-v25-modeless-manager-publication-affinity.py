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
    pending_close = body.find('CloseOwnerBeforeReplacement(pending')
    published_close = body.find('CloseOwnerBeforeReplacement(previous')
    constructor_tokens = ('new FamilyManagerWindow(document)', 'new FloorLevelWindow(document)', 'new ZoneManagerWindow(document)')
    constructor_pos = min((p for token in constructor_tokens if (p := body.find(token)) >= 0), default=-1)
    pending_publish_pos = body.find('_pending = owner')
    show_pos = body.find('Application.ShowModelessWindow')
    publish_pos = body.find('_published = owner')
    needle = 'IsActiveDocumentGeneration(document, nativeDatabaseIdentity)'
    calls = []
    pos = 0
    while True:
        found = body.find(needle, pos)
        if found < 0:
            break
        calls.append(found)
        pos = found + len(needle)

    require(label, len(calls) >= 5,
            'must fence destructive replacement, post-close construction, host publication, post-show authority, and UI publication')
    close_positions = [p for p in (pending_close, published_close) if p >= 0]
    if close_positions:
        require(label, any(call < min(close_positions) for call in calls),
                'a generation fence must precede destructive close/replacement')
    if pending_close >= 0 and constructor_pos >= 0:
        require(label, any(pending_close < call < constructor_pos for call in calls),
                'must revalidate after pending-owner close before constructing a replacement window')
    if published_close >= 0 and constructor_pos >= 0:
        require(label, any(published_close < call < constructor_pos for call in calls),
                'must revalidate after published-owner close before constructing a replacement window')
    if pending_publish_pos >= 0 and show_pos >= 0:
        require(label, any(pending_publish_pos < call < show_pos for call in calls),
                'a fresh generation fence must run after candidate ownership and before ShowModelessWindow')
    if show_pos >= 0 and publish_pos >= 0:
        require(label, any(show_pos < call < publish_pos for call in calls),
                'a post-show generation fence must run before _published authority transfer')

    require(label, 'nativeDatabaseIdentity = document.Database.UnmanagedObject;' in body,
            'must capture exact native database identity at command admission')
    require(label, 'ReferenceEquals(activeDocument, document)' in text,
            'generation helper must require the exact managed active-document wrapper')
    require(label, 'database.UnmanagedObject == nativeDatabaseIdentity' in text,
            'generation helper must require the exact native database identity')
    require(label, body.count('CloseCandidateOnAffinityDrift(candidate);') >= 2,
            'candidate cleanup must cover both pre-show and post-show affinity drift')
    require(label, 'if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))' in body,
            'status/error publication must be gated by the exact active document generation')
    require(label, 'if (!candidate.Window.IsLoaded)' in text and
                   'if (ReferenceEquals(_pending, candidate)) _pending = null;' in text,
            'failed candidate close must retain ownership while a window remains loaded')

if failures:
    for failure in failures:
        print('FAIL', failure)
    print(f'FAILED: {len(failures)} modeless manager publication-affinity requirement(s) missing')
    sys.exit(1)

print('PASS: V25 manager modeless publication is fenced to the exact active document generation')
