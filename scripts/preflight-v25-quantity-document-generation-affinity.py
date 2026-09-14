#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
INSIGHT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
SUMMARY = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: missing {needle}")


def ordered(text: str, first: str, second: str, label: str) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        fail(f"{label}: expected {first} before {second}")


insight = INSIGHT.read_text(encoding="utf-8")
summary = SUMMARY.read_text(encoding="utf-8")
require(insight, "private IntPtr _boundNativeDatabaseIdentity;", "QuantityInsightPanel")
require(insight, "private static IntPtr GetNativeDatabaseIdentity(Document document)", "QuantityInsightPanel")
require(insight, "private bool IsBoundToCurrentNativeGeneration(Document document)", "QuantityInsightPanel")
require(insight, "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);", "QuantityInsightPanel")
require(insight, "_boundNativeDatabaseIdentity = nativeDatabaseIdentity;", "QuantityInsightPanel")
require(insight, "_boundNativeDatabaseIdentity = IntPtr.Zero;", "QuantityInsightPanel")
require(insight, "if (!IsBoundToCurrentNativeGeneration(document))", "QuantityInsightPanel")
ordered(insight, "if (!IsBoundToCurrentNativeGeneration(document))", "LocateSelectionGeometry(document, item);", "QuantityInsightPanel Locate")

require(summary, "private readonly IntPtr _nativeDatabaseIdentity;", "QuantitySummaryWindow")
require(summary, "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(_document);", "QuantitySummaryWindow")
require(summary, "private static IntPtr GetNativeDatabaseIdentity(Document document)", "QuantitySummaryWindow")
require(summary, "private bool IsCurrentNativeGeneration()", "QuantitySummaryWindow")
require(summary, "if (!IsCurrentNativeGeneration())", "QuantitySummaryWindow")
ordered(summary, "if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))", "if (!IsCurrentNativeGeneration())", "QuantitySummaryWindow EnsureActive")

if "UnmanagedObject" not in insight or "UnmanagedObject" not in summary:
    fail("quantity modeless surfaces must read native database identity")
if "GetNativeDatabaseIdentity(document) == _boundNativeDatabaseIdentity" not in insight:
    fail("QuantityInsightPanel must compare the current native generation to its bound identity")
if "database.UnmanagedObject == _nativeDatabaseIdentity" not in summary:
    fail("QuantitySummaryWindow must compare the current native generation to its bound identity")

print("PASS: Quantity Insight and BQ operations are fenced to the exact native database generation")
