using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            UnrelatedAmbiguityDoesNotBlockCleanSelection();
            SelectedAmbiguityIsRejected();
            GeneratedMultiHandleResolvesOwner();
        }

        private static void UnrelatedAmbiguityDoesNotBlockCleanSelection()
        {
            var project = Project();
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "AA" });
            if (resolved.Count != 1 || resolved[0].Id != "A")
                throw new Exception("Unrelated ownership ambiguity blocked or changed a clean semantic selection.");
        }

        private static void SelectedAmbiguityIsRejected()
        {
            var project = Project();
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "BB" }));
        }

        private static void GeneratedMultiHandleResolvesOwner()
        {
            var project = Project();
            var curtain = new ProjectElement("CW", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            curtain.Properties["GeneratedCurtainFrameHandles"] = "C1; C2 ; C2";
            project.Elements.Add(curtain);

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "c2" });
            if (resolved.Count != 1 || resolved[0].Id != "CW")
                throw new Exception("Generated multi-handle selection did not resolve its semantic owner.");
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("selection-safety", "Selection Safety");
            var a = new ProjectElement("A", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            a.SourceHandles.Add("AA");
            var b = new ProjectElement("B", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            b.SourceHandles.Add("BB");
            var c = new ProjectElement("C", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            c.Properties["GeneratedSolidHandle"] = "BB";
            project.Elements.Add(a);
            project.Elements.Add(b);
            project.Elements.Add(c);
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
