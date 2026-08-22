using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Rebar
{
    public sealed class RebarRegenerator : IElementRegenerator
    {
        private readonly RebarScheduleBuilder _builder = new RebarScheduleBuilder();
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Rebar;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var row = _builder.BuildElement(element);
            element.SetQuantity("Count", row.Quantity);
            element.SetQuantity("CutLengthM", row.CutLengthM);
            element.SetQuantity("TotalLengthM", row.TotalLengthM);
            element.SetQuantity("UnitWeightKgPerM", row.UnitWeightKgPerM);
            element.SetQuantity("SteelWeightKg", row.TotalWeightKg);
        }
    }
}
