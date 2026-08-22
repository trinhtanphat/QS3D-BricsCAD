using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarProviderOwnershipSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BeamStirrupLaterOwnerIsConflict();
            TieLaterOwnerIsConflict();
            LongitudinalRebarLaterOwnerIsConflict();
            OwnershipPolicyFailsClosedAndIndexIgnoresNullEntries();
        }

        private static void BeamStirrupLaterOwnerIsConflict()
        {
            var project = ProjectWithNull("beam");
            var beam = new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            beam.Properties["GeneratedBeamStirrupHandles"] = "AA";
            beam.Properties["GeneratedBeamStirrupCount"] = "1";
            beam.Properties["GeneratedBeamStirrupDiameterMm"] = "8";
            beam.Properties["GeneratedBeamStirrupMode"] = "Beam.Line.RectangularClosedLoop";
            project.Elements.Add(beam);
            project.Elements.Add(FutureOwner("F1", "AA"));

            var issues = new GeneratedBeamStirrupHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT" && x.ElementId == beam.Id),
                "beam stirrup later-owner conflict was missed");
        }

        private static void TieLaterOwnerIsConflict()
        {
            var project = ProjectWithNull("tie");
            var column = new ProjectElement("C1", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            column.Properties["GeneratedTieRebarHandles"] = "AB";
            column.Properties["GeneratedTieRebarCount"] = "1";
            column.Properties["GeneratedTieRebarDiameterMm"] = "8";
            column.Properties["GeneratedTieRebarActualSpacingM"] = "0.15";
            project.Elements.Add(column);
            project.Elements.Add(FutureOwner("F2", "AB"));

            var issues = new GeneratedTieRebarHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT" && x.ElementId == column.Id),
                "tie later-owner conflict was missed");
        }

        private static void LongitudinalRebarLaterOwnerIsConflict()
        {
            var project = ProjectWithNull("rebar");
            var column = new ProjectElement("C2", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            column.Properties["GeneratedRebarHandles"] = "AC";
            column.Properties["GeneratedRebarCount"] = "1";
            column.Properties["GeneratedRebarDiameterMm"] = "16";
            project.Elements.Add(column);
            project.Elements.Add(FutureOwner("F3", "AC"));

            var issues = new GeneratedRebarHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "REBAR_GENERATED_OWNERSHIP_CONFLICT" && x.ElementId == column.Id),
                "longitudinal rebar later-owner conflict was missed");
        }

        private static void OwnershipPolicyFailsClosedAndIndexIgnoresNullEntries()
        {
            var project = ProjectWithNull("index");
            var owner = new ProjectElement("O1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            owner.Properties["GeneratedFutureOwnershipHandle"] = "AD";
            project.Elements.Add(owner);

            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project));
            Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "AD", out _, out _));

            var index = GeneratedHandleOwnershipIndex.Build(project);
            Require(index.TryFindOwner("AD", out var found, out _) && ReferenceEquals(found, owner),
                "ownership index failed to resolve unique owner after null semantic entry");
        }

        private static ProjectState ProjectWithNull(string id)
        {
            var project = new ProjectState("P-" + id, id);
            project.Elements.Add(null!);
            return project;
        }

        private static ProjectElement FutureOwner(string id, string handle)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedFutureOwnershipHandles"] = handle;
            return element;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("GeneratedRebarProviderOwnershipSmoke: " + message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("GeneratedRebarProviderOwnershipSmoke: expected " + typeof(T).Name + ".");
        }

    }
}
