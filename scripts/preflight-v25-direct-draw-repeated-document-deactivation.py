from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'src/QS3D.BricsCAD.V25/DirectDrawRepeatedCommands.cs'
text = SOURCE.read_text(encoding='utf-8')
failures = []


def require(condition, message):
    if not condition:
        failures.append(message)


class_pos = text.find('private sealed class RepeatedDocumentLifecycleGuard')
require(class_pos >= 0, 'missing RepeatedDocumentLifecycleGuard')
body = text[class_pos:] if class_pos >= 0 else ''
callback_pos = body.find('private void OnDocumentToBeDeactivated')
require(callback_pos >= 0, 'missing DocumentToBeDeactivated callback')
callback = body[callback_pos:body.find('\n        }\n\n        private sealed class RepeatedWholeCommandRollbackException', callback_pos)] if callback_pos >= 0 else ''

require('_disposeRequested' in callback and 'TryDetach();' in callback,
        'disposed/retained callbacks must remain cleanup-only and retry native detach')
require('Document deactivatingDocument;' in callback,
        'callback must snapshot the native event payload behind exception containment')
require('deactivatingDocument = args.Document;' in callback,
        'callback must isolate the DocumentCollectionEventArgs.Document accessor')
require('catch' in callback,
        'native event payload accessor failure must be exception-contained')
require('_wasDeactivated = true;' in callback,
        'native event payload accessor failure must fail closed by marking the command deactivated')

try_pos = callback.find('try')
access_pos = callback.find('deactivatingDocument = args.Document;')
catch_pos = callback.find('catch', access_pos if access_pos >= 0 else 0)
require(try_pos >= 0 and access_pos > try_pos and catch_pos > access_pos,
        'Document accessor must execute inside the callback try/catch boundary')

# Preserve the already-hardened native subscription ownership contract while changing callback containment.
require('_subscribed = true;' in body and
        body.find('_subscribed = true;') < body.find('_documents.DocumentToBeDeactivated += OnDocumentToBeDeactivated;'),
        'subscription ownership must remain conservatively published before fallible native add')
remove_pos = body.find('_documents.DocumentToBeDeactivated -= OnDocumentToBeDeactivated;')
clear_pos = body.find('_subscribed = false;', remove_pos if remove_pos >= 0 else 0)
require(remove_pos >= 0 and clear_pos > remove_pos,
        'subscription ownership must clear only after exact native remove succeeds')
require('_detachInProgress' in body,
        'detach retry must remain reentrancy-fenced')

if failures:
    for failure in failures:
        print('FAIL', failure)
    print(f'FAILED: {len(failures)} repeated Direct Draw document-deactivation requirement(s) missing')
    sys.exit(1)

print('PASS: repeated Direct Draw contains document-deactivation payload failures and fails closed')
