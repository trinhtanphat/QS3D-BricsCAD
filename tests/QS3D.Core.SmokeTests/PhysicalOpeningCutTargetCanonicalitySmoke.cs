using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        public static void Run()
        {
            CanonicalRoundTripAndResolutionRemainCompatible();
            PaddedCallerIdsFailClosedAcrossPublicEntryPoints();
            RejectedWritePreservesExistingMetadata();
            PersistedReadAndCallerCanonicalityStaySymmetric();
            NonCanonicalProjectElementIdsFailBeforeResolution();
            DuplicateAndHostRelationContractsRemainFailClosed();
        }

        private static void CanonicalRoundTripAndResolutionRemainCompatible()
        {
            var fixture = CreateFixture();
            PhysicalOpeningCutTargetStateCodec.Write(fixture.Host, new[] { "opening-b", "OPENING-A" });
            if (!PhysicalOpeningCutTargetStateCodec.TryRead(fixture.Host, out var persisted))
                throw new Exception("Canonical physical opening target-state must remain readable.");
            if (persisted.Count != 2 || persisted[0] != "OPENING-A" || persisted[1] != "opening-b")
                throw new Exception("Physical opening target-state must preserve deterministic canonical ordering without changing caller casing.");

            var resolved = PhysicalOpeningCutTargetStateCodec.Resolve(fixture.Project, fixture.Host, new[] { "OPENING-A", "opening-B" });
            if (resolved.Count != 2 || !ReferenceEquals(resolved[0], fixture.OpeningA) || !ReferenceEquals(resolved[1], fixture.OpeningB))
                throw new Exception("Canonical case-insensitive physical opening identity resolution regressed.");
        }

        private static void PaddedCallerIdsFailClosedAcrossPublicEntryPoints()
        {
            foreach (var padded in new[] { " opening-a", "opening-a ", " opening-a ", "\topening-a", "opening-a\t" })
            {
                ExpectInvalidOperation(
                    () => PhysicalOpeningCutTargetStateCodec.Normalize(new[] { padded }),
                    "non-canonical opening id");

                var writeFixture = CreateFixture();
                ExpectInvalidOperation(
                    () => PhysicalOpeningCutTargetStateCodec.Write(writeFixture.Host, new[] { padded }),
                    "non-canonical opening id");

                var resolveFixture = CreateFixture();
                var version = resolveFixture.Project.ChangeVersion;
                ExpectInvalidOperation(
                    () => PhysicalOpeningCutTargetStateCodec.Resolve(resolveFixture.Project, resolveFixture.Host, new[] { padded }),
                    "non-canonical opening id");
                if (resolveFixture.Project.ChangeVersion != version)
                    throw new Exception("Rejected padded physical opening target id must not mutate project state.");
            }
        }

        private static void RejectedWritePreservesExistingMetadata()
        {
            var fixture = CreateFixture();
            PhysicalOpeningCutTargetStateCodec.Write(fixture.Host, new[] { "opening-a" });
            var before = fixture.Host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey];

            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Write(fixture.Host, new[] { " opening-b " }),
                "non-canonical opening id");

            if (!fixture.Host.Properties.TryGetValue(PhysicalOpeningCutTargetStateCodec.OpeningIdsKey, out var after) ||
                !string.Equals(before, after, StringComparison.Ordinal))
                throw new Exception("Rejected physical opening target write must leave prior host metadata unchanged.");
        }

        private static void PersistedReadAndCallerCanonicalityStaySymmetric()
        {
            var fixture = CreateFixture();
            fixture.Host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(" opening-a "));
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.TryRead(fixture.Host, out _),
                "non-canonical or duplicate id");

            PhysicalOpeningCutTargetStateCodec.Write(fixture.Host, new[] { "opening-a" });
            if (!PhysicalOpeningCutTargetStateCodec.TryRead(fixture.Host, out var ids) || ids.Count != 1 || ids[0] != "opening-a")
                throw new Exception("Canonical caller-written target identity must remain valid persisted target-state.");
        }

        private static void NonCanonicalProjectElementIdsFailBeforeResolution()
        {
            var fixture = CreateFixture(includeOpeningA: false);
            var malformed = new ProjectElement("opening-a", ElementCategory.Door);
            malformed.Properties["HostWallId"] = fixture.Host.Id;

            // ProjectElement's public constructor canonicalizes IDs, so emulate a corrupted in-memory
            // instance that bypassed that boundary in order to exercise the codec's defense-in-depth check.
            var idField = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (idField == null)
                throw new Exception("ProjectElement Id backing field was unavailable for corruption regression setup.");
            idField.SetValue(malformed, " opening-a ");

            fixture.Project.Elements.Add(malformed);
            var version = fixture.Project.ChangeVersion;

            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Resolve(fixture.Project, fixture.Host, new[] { "opening-a" }),
                "non-canonical semantic id");
            if (fixture.Project.ChangeVersion != version)
                throw new Exception("Non-canonical project element identity rejection must remain read-only.");
        }

        private static void DuplicateAndHostRelationContractsRemainFailClosed()
        {
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "opening-a", "OPENING-A" }),
                "duplicate opening id");

            var fixture = CreateFixture();
            fixture.OpeningA.Properties["HostWallId"] = " other-host ";
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Resolve(fixture.Project, fixture.Host, new[] { "opening-a" }),
                "non-canonical HostWallId");
        }

        private static Fixture CreateFixture(bool includeOpeningA = true)
        {
            var project = new ProjectState("P1", "Physical opening canonicality");
            var host = new ProjectElement("host-1", ElementCategory.ArchitecturalWall);
            var openingA = new ProjectElement("opening-a", ElementCategory.Door);
            openingA.Properties["HostWallId"] = host.Id;
            var openingB = new ProjectElement("opening-b", ElementCategory.WallOpening);
            openingB.Properties["HostWallId"] = host.Id;
            project.Elements.Add(host);
            if (includeOpeningA)
                project.Elements.Add(openingA);
            project.Elements.Add(openingB);
            return new Fixture(project, host, openingA, openingB);
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
                throw new Exception("Expected physical opening target-state validation to fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Physical opening target validation failed with an unexpected diagnostic: " + ex.Message);
            }
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ProjectElement host, ProjectElement openingA, ProjectElement openingB)
            {
                Project = project;
                Host = host;
                OpeningA = openingA;
                OpeningB = openingB;
            }

            public ProjectState Project { get; }
            public ProjectElement Host { get; }
            public ProjectElement OpeningA { get; }
            public ProjectElement OpeningB { get; }
        }
    }
}
