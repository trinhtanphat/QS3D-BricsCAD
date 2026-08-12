using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetStateOwnerBindingSmoke
    {
        internal static void Run()
        {
            CanonicalHostResolvesOwnedOpeningWithoutMutation();
            DetachedSameIdHostFailsClosed();
            MissingHostFailsClosed();
        }

        private static void CanonicalHostResolvesOwnedOpeningWithoutMutation()
        {
            var fixture = NewFixture();
            var beforeVersion = fixture.Project.ChangeVersion;
            var resolved = PhysicalOpeningCutTargetStateCodec.Resolve(
                fixture.Project,
                fixture.Host,
                new[] { fixture.Opening.Id });

            Equal(1, resolved.Count);
            if (!ReferenceEquals(fixture.Opening, resolved[0]))
                throw new Exception("Physical opening target resolution must return the exact project-owned opening instance.");
            Equal(beforeVersion, fixture.Project.ChangeVersion);
        }

        private static void DetachedSameIdHostFailsClosed()
        {
            var fixture = NewFixture();
            var detachedHost = new ProjectElement(fixture.Host.Id, fixture.Host.Category);
            var beforeVersion = fixture.Project.ChangeVersion;

            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Resolve(
                fixture.Project,
                detachedHost,
                new[] { fixture.Opening.Id }));

            Equal(beforeVersion, fixture.Project.ChangeVersion);
            if (!ReferenceEquals(fixture.Host, fixture.Project.FindElement(fixture.Host.Id)))
                throw new Exception("Rejected detached host must not replace the canonical project host.");
        }

        private static void MissingHostFailsClosed()
        {
            var fixture = NewFixture();
            var missingHost = new ProjectElement("WALL-MISSING", ElementCategory.ArchitecturalWall);
            var beforeVersion = fixture.Project.ChangeVersion;

            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Resolve(
                fixture.Project,
                missingHost,
                new[] { fixture.Opening.Id }));

            Equal(beforeVersion, fixture.Project.ChangeVersion);
        }

        private static Fixture NewFixture()
        {
            var project = new ProjectState("P-OPENING-OWNER", "Opening owner binding");
            var host = new ProjectElement("WALL-1", ElementCategory.ArchitecturalWall);
            var opening = new ProjectElement("OPEN-1", ElementCategory.WallOpening);
            opening.Properties["HostWallId"] = host.Id;
            project.Elements.Add(host);
            project.Elements.Add(opening);
            return new Fixture(project, host, opening);
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ProjectElement host, ProjectElement opening)
            {
                Project = project;
                Host = host;
                Opening = opening;
            }

            public ProjectState Project { get; }
            public ProjectElement Host { get; }
            public ProjectElement Opening { get; }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class PhysicalOpeningCutTargetStateOwnerBindingSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => PhysicalOpeningCutTargetStateOwnerBindingSmoke.Run();
    }
}
