using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementNullScalarPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullAssignmentsCanonicalizeImmediately();
            ConstructorAndRelationSettersTrimWhileFingerprintCanonicalizes();
            CanonicalEmptyScalarsRoundTripThroughQsdb();
        }

        private static void NullAssignmentsCanonicalizeImmediately()
        {
            var element = new ProjectElement("ELEMENT-NULL-SCALARS", ElementCategory.Beam, "F", "L", "Z")
            {
                DrawingFingerprint = "FP"
            };

            element.FamilyId = null!;
            element.FloorId = null!;
            element.ZoneId = null!;
            element.DrawingFingerprint = null!;

            Equal(string.Empty, element.FamilyId);
            Equal(string.Empty, element.FloorId);
            Equal(string.Empty, element.ZoneId);
            Equal(string.Empty, element.DrawingFingerprint);
        }

        private static void ConstructorAndRelationSettersTrimWhileFingerprintCanonicalizes()
        {
            var element = new ProjectElement("ELEMENT-EXACT", ElementCategory.Beam, "  F  ", "  L  ", "  Z  ");
            Equal("F", element.FamilyId);
            Equal("L", element.FloorId);
            Equal("Z", element.ZoneId);

            element.FamilyId = "  F2  ";
            element.FloorId = "  L2  ";
            element.ZoneId = "  Z2  ";
            element.DrawingFingerprint = "  FP2  ";

            Equal("F2", element.FamilyId);
            Equal("L2", element.FloorId);
            Equal("Z2", element.ZoneId);
            Equal("FP2", element.DrawingFingerprint);
        }

        private static void CanonicalEmptyScalarsRoundTripThroughQsdb()
        {
            var project = new ProjectState("PROJECT-ELEMENT-NULL-SCALARS", "Element null scalars");
            var element = new ProjectElement("E1", ElementCategory.Beam)
            {
                FamilyId = null!,
                FloorId = null!,
                ZoneId = null!,
                DrawingFingerprint = null!
            };
            project.Elements.Add(element);

            var path = Path.Combine(
                Path.GetTempPath(),
                "qs3d-project-element-null-scalars-" + Guid.NewGuid().ToString("N") + ".qsdb");

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var loadedElement = loaded.FindElement("E1")
                    ?? throw new InvalidOperationException("Round-tripped element was not found.");

                Equal(string.Empty, loadedElement.FamilyId);
                Equal(string.Empty, loadedElement.FloorId);
                Equal(string.Empty, loadedElement.ZoneId);
                Equal(string.Empty, loadedElement.DrawingFingerprint);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup only; persistence assertions above are authoritative.
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
