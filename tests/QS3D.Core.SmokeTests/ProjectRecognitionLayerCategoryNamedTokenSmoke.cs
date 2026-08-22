using System;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRecognitionLayerCategoryNamedTokenSmoke
    {
        internal static void Run()
        {
            RejectsNumericCategoryAlias();
            AcceptsLowercaseNamedCategory();
        }

        private static void RejectsNumericCategoryAlias()
        {
            var project = Project("P-RECOG-NUMERIC");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-WALL"] = ((int)ElementCategory.ArchitecturalWall).ToString();
            var snapshot = new EntitySnapshot("1A", "line", "A-WALL");

            Throws<InvalidOperationException>(() => new ProjectRecognitionService().Suggest(project, snapshot));
        }

        private static void AcceptsLowercaseNamedCategory()
        {
            var project = Project("P-RECOG-NAMED");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-WALL"] = "architecturalwall";
            var snapshot = new EntitySnapshot("1B", "line", "A-WALL");

            var result = new ProjectRecognitionService().Suggest(project, snapshot);
            var top = result.TopCandidate ?? throw new InvalidOperationException("Expected project layer mapping recognition candidate.");
            if (top.Category != ElementCategory.ArchitecturalWall)
                throw new InvalidOperationException("Lowercase named layer category did not resolve to ArchitecturalWall.");
            if (!string.Equals(top.RuleId, "project-layer:A-WALL", StringComparison.Ordinal))
                throw new InvalidOperationException("Project layer mapping recognition rule identity changed unexpectedly.");
            if (Math.Abs(top.Confidence - 0.99d) > 1e-12d)
                throw new InvalidOperationException("Project layer mapping confidence changed unexpectedly.");
        }

        private static ProjectState Project(string id) => new ProjectState(id, "Recognition named token");

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
