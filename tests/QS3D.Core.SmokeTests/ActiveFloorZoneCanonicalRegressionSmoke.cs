using System;
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
            RepairsZoneAliasAndPreservesCanonicalNoOp();
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

        private static void RepairsZoneAliasAndPreservesCanonicalNoOp()
        {
            var project = new ProjectState("P-ACTIVE-ZONE-CANONICAL", "Active zone canonical regression");
            var zone = ProjectZoneService.Create(project, "zone-a", "Zone A");
            project.ActiveZoneId = " ZONE-A ";
            var beforeRepair = project.ChangeVersion;

            ProjectZoneService.SetActive(project, " Zone-A ");

            Equal(zone.Id, project.ActiveZoneId, "zone canonical repair");
            Equal(beforeRepair + 1L, project.ChangeVersion, "zone repair version");

            var canonicalVersion = project.ChangeVersion;
            ProjectZoneService.SetActive(project, zone.Id);
            Equal(canonicalVersion, project.ChangeVersion, "zone canonical no-op version");

            Throws<InvalidOperationException>(() => ProjectZoneService.SetActive(project, "missing-zone"));
            Equal(zone.Id, project.ActiveZoneId, "zone missing-id state");
            Equal(canonicalVersion, project.ChangeVersion, "zone missing-id version");
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
