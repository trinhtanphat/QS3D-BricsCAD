using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarOwnershipElementIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsDuplicateSemanticIdsBeforeOwnershipTokenCollapse();
            KeepsValidSingleOwnerClean();
        }

        private static void RejectsDuplicateSemanticIdsBeforeOwnershipTokenCollapse()
        {
            var project = new ProjectState("P-REBAR-OWNER-ID", "Generated rebar ownership element integrity");
            var first = new ProjectElement("E1", ElementCategory.Beam);
            var second = new ProjectElement("e1", ElementCategory.Column);
            first.Properties["GeneratedRebarHandles"] = "AA";
            second.Properties["GeneratedRebarHandles"] = "AA";
            project.Elements.Add(first);
            project.Elements.Add(second);

            Throws<InvalidOperationException>(
                () => new GeneratedRebarOwnershipHealthService().Inspect(project),
                "case-insensitive duplicate semantic ids");
        }

        private static void KeepsValidSingleOwnerClean()
        {
            var project = new ProjectState("P-REBAR-OWNER-VALID", "Generated rebar ownership valid control");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["GeneratedRebarHandles"] = "AA";
            project.Elements.Add(element);

            var issues = new GeneratedRebarOwnershipHealthService().Inspect(project);
            Equal(0, issues.Count, "valid single-owner issue count");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("GeneratedRebarOwnershipElementIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("GeneratedRebarOwnershipElementIntegritySmoke " + label + ": expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("GeneratedRebarOwnershipElementIntegritySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
