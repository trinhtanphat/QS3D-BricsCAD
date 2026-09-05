using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorCreateDuplicateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var malformed = new ProjectState("P-FLOOR-DUP", "Floor duplicate smoke");
            malformed.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            malformed.Floors.Add(new FloorDefinition("f1", "Level 1 duplicate", 3d));
            malformed.ActiveFloorId = "F1";
            var beforeCount = malformed.Floors.Count;
            var beforeActive = malformed.ActiveFloorId;
            var beforeVersion = malformed.ChangeVersion;
            var beforeUpdated = malformed.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectFloorService.Create(malformed, "F2", "Level 2", 6d));
            Equal(beforeCount, malformed.Floors.Count, "malformed count");
            Equal(beforeActive, malformed.ActiveFloorId, "malformed active floor");
            Equal(beforeVersion, malformed.ChangeVersion, "malformed change version");
            Equal(beforeUpdated, malformed.UpdatedUtc, "malformed updated time");

            var valid = new ProjectState("P-FLOOR-VALID", "Floor valid smoke");
            ProjectFloorService.Create(valid, "F1", "Level 1", 0d);
            var validVersion = valid.ChangeVersion;
            var created = ProjectFloorService.Create(valid, "F2", "Level 2", 3d);
            Equal("F2", created.Id, "valid created id");
            Equal(2, valid.Floors.Count, "valid count");
            Equal(validVersion + 2L, valid.ChangeVersion, "valid revision");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectFloorCreateDuplicateIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectFloorCreateDuplicateIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
