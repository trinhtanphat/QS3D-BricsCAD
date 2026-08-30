#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomFinishScheduleWindowCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    'private static RoomFinishScheduleWindow? _window;',
    'private static Document? _publishedDocument;',
    'private static IntPtr _publishedNativeDatabaseIdentity;',
    'var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);',
    'if (!PreparePublishedWindow(document, nativeDatabaseIdentity))',
    'if (_window != null)',
    'window.Closed += (_, __) => ReleasePublishedWindow(window);',
    'Application.ShowModelessWindow(IntPtr.Zero, window, true);',
    'if (!window.IsLoaded) return;',
    '_publishedDocument = document;',
    '_publishedNativeDatabaseIdentity = nativeDatabaseIdentity;',
    '_window = window;',
    'ReferenceEquals(_publishedDocument, requestedDocument)',
    '_publishedNativeDatabaseIdentity == requestedNativeDatabaseIdentity',
    'published.Close();',
    'if (published.IsLoaded)',
    'if (!ReferenceEquals(_window, window)) return;',
    '_publishedNativeDatabaseIdentity = IntPtr.Zero;',
    'var identity = database.UnmanagedObject;',
    'if (identity == IntPtr.Zero)',
]

missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("Room Finish Schedule publication guard missing contract tokens: " + "; ".join(missing))

show = text.index('Application.ShowModelessWindow(IntPtr.Zero, window, true);')
loaded = text.index('if (!window.IsLoaded) return;', show)
publish_doc = text.index('_publishedDocument = document;', loaded)
publish_native = text.index('_publishedNativeDatabaseIdentity = nativeDatabaseIdentity;', publish_doc)
publish_window = text.index('_window = window;', publish_native)
if not (show < loaded < publish_doc < publish_native < publish_window):
    raise SystemExit("Room Finish Schedule must publish only after ShowModelessWindow and IsLoaded admission")

close_call = text.index('published.Close();')
close_check = text.index('if (published.IsLoaded)', close_call)
release_after_close = text.index('ReleasePublishedWindow(published);', close_check)
if not (close_call < close_check < release_after_close):
    raise SystemExit("Room Finish Schedule replacement must require terminal close before release")

if 'new RoomFinishScheduleWindow(document), true' in text:
    raise SystemExit("Room Finish Schedule must not directly publish an untracked transient window")

print("PASS Room Finish Schedule modeless publication lifecycle")
