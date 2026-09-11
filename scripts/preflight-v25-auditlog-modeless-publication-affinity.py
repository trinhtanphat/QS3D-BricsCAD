from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / 'src/QS3D.BricsCAD.V25/AuditCommands.cs'
text = PATH.read_text(encoding='utf-8')
body_start = text.find('public void ShowAuditLog')
body = text[body_start:] if body_start >= 0 else text
failures = []

def require(condition, message):
    if not condition:
        failures.append(message)

needle = 'IsActiveDocumentGeneration(document, nativeDatabaseIdentity)'
calls = []
pos = 0
while True:
    found = body.find(needle, pos)
    if found < 0:
        break
    calls.append(found)
    pos = found + len(needle)

require('nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);' in body,
        'must capture a non-zero native database identity at command admission')
require('private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)' in text,
        'missing exact active managed-document/native-database generation helper')
require('ReferenceEquals(activeDocument, document)' in text,
        'generation helper must require exact managed Document wrapper')
require('database.UnmanagedObject == nativeDatabaseIdentity' in text,
        'generation helper must require exact native database identity')
require(len(calls) >= 7,
        'must revalidate after cleanup/replacement, around reuse, construction/show, and before status publication')
require('private static WeakReference<Document>? _publishedDocument;' in text,
        'published Audit Log ownership must retain exact managed Document affinity')
require('PreparePublishedWindow(document, nativeDatabaseIdentity)' in body,
        'published-window preparation must receive the exact managed Document generation')
require('_publishedDocument = new WeakReference<Document>(document);' in body,
        'publication must bind the owner to the exact managed Document wrapper')
require('PublishedDocumentMatches(document)' in text,
        'same-native-database reuse must also require the exact managed Document wrapper')
require('CloseUnpublishedCandidate(candidate)' in body,
        'affinity drift must use existing residue-aware unpublished-candidate cleanup')
show_pos = body.find('Application.ShowModelessWindow')
publish_pos = body.find('_window = candidate;')
require(show_pos >= 0 and publish_pos >= 0 and any(show_pos < p < publish_pos for p in calls),
        'must revalidate exact document generation after ShowModelessWindow before authority transfer')
require('if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;' in body,
        'stale/background generations must fail closed before UI/status publication')
require('if (ReferenceEquals(_window, candidate))' in text and '_publishedDocument = null;' in text,
        'terminal published-window release must clear managed-wrapper ownership')

candidate_pos = body.find('var candidate = new AuditLogWindow(document);')
unpublished_pos = body.find('_unpublishedCandidate = candidate;', candidate_pos)
inflight_pos = body.find('_publicationInFlightCandidate = candidate;', candidate_pos)
pre_show_fence_pos = body.find('if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))', unpublished_pos)
require(candidate_pos >= 0 and unpublished_pos >= 0 and pre_show_fence_pos >= 0 and inflight_pos >= 0,
        'must retain unpublished ownership and a pre-show document-generation fence')
require(pre_show_fence_pos < inflight_pos < show_pos,
        'publication-in-flight ownership must begin only after the pre-show affinity fence, immediately before host show')
require('finally\n                {\n                    if (ReferenceEquals(_publicationInFlightCandidate, candidate))\n                        _publicationInFlightCandidate = null;' in body,
        'publication-in-flight ownership must be cleared in the ShowModelessWindow finally path')

if failures:
    for failure in failures:
        print('FAIL Audit Log:', failure)
    print(f'FAILED: {len(failures)} Audit Log modeless publication-affinity requirement(s) missing')
    sys.exit(1)

print('PASS: Audit Log modeless publication is fenced to the exact active document generation')
