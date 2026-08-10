using System;
using System.IO;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbCanonicalPersistenceSmoke
    {
        public static void Run()
        {
            PaddedMapKeyFailsBeforePersistence();
            PaddedQuantityNameFailsBeforePersistence();
            NonCanonicalHandleAndDependencyFailBeforePersistence();
            NullAuditEventFailsClosed();
        }

        private static void PaddedMapKeyFailsBeforePersistence()
        {
            var project = NewProject("map-key");
            project.Metadata[" padded "] = "value";
            RejectSave(project, "Padded metadata key was silently persisted/normalized.");

            project = NewProject("property-key");
            var element = AddElement(project);
            element.Properties[" WidthM "] = "1.2";
            RejectSave(project, "Padded element property key was silently persisted/normalized.");
        }

        private static void PaddedQuantityNameFailsBeforePersistence()
        {
            var project = NewProject("quantity-key");
            var element = AddElement(project);
            element.Quantities[" AreaM2 "] = 3d;
            RejectSave(project, "Padded quantity name was silently persisted/normalized.");
        }

        private static void NonCanonicalHandleAndDependencyFailBeforePersistence()
        {
            var project = NewProject("handle-key");
            var element = AddElement(project);
            element.SourceHandles.Add(" 1A ");
            RejectSave(project, "Padded source handle was silently persisted/normalized.");

            project = NewProject("dependency-key");
            element = AddElement(project);
            element.DependsOn.Add(" E2 ");
            RejectSave(project, "Padded dependency id was silently persisted/normalized.");

            project = NewProject("blank-handle");
            element = AddElement(project);
            element.SourceHandles.Add("   ");
            RejectSave(project, "Blank source handle was silently dropped during persistence.");
        }

        private static void NullAuditEventFailsClosed()
        {
            var project = NewProject("null-audit");
            project.AuditEvents.Add(null!);
            RejectSave(project, "Null audit event reached serialization instead of failing validation.");
        }

        private static ProjectState NewProject(string id)
        {
            return new ProjectState(id, "Canonical persistence");
        }

        private static ProjectElement AddElement(ProjectState project)
        {
            var element = new ProjectElement("E1", ElementCategory.Wall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return element;
        }

        private static void RejectSave(ProjectState project, string message)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-canonical-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var rejected = false;
                try { new QsdbProjectStore().Save(project, path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception(message);
                if (File.Exists(path)) throw new Exception("Rejected non-canonical project still created a QSDB file.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }
    }
}
