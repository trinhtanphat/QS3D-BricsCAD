using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverCanonicalDirectSmoke
    {
        internal static void Run()
        {
            CanonicalDirectSourceRemainsAuthoritative();
            PaddedDirectSourceFailsBeforeGeneratedFallback();
            BlankDirectSourceFailsBeforeGeneratedFallback();
        }

        private static void CanonicalDirectSourceRemainsAuthoritative()
        {
            var project = NewProject("SOURCE-A");
            var element = project.Elements[0];
            var beforeVersion = project.ChangeVersion;

            var handles = SourceHandleResolver.Resolve(project, new[] { element.Id });
            Equal(1, handles.Count);
            Equal("SOURCE-A", handles[0]);
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void PaddedDirectSourceFailsBeforeGeneratedFallback()
        {
            var project = NewPersistedProject(" SOURCE-A ");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(project, new[] { "E-1" }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void BlankDirectSourceFailsBeforeGeneratedFallback()
        {
            var project = NewPersistedProject("   ");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(project, new[] { "E-1" }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static ProjectState NewProject(string directHandle)
        {
            var project = new ProjectState("P-LOCATE-SOURCE", "Locate source canonicality");
            var element = new ProjectElement("E-1", ElementCategory.Room);
            element.SourceHandles.Add(directHandle);
            element.Properties["GeneratedTieRebarHandles"] = "GENERATED-FALLBACK";
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState NewPersistedProject(string directHandle)
        {
            var project = new ProjectState("P-LOCATE-SOURCE", "Locate source canonicality");
            var element = new ProjectElement("E-1", ElementCategory.Room);
            var valuesField = element.SourceHandles.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed malformed persisted source-handle state.");
            var values = valuesField.GetValue(element.SourceHandles) as List<string>
                ?? throw new InvalidOperationException("Unexpected ProjectElement.SourceHandles backing collection.");
            values.Add(directHandle);
            element.Properties["GeneratedTieRebarHandles"] = "GENERATED-FALLBACK";
            project.Elements.Add(element);
            return project;
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

    internal static class SourceHandleResolverCanonicalDirectSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SourceHandleResolverCanonicalDirectSmoke.Run();
    }
}
