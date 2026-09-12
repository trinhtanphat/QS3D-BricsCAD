from pathlib import Path
root = Path(__file__).resolve().parents[1]
files = [
    root / 'src/QS3D.BricsCAD.V25/DomainHubCommands.cs',
    root / 'src/QS3D.BricsCAD.V25/GeometryExtensionsCommands.cs',
    root / 'src/QS3D.BricsCAD.V25/ReferenceSearchCommands.cs',
]
required = [
    'PublishedWindow? _pending', 'PublishedWindow? _published',
    'IsActiveDocumentGeneration', 'NativeDatabaseIdentity',
    'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)',
    'Application.ShowModelessWindow', 'if (!ReferenceEquals(_pending, owner))',
    'ClosePendingOnFailure',
]
for path in files:
    text = path.read_text(encoding='utf-8-sig')
    for token in required:
        if token not in text:
            raise SystemExit(f'{path.name}: missing {token}')
    show = text.index('Application.ShowModelessWindow')
    post = text.find('IsActiveDocumentGeneration', show)
    publish = text.find('_published = owner', show)
    if post < 0 or publish < 0 or not (show < post < publish):
        raise SystemExit(f'{path.name}: host-pumping fence ordering invalid')
print('PASS: auxiliary modeless document-affinity lifecycle')