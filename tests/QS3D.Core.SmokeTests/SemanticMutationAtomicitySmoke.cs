using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticMutationAtomicitySmoke
    {
        public static void Run()
        {
            BulkSetPropertyOverflowRollsBack();
            BulkMultiplyOverflowRollsBack();
            BulkAssignFamilyOverflowRollsBack();
            HostRelinkAuditOverflowRollsBack();
            HostUnlinkAuditOverflowRollsBack();
            StaleAutoHostAuditOverflowRollsBack();
            DirtyPropagationOverflowRollsBack();
            SemanticUntrackOverflowRollsBack();
        }

        private static void BulkSetPropertyOverflowRollsBack()
        {
            var project = AtVersion(BulkProject(), long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => new BulkEditService().SetProperty(project, new[] { RequiredElement(project, "W1") }, "WidthM", "0.25"));

            var wall = RequiredElement(project, "W1");
            Equal("0.2", wall.Properties["WidthM"], "Failed bulk set partially changed the property.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed bulk set partially changed dirty flags.");
            Equal(long.MaxValue, project.ChangeVersion, "Failed bulk set changed the maximum project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed bulk set changed UpdatedUtc.");
        }

        private static void BulkMultiplyOverflowRollsBack()
        {
            var project = AtVersion(BulkProject(), long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => new BulkEditService().MultiplyNumericProperty(project, new[] { RequiredElement(project, "W1") }, "WidthM", 2d));

            var wall = RequiredElement(project, "W1");
            Equal("0.2", wall.Properties["WidthM"], "Failed bulk multiply partially changed the property.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed bulk multiply partially changed dirty flags.");
            Equal(long.MaxValue, project.ChangeVersion, "Failed bulk multiply changed the maximum project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed bulk multiply changed UpdatedUtc.");
        }

        private static void BulkAssignFamilyOverflowRollsBack()
        {
            var project = AtVersion(BulkProject(), long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => new BulkEditService().AssignFamily(project, new[] { "W1" }, "F-B"));

            var wall = RequiredElement(project, "W1");
            Equal("F-A", wall.FamilyId, "Failed bulk family assignment changed FamilyId.");
            Equal("0.2", wall.Properties["WidthM"], "Failed bulk family assignment changed inherited properties.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed bulk family assignment partially changed dirty flags.");
            Equal(long.MaxValue, project.ChangeVersion, "Failed bulk family assignment changed the maximum project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed bulk family assignment changed UpdatedUtc.");
        }

        private static void HostRelinkAuditOverflowRollsBack()
        {
            var project = AtVersion(HostProject(), long.MaxValue - 1L);
            var beforeUtc = project.UpdatedUtc;
            var beforeAudits = project.AuditEvents.Count;

            Throws<OverflowException>(() => new HostLinkService().LinkOpening(project, "OPENING", "WALL-B"));

            var opening = RequiredElement(project, "OPENING");
            Equal("WALL-A", opening.Properties["HostWallId"], "Failed re-host changed HostWallId.");
            Equal(1, opening.DependsOn.Count, "Failed re-host changed dependency count.");
            Equal("WALL-A", opening.DependsOn.Single(), "Failed re-host changed the host dependency.");
            Equal(ElementDirtyFlags.None, opening.Dirty, "Failed re-host changed opening dirty flags.");
            Equal(ElementDirtyFlags.None, RequiredElement(project, "WALL-A").Dirty, "Failed re-host changed previous host dirty flags.");
            Equal(ElementDirtyFlags.None, RequiredElement(project, "WALL-B").Dirty, "Failed re-host changed new host dirty flags.");
            Equal(beforeAudits, project.AuditEvents.Count, "Failed re-host appended an audit event.");
            Equal(long.MaxValue - 1L, project.ChangeVersion, "Failed re-host did not restore the pre-operation project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed re-host did not restore UpdatedUtc.");
        }

        private static void HostUnlinkAuditOverflowRollsBack()
        {
            var project = AtVersion(HostProject(), long.MaxValue - 1L);
            var beforeUtc = project.UpdatedUtc;
            var beforeAudits = project.AuditEvents.Count;

            Throws<OverflowException>(() => new HostLinkService().UnlinkOpening(project, "OPENING"));

            var opening = RequiredElement(project, "OPENING");
            Equal("WALL-A", opening.Properties["HostWallId"], "Failed unlink removed HostWallId.");
            Equal(1, opening.DependsOn.Count, "Failed unlink changed dependency count.");
            Equal("WALL-A", opening.DependsOn.Single(), "Failed unlink changed the host dependency.");
            Equal(ElementDirtyFlags.None, opening.Dirty, "Failed unlink changed opening dirty flags.");
            Equal(ElementDirtyFlags.None, RequiredElement(project, "WALL-A").Dirty, "Failed unlink changed host dirty flags.");
            Equal(beforeAudits, project.AuditEvents.Count, "Failed unlink appended an audit event.");
            Equal(long.MaxValue - 1L, project.ChangeVersion, "Failed unlink did not restore the pre-operation project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed unlink did not restore UpdatedUtc.");
        }

        private static void StaleAutoHostAuditOverflowRollsBack()
        {
            var source = HostProject();
            var opening = RequiredElement(source, "OPENING");
            opening.Properties.Remove("HostWallId");
            opening.DependsOn.Clear();
            opening.Properties["AutoHostMatched"] = "1";
            opening.MarkClean(ElementDirtyFlags.All);
            var project = AtVersion(source, long.MaxValue - 1L);
            var beforeUtc = project.UpdatedUtc;
            var beforeAudits = project.AuditEvents.Count;

            Throws<OverflowException>(() => new HostLinkService().UnlinkOpening(project, "OPENING"));

            opening = RequiredElement(project, "OPENING");
            Equal("1", opening.Properties["AutoHostMatched"], "Failed stale auto-host cleanup removed provenance metadata.");
            Equal(ElementDirtyFlags.None, opening.Dirty, "Failed stale auto-host cleanup changed dirty flags.");
            Equal(beforeAudits, project.AuditEvents.Count, "Failed stale auto-host cleanup appended an audit event.");
            Equal(long.MaxValue - 1L, project.ChangeVersion, "Failed stale auto-host cleanup did not restore the pre-operation project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed stale auto-host cleanup did not restore UpdatedUtc.");
        }

        private static void DirtyPropagationOverflowRollsBack()
        {
            var source = new ProjectState("P-DIRTY-ATOMIC", "Dirty propagation atomicity");
            var root = new ProjectElement("ROOT", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            dependent.DependsOn.Add(root.Id);
            root.MarkClean(ElementDirtyFlags.All);
            dependent.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(root);
            source.Elements.Add(dependent);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;
            var engine = new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>());

            Throws<OverflowException>(() => engine.MarkChanged(project, "ROOT", ElementDirtyFlags.Properties));

            root = RequiredElement(project, "ROOT");
            dependent = RequiredElement(project, "DEPENDENT");
            Equal(ElementDirtyFlags.None, root.Dirty, "Failed dirty propagation changed the source dirty flags.");
            Equal(ElementDirtyFlags.None, dependent.Dirty, "Failed dirty propagation changed dependent dirty flags.");
            Equal(long.MaxValue, project.ChangeVersion, "Failed dirty propagation changed the maximum project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed dirty propagation changed UpdatedUtc.");
        }

        private static void SemanticUntrackOverflowRollsBack()
        {
            var source = new ProjectState("P-UNTRACK-ATOMIC", "Semantic untrack atomicity");
            var wall = new ProjectElement("W-UNTRACK", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.SourceHandles.Add("A1");
            wall.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(wall);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => SemanticUntrackService.Untrack(project, new[] { "A1" }));

            wall = RequiredElement(project, "W-UNTRACK");
            Equal(1, project.Elements.Count, "Failed semantic untrack removed the target element.");
            Equal(1, wall.SourceHandles.Count, "Failed semantic untrack changed source handles.");
            Equal("A1", wall.SourceHandles.Single(), "Failed semantic untrack changed source ownership.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed semantic untrack changed dirty flags.");
            Equal(long.MaxValue, project.ChangeVersion, "Failed semantic untrack changed the maximum project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed semantic untrack changed UpdatedUtc.");
        }

        private static ProjectState BulkProject()
        {
            var project = new ProjectState("P-BULK-ATOMIC", "Bulk atomicity");
            var familyA = new ProjectFamily("F-A", "Wall A", ElementCategory.ArchitecturalWall);
            familyA.Properties["WidthM"] = "0.2";
            var familyB = new ProjectFamily("F-B", "Wall B", ElementCategory.ArchitecturalWall);
            familyB.Properties["WidthM"] = "0.3";
            project.Families.Add(familyA);
            project.Families.Add(familyB);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, familyA.Id, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            return project;
        }

        private static ProjectState HostProject()
        {
            var project = new ProjectState("P-HOST-ATOMIC", "Host atomicity");
            var wallA = new ProjectElement("WALL-A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            var wallB = new ProjectElement("WALL-B", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            var opening = new ProjectElement("OPENING", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            opening.Properties["HostWallId"] = wallA.Id;
            opening.DependsOn.Add(wallA.Id);
            wallA.MarkClean(ElementDirtyFlags.All);
            wallB.MarkClean(ElementDirtyFlags.All);
            opening.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wallA);
            project.Elements.Add(wallB);
            project.Elements.Add(opening);
            return project;
        }

        private static ProjectState AtVersion(ProjectState source, long version)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-semantic-atomicity-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(source, path);
                var document = XDocument.Load(path);
                var root = document.Root ?? throw new Exception("QSDB fixture has no root element.");
                root.SetAttributeValue("changeVersion", version.ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);
                return store.Load(path);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static ProjectElement RequiredElement(ProjectState project, string id) =>
            project.FindElement(id) ?? throw new Exception("Missing fixture element: " + id);

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
