using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ActiveFloorZoneCanonicalRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RepairsFloorAliasAndPreservesCanonicalNoOp();
            RejectsZoneAliasAndPreservesCanonicalNoOp();
        }

        private static void RepairsFloorAliasAndPreservesCanonicalNoOp()
        {
            var project = new ProjectState("P-ACTIVE-FLOOR-CANONICAL", "Active floor canonical regression");
            var floor = ProjectFloorService.Create(project, "floor-a", "Floor A", 0d);
            project.ActiveFloorId = " FLOOR-A ";
            var beforeRepair = project.ChangeVersion;

            ProjectFloorService.SetActive(project, " Floor-A ");

            Equal(floor.Id, project.ActiveFloorId, "floor canonical repair");
            Equal(beforeRepair + 1L, project.ChangeVersion, "floor repair version");

            var canonicalVersion = project.ChangeVersion;
            ProjectFloorService.SetActive(project, floor.Id);
            Equal(canonicalVersion, project.ChangeVersion, "floor canonical no-op version");

            Throws<InvalidOperationException>(() => ProjectFloorService.SetActive(project, "missing-floor"));
            Equal(floor.Id, project.ActiveFloorId, "floor missing-id state");
            Equal(canonicalVersion, project.ChangeVersion, "floor missing-id version");
        }

        private static void RejectsZoneAliasAndPreservesCanonicalNoOp()
        {
            var project = new ProjectState("P-ACTIVE-ZONE-CANONICAL", "Active zone canonical regression");
            var zone = ProjectZoneService.Create(project, "zone-a", "Zone A");
            var canonicalVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, " Zone-A "));
            Equal(zone.Id, project.ActiveZoneId, "zone padded caller state");
            Equal(canonicalVersion, project.ChangeVersion, "zone padded caller version");

            SetRawActiveZoneId(project, " ZONE-A ");
            var corruptVersion = project.ChangeVersion;
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, zone.Id));
            Equal(" ZONE-A ", RawActiveZoneId(project), "zone padded stored state");
            Equal(corruptVersion, project.ChangeVersion, "zone padded stored version");

            SetRawActiveZoneId(project, zone.Id);
            ProjectZoneService.SetActive(project, zone.Id);
            Equal(corruptVersion, project.ChangeVersion, "zone canonical no-op version");

            Throws<InvalidOperationException>(() => ProjectZoneService.SetActive(project, "missing-zone"));
            Equal(zone.Id, project.ActiveZoneId, "zone missing-id state");
            Equal(corruptVersion, project.ChangeVersion, "zone missing-id version");
        }

        private static void SetRawActiveZoneId(ProjectState project, string value)
        {
            var field = typeof(ProjectState).GetField("_activeZoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("ProjectState._activeZoneId field was not found.");
            field.SetValue(project, value);
        }

        private static string RawActiveZoneId(ProjectState project)
        {
            var field = typeof(ProjectState).GetField("_activeZoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("ProjectState._activeZoneId field was not found.");
            return field.GetValue(project) as string
                ?? throw new InvalidOperationException("ProjectState._activeZoneId was not a string.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ActiveFloorZoneCanonicalRegressionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("ActiveFloorZoneCanonicalRegressionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
