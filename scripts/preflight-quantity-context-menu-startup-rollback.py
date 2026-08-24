#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantityContextMenuCoordinator.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    sys.exit(1)


if not SOURCE.is_file():
    fail("missing QuantityContextMenuCoordinator.cs")

text = SOURCE.read_text(encoding="utf-8")

required = (
    "var nativeRegistrationAdded = false;",
    "AddObjectContextMenuExtension(runtimeClass, extension);",
    "nativeRegistrationAdded = true;",
    "if (nativeRegistrationAdded)",
    "TryRemoveObjectContextMenuExtension(runtimeClass, extension);",
    "private static void AddObjectContextMenuExtension(RXClass runtimeClass, object extension)",
    "private static void TryRemoveObjectContextMenuExtension(RXClass runtimeClass, object extension)",
    'FindApplicationMethod("AddObjectContextMenuExtension", runtimeClass.GetType(), extension.GetType())',
    'FindApplicationMethod("RemoveObjectContextMenuExtension", runtimeClass.GetType(), extension.GetType())',
)
for token in required:
    if token not in text:
        fail(f"startup rollback contract missing {token!r}")

add_call = text.find("AddObjectContextMenuExtension(runtimeClass, extension);")
mark_registered = text.find("nativeRegistrationAdded = true;", add_call)
publish_owner = text.find("_entityRuntimeClass = runtimeClass;", mark_registered)
refresh = text.find("RefreshMenuItemState();", publish_owner)
catch_block = text.find("catch\n            {", refresh)
rollback = text.find("TryRemoveObjectContextMenuExtension(runtimeClass, extension);", catch_block)
detach_popup = text.find('TryDetachEvent(extension, "Popup", popupHandler);', catch_block)
if min(add_call, mark_registered, publish_owner, refresh, catch_block, rollback, detach_popup) < 0:
    fail("cannot locate transactional startup/rollback ordering")
if not (add_call < mark_registered < publish_owner < refresh < catch_block < rollback < detach_popup):
    fail("native registration must be marked before publication and rolled back before delegate teardown")

stop = text.find("public static void Stop()")
stop_remove = text.find("TryRemoveObjectContextMenuExtension(runtimeClass, extension);", stop)
stop_detach = text.find('TryDetachEvent(extension, "Popup", popupHandler);', stop)
if stop_remove < 0 or stop_detach < 0 or stop_remove > stop_detach:
    fail("Stop must use the same idempotent native-removal helper before delegate teardown")

old_inline_add = 'var addMethod = FindApplicationMethod(\n                    "AddObjectContextMenuExtension"'
old_inline_remove = 'var removeMethod = FindApplicationMethod(\n                        "RemoveObjectContextMenuExtension"'
if old_inline_add in text or old_inline_remove in text:
    fail("Start/Stop must not keep separate inline native registration reflection paths")

print("PASS: quantity context-menu startup owns and rolls back native registration transactionally")
