using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnershipSafetySmoke
    {
        internal static void Run()
        {
            ValidOwnershipStillResolvesDeterministically();
            NullElementFailsClosed();
            DuplicateElementIdsFailClosed();
        }

        private static void ValidOwnershipStillResolvesDeterministically()
        {
            var project = new ProjectState("P", "Project");
            var first = new ProjectElement("E1", ElementCategory.Beam);
            first.Properties["GeneratedSolidHandle"] = "AA11";
            var second = new ProjectElement("E2", ElementCategory.Beam);
            second.Properties["GeneratedRebarHandles"] = "CC33;BB22";
            project.Elements.Add(first);
            project.Elements.Add(second);

            Equal("AA11|BB22|CC33", string.Join("|", GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)));
            True(GeneratedHandleOwnershipPolicy.TryFindOwner(project, "bb22", out var owner, out var propertyKey));
            True(ReferenceEquals(second, owner));
            Equal("GeneratedRebarHandles", propertyKey);
        }

        private static void NullElementFailsClosed()
        {
            var project = new ProjectState("P", "Project");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA11";
            project.Elements.Add(element);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project));
            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "UNOWNED", out _, out _));
        }

        private static void DuplicateElementIdsFailClosed()
        {
            var project = new ProjectState("P", "Project");
            var first = new ProjectElement("E1", ElementCategory.Beam);
            first.Properties["GeneratedSolidHandle"] = "AA11";
            var duplicate = new ProjectElement("E1", ElementCategory.Beam);
            duplicate.Properties["GeneratedRebarHandles"] = "BB22";
            project.Elements.Add(first);
            project.Elements.Add(duplicate);

            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project));
            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "UNOWNED", out _, out _));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class GeneratedHandleOwnershipSafetySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GeneratedHandleOwnershipSafetySmoke.Run();
    }
}
