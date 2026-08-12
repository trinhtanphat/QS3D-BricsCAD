using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkDependencyCycleSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TransitiveCycleFailsBeforeMutation();
            AcyclicHostLinkStillSucceeds();
        }

        private static void TransitiveCycleFailsBeforeMutation()
        {
            var project = new ProjectState("P-HOST-CYCLE", "Host dependency cycle");
            var opening = new ProjectElement("OPEN-1", ElementCategory.Door, string.Empty, string.Empty, string.Empty);
            var bridge = new ProjectElement("MID-1", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            var wall = new ProjectElement("WALL-1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            bridge.DependsOn.Add(opening.Id);
            wall.DependsOn.Add(bridge.Id);
            project.Elements.Add(opening);
            project.Elements.Add(bridge);
            project.Elements.Add(wall);

            var beforeVersion = project.ChangeVersion;
            var beforeAuditCount = project.AuditEvents.Count;
            var failedClosed = false;
            try
            {
                new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            }
            catch (InvalidOperationException ex)
            {
                failedClosed = ex.Message.IndexOf("dependency cycle", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!failedClosed)
                throw new Exception("Host linking must fail closed when the target wall already depends transitively on the opening.");
            if (project.ChangeVersion != beforeVersion)
                throw new Exception("Host dependency-cycle preflight must fail before project revision mutation.");
            if (project.AuditEvents.Count != beforeAuditCount)
                throw new Exception("Host dependency-cycle preflight must fail before audit mutation.");
            if (opening.Properties.ContainsKey("HostWallId"))
                throw new Exception("Host dependency-cycle preflight must not persist HostWallId.");
            if (opening.DependsOn.Count != 0)
                throw new Exception("Host dependency-cycle preflight must not add the target wall dependency.");
            if (bridge.DependsOn.Count != 1 || !string.Equals(bridge.DependsOn[0], opening.Id, StringComparison.Ordinal) ||
                wall.DependsOn.Count != 1 || !string.Equals(wall.DependsOn[0], bridge.Id, StringComparison.Ordinal))
                throw new Exception("Host dependency-cycle preflight must preserve the existing dependency chain.");
        }

        private static void AcyclicHostLinkStillSucceeds()
        {
            var project = new ProjectState("P-HOST-ACYCLIC", "Acyclic host link");
            var opening = new ProjectElement("OPEN-2", ElementCategory.Door, string.Empty, string.Empty, string.Empty);
            var wall = new ProjectElement("WALL-2", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(opening);
            project.Elements.Add(wall);

            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);

            if (!opening.Properties.TryGetValue("HostWallId", out var hostId) ||
                !string.Equals(hostId, wall.Id, StringComparison.Ordinal))
                throw new Exception("Acyclic host linking must persist the canonical HostWallId.");
            if (opening.DependsOn.Count != 1 || !string.Equals(opening.DependsOn[0], wall.Id, StringComparison.Ordinal))
                throw new Exception("Acyclic host linking must persist exactly one canonical host dependency.");
        }
    }
}
