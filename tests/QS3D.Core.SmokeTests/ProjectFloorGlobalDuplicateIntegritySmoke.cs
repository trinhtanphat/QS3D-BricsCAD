using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorGlobalDuplicateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var malformed = new ProjectState("P-FLOOR-GLOBAL-DUP", "Global floor duplicate smoke");
            malformed.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            malformed.Floors.Add(new FloorDefinition("f1", "Level 1 duplicate", 1d));
            malformed.Floors.Add(new FloorDefinition("F2", "Level 2", 3d));
            malformed.ActiveFloorId = "F1";
            var beforeActive = malformed.ActiveFloorId;
            var beforeVersion = malformed.ChangeVersion;
            var beforeUpdated = malformed.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectFloorService.SetActive(malformed, "F2"));
            Equal(beforeActive, malformed.ActiveFloorId, "malformed active floor");
            Equal(beforeVersion, malformed.ChangeVersion, "malformed change version");
            Equal(beforeUpdated, malformed.UpdatedUtc, "malformed updated time");

            var valid = new ProjectState("P-FLOOR-GLOBAL-VALID", "Global floor valid smoke");
            valid.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            valid.Floors.Add(new FloorDefinition("F2", "Level 2", 3d));
            valid.ActiveFloorId = "F1";
            var validVersion = valid.ChangeVersion;
            ProjectFloorService.SetActive(valid, "f2");
            Equal("F2", valid.ActiveFloorId, "valid active floor");
            Equal(validVersion + 1L, valid.ChangeVersion, "valid revision");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectFloorGlobalDuplicateIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectFloorGlobalDuplicateIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
