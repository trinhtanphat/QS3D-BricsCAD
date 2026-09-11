from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / 'src' / 'QS3D.BricsCAD.V25' / 'RoomFinishScheduleWindowCommands.cs',
    ROOT / 'src' / 'QS3D.BricsCAD.V25' / 'WallQuantityCommands.cs',
]
errors = []
for source in FILES:
    text = source.read_text(encoding='utf-8')
    name = source.name
    required = [
        'private static PublishedWindow? _pending;',
        'private static PublishedWindow? _published;',
        'if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;',
        'var releaseOwner = owner;',
        'window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);',
        '_pending = owner;',
        'Application.ShowModelessWindow(IntPtr.Zero, window, true);',
        'if (!window.IsLoaded)',
        'if (!ReferenceEquals(_pending, owner))',
        '_pending = null;',
        '_published = owner;',
        'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)',
        'database.UnmanagedObject == nativeDatabaseIdentity',
    ]
    for needle in required:
        if needle not in text:
            errors.append(f'{name}: missing contract: {needle}')
    show = text.find('Application.ShowModelessWindow(IntPtr.Zero, window, true);')
    publish = text.find('_published = owner;', show)
    if show >= 0 and publish >= 0:
        if text.find('if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))', show, publish) < 0:
            errors.append(f'{name}: missing exact-generation fence after host show')
    if 'window.Closed += (_, __) => ReleaseOwnedWindow(owner);' in text:
        errors.append(f'{name}: Closed callback captures mutable owner')

print('QS3D V25 report modeless document affinity preflight')
if errors:
    for error in errors:
        print('ERROR:', error)
    print('FAILED with', len(errors), 'error(s).')
    raise SystemExit(1)
print('PASS: report modeless launchers use exact document/native-generation affinity and pending-first publication ownership.')
