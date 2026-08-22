using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGeometryStaleHandleIdentitySmoke
    {
        internal static void Run()
        {
            SingleHandleSpellingUsesNumericIdentity();
            LegacySnapshotSpellingUsesNumericIdentity();
            MultiHandleSignatureUsesNumericIdentity();
            CurtainPanelSpellingUsesNumericIdentity();
        }

        private static void SingleHandleSpellingUsesNumericIdentity()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "0A";
            element.MarkGeneratedGeometryStale("source changed");

            Equal("A", element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey]);
            True(element.IsGeneratedSolidStale());

            element.Properties["GeneratedSolidHandle"] = "0xA";
            True(element.IsGeneratedSolidStale());

            element.Properties["GeneratedSolidHandle"] = "A";
            True(element.IsGeneratedSolidStale());

            element.Properties["GeneratedSolidHandle"] = "B";
            False(element.IsGeneratedSolidStale());
        }

        private static void LegacySnapshotSpellingUsesNumericIdentity()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "A";
            element.Properties[ProjectElement.GeneratedSolidStateKey] = "stale";
            element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey] = "000A";

            True(element.IsGeneratedSolidStale());
            Equal("000A", element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey]);
        }

        private static void MultiHandleSignatureUsesNumericIdentity()
        {
            var element = Element();
            element.Properties["GeneratedRebarHandles"] = "000A;B;0x0A";
            element.MarkGeneratedGeometryStale("rebar source changed");

            Equal("A;B", element.Properties[ProjectElement.GeneratedRebarStaleSnapshotKey]);
            element.Properties["GeneratedRebarHandles"] = "0xB;A;0A";
            True(element.IsGeneratedRebarStale());

            element.Properties["GeneratedRebarHandles"] = "A;C";
            False(element.IsGeneratedRebarStale());
        }

        private static void CurtainPanelSpellingUsesNumericIdentity()
        {
            var element = Element();
            element.Properties["GeneratedCurtainPanelHandles"] = "0x0A;0B";
            element.MarkGeneratedCurtainPanelStale("panel source changed");

            Equal("A;B", element.Properties[ProjectElement.GeneratedCurtainPanelStaleSnapshotKey]);
            element.Properties["GeneratedCurtainPanelHandles"] = "B;A";
            True(element.IsGeneratedCurtainPanelStale());

            element.Properties["GeneratedCurtainPanelHandles"] = "A;C";
            False(element.IsGeneratedCurtainPanelStale());
        }

        private static ProjectElement Element() => new ProjectElement("E-HANDLE-STALE", ElementCategory.Beam);
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + "."); }
    }
}
