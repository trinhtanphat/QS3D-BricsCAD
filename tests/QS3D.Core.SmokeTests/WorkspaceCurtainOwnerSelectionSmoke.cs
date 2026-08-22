using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkspaceCurtainOwnerSelectionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PanelFrameAndLegacyReferencesResolveTheExistingFamily();
            UnknownAndAmbiguousPanelOwnershipFailClosed();
            MultipleReferencesCollapseToOneCanonicalOwner();
        }

        private static void PanelFrameAndLegacyReferencesResolveTheExistingFamily()
        {
            var project = Project(out var family, out var curtain);

            foreach (var handle in new[] { "A1", "A2", "A3", "B1", "C1" })
            {
                var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { handle });
                if (resolved.Count != 1 || !ReferenceEquals(resolved[0], curtain))
                    throw new Exception("Workspace Curtain reference did not resolve the canonical GlassWall owner: " + handle + ".");
                if (!ReferenceEquals(project.FindFamily(resolved[0].FamilyId), family))
                    throw new Exception("Workspace Curtain owner did not retain its existing canonical Family: " + handle + ".");
            }
        }

        private static void UnknownAndAmbiguousPanelOwnershipFailClosed()
        {
            var project = Project(out _, out _);
            if (SemanticHandleOwnershipResolver.Resolve(project, new[] { "FFFF" }).Count != 0)
                throw new Exception("Unknown CAD reference unexpectedly resolved to a semantic owner.");

            var competing = new ProjectElement("CURTAIN-OTHER", ElementCategory.GlassWall, "CURTAIN-FAMILY", string.Empty, string.Empty);
            competing.Properties["GeneratedCurtainPanelHandles"] = "C1";
            project.Elements.Add(competing);
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "C1" }));
        }

        private static void MultipleReferencesCollapseToOneCanonicalOwner()
        {
            var project = Project(out _, out var curtain);
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "B1", "C1" });
            if (resolved.Count != 1 || !ReferenceEquals(resolved[0], curtain))
                throw new Exception("Canonical generated-owner resolution did not collapse frame/panel references to one GlassWall.");
        }

        private static ProjectState Project(out ProjectFamily family, out ProjectElement curtain)
        {
            var project = new ProjectState("workspace-curtain-selection", "Workspace Curtain Selection");
            family = new ProjectFamily("CURTAIN-FAMILY", "Curtain Family", ElementCategory.GlassWall);
            project.Families.Add(family);

            curtain = new ProjectElement("CURTAIN", ElementCategory.GlassWall, family.Id, string.Empty, string.Empty);
            curtain.SourceHandles.Add("A1");
            curtain.Properties["GeneratedSolidHandle"] = "A2";
            curtain.Properties["PhysicalOpeningCutSolidHandle"] = "A3";
            curtain.Properties["GeneratedCurtainFrameHandles"] = "B1;B2";
            curtain.Properties["GeneratedCurtainPanelHandles"] = "C1;C2";
            project.Elements.Add(curtain);
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
