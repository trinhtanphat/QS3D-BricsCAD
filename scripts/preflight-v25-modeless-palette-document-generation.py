#!/usr/bin/env python3
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
START = (ROOT / 'src/QS3D.BricsCAD.V25/StartCenterPaletteCoordinator.cs').read_text(encoding='utf-8')
PROJECT = (ROOT / 'src/QS3D.BricsCAD.V25/ProjectSetupPaletteCoordinator.cs').read_text(encoding='utf-8')
START_PANEL = (ROOT / 'src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs').read_text(encoding='utf-8')
PROJECT_PANEL = (ROOT / 'src/QS3D.BricsCAD.V25/UI/BltProjectSetupPanel.cs').read_text(encoding='utf-8')
HELPER = PROJECT_PANEL

for name, text in [('StartCenter', START), ('ProjectInformation', PROJECT)]:
    for token in ('DocumentGenerationGuard.CaptureCurrent', 'RequireCurrentDocumentGeneration'):
        if token not in text:
            raise SystemExit(f'{name} exact native database generation contract missing: {token}')

for token in ('UnmanagedObject', 'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)', 'nativeDatabaseIdentity'):
    if token not in HELPER:
        raise SystemExit(f'shared document-generation guard missing: {token}')

if 'RefreshFromDocument(Document? document, IntPtr nativeDatabaseIdentity)' not in START_PANEL:
    raise SystemExit('Start Center panel refresh must receive exact native database generation')
if 'RefreshFromDocument(Document? document, IntPtr nativeDatabaseIdentity)' not in PROJECT_PANEL:
    raise SystemExit('Project Information panel refresh must receive exact native database generation')

for name, text in [('StartCenterPanel', START_PANEL), ('ProjectInformationPanel', PROJECT_PANEL)]:
    if 'DocumentGenerationGuard.IsCurrent(document, nativeDatabaseIdentity)' not in text:
        raise SystemExit(f'{name} must reject stale managed/native document generation before publication')

if 'RecordProject(normalized)' in START_PANEL:
    record = START_PANEL.index('RecordProject(normalized)')
    guard = START_PANEL.find('DocumentGenerationGuard.IsCurrent(document, nativeDatabaseIdentity)')
    if guard < 0 or guard > record:
        raise SystemExit('Start Center recent-project persistence must be fenced before publication')

# Activation callbacks are host-pumping boundaries too. A refresh can throw after the managed
# Document wrapper survives a native Database replacement, so exception diagnostics/clear-state
# publication must prove the same captured generation rather than writing to the event document or
# clearing the palette unconditionally. Keep these call-site checks whitespace-insensitive so
# formatting cannot create a false regression.
if re.search(r'TryWriteRefreshDiagnostic\s*\(\s*document\s*,\s*nativeDatabaseIdentity\s*\)', START) is None:
    raise SystemExit('Start Center activation diagnostics must be generation-fenced before Editor publication')
if re.search(r'ShowUnavailableIfCurrent\s*\(\s*document\s*,\s*nativeDatabaseIdentity\s*,', PROJECT) is None:
    raise SystemExit('Project Information activation failure clearing must be generation-fenced before UI publication')

for text in (START, PROJECT, START_PANEL, PROJECT_PANEL):
    for forbidden in ('ex.Message', 'error.Message', 'Exception.Message'):
        if forbidden in text:
            raise SystemExit(f'modeless palette generation boundary must not expose raw exception detail: {forbidden}')

print('PASS modeless Start Center / Project Information exact document-generation contract')
