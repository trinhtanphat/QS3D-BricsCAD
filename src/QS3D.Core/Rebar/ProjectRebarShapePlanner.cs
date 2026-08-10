using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Rebar
{
    public sealed class ProjectRebarShapePlan
    {
        public string ElementId { get; set; } = string.Empty;
        public string BarMark { get; set; } = string.Empty;
        public double DiameterMm { get; set; }
        public int Quantity { get; set; }
        public RebarShapePath Path { get; set; } = null!;
    }

    public static class ProjectRebarShapePlanner
    {
        public static IReadOnlyList<ProjectRebarShapePlan> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var rows = ProjectRebarScheduleBuilder.Build(project);
            var elements = project.Elements.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var result = new List<ProjectRebarShapePlan>(rows.Count);
            foreach (var row in rows)
            {
                if (!elements.TryGetValue(row.ElementId, out var element))
                    throw new InvalidOperationException("BBS row references a missing project element: " + row.ElementId);
                element.Properties.TryGetValue("RebarShapeLegsM", out var legs);
                element.Properties.TryGetValue("RebarShapeTurnsDeg", out var turns);
                var path = RebarShapePathBuilder.Build(row.ShapeCode, row.CuttingLengthM, legs, turns);
                result.Add(new ProjectRebarShapePlan
                {
                    ElementId = row.ElementId,
                    BarMark = row.BarMark,
                    DiameterMm = row.DiameterMm,
                    Quantity = row.Quantity,
                    Path = path
                });
            }
            return result.AsReadOnly();
        }
    }
}
