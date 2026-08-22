using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionLayerMappingEnumerationFreshnessSmoke
    {
        private const string WallLayer = "L-WALL";
        private const string BeamLayer = "L-BEAM";

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsLayerMappingValueMutation();
            RejectsLayerMappingAddition();
            RejectsLayerMappingRemoval();
            PreservesUnchangedLayerMappings();
            IgnoresUnrelatedMetadataMutation();
        }

        private static void RejectsLayerMappingValueMutation()
        {
            var project = CreateProject();
            var version = project.ChangeVersion;
            ThrowsInvalidOperation(() => new ProjectRecognitionService().SuggestBatch(
                project,
                MutatingSnapshots(project, () => SetPersistenceMetadata(project, MappingKey(WallLayer), ElementCategory.Beam.ToString()))));
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Recognition layer-mapping freshness persistence fixture unexpectedly advanced ChangeVersion.");
        }

        private static void RejectsLayerMappingAddition()
        {
            var project = CreateProject();
            var version = project.ChangeVersion;
            ThrowsInvalidOperation(() => new ProjectRecognitionService().SuggestBatch(
                project,
                MutatingSnapshots(project, () => SetPersistenceMetadata(project, MappingKey(BeamLayer), ElementCategory.Beam.ToString()))));
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Recognition layer-mapping addition persistence fixture unexpectedly advanced ChangeVersion.");
        }

        private static void RejectsLayerMappingRemoval()
        {
            var project = CreateProject();
            var version = project.ChangeVersion;
            ThrowsInvalidOperation(() => new ProjectRecognitionService().SuggestBatch(
                project,
                MutatingSnapshots(project, () => RemovePersistenceMetadata(project, MappingKey(WallLayer)))));
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Recognition layer-mapping removal persistence fixture unexpectedly advanced ChangeVersion.");
        }

        private static void PreservesUnchangedLayerMappings()
        {
            var project = CreateProject();
            var batch = new ProjectRecognitionService().SuggestBatch(
                project,
                new[] { new EntitySnapshot("R4", "Line", WallLayer) });
            if (batch.Results.Count != 1 || batch.Results[0].TopCandidate == null ||
                batch.Results[0].TopCandidate!.Category != ElementCategory.ArchitecturalWall ||
                !batch.Results[0].TopCandidate!.RuleId.StartsWith("project-layer:", StringComparison.Ordinal))
                throw new InvalidOperationException("Unchanged project layer mappings no longer remain authoritative in recognition batches.");
        }

        private static void IgnoresUnrelatedMetadataMutation()
        {
            var project = CreateProject();
            var version = project.ChangeVersion;
            var batch = new ProjectRecognitionService().SuggestBatch(
                project,
                MutatingSnapshots(project, () => SetPersistenceMetadata(project, "RecognitionFreshness.Unrelated", "changed")));
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Recognition unrelated-metadata persistence fixture unexpectedly advanced ChangeVersion.");
            if (batch.Results.Count != 1 || batch.Results[0].TopCandidate == null ||
                batch.Results[0].TopCandidate!.Category != ElementCategory.ArchitecturalWall)
                throw new InvalidOperationException("Layer-mapping freshness guard widened unexpectedly to unrelated metadata.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("recognition-layer-freshness", "Recognition layer freshness");
            project.Metadata[MappingKey(WallLayer)] = ElementCategory.ArchitecturalWall.ToString();
            return project;
        }

        private static string MappingKey(string layer) => TemplateProfileStore.LayerMappingPrefix + layer;

        private static IEnumerable<EntitySnapshot> MutatingSnapshots(ProjectState project, Action mutation)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            mutation();
            yield return new EntitySnapshot("R1", "Line", WallLayer);
        }

        private static void SetPersistenceMetadata(ProjectState project, string key, string value)
        {
            var method = project.Metadata.GetType().GetMethod(
                "SetPersistenceValue",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Project metadata persistence setter is unavailable.");
            method.Invoke(project.Metadata, new object[] { key, value });
        }

        private static void RemovePersistenceMetadata(ProjectState project, string key)
        {
            var method = project.Metadata.GetType().GetMethod(
                "RemoveOwned",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Project metadata persistence remover is unavailable.");
            if (!(bool)(method.Invoke(project.Metadata, new object[] { key }) ?? false))
                throw new InvalidOperationException("Recognition freshness fixture expected persisted layer metadata removal.");
        }

        private static void ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("recognition", StringComparison.OrdinalIgnoreCase) ||
                    !ex.Message.Contains("enumerat", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Recognition freshness failed with an unexpected diagnostic.", ex);
                return;
            }
            throw new InvalidOperationException("Expected recognition batch to reject a layer-mapping mutation during snapshot enumeration.");
        }
    }
}
