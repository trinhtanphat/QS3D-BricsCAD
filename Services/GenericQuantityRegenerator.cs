using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class GenericQuantityRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.CustomQuantity;
        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetQuantity("Count", 1d);
            var length = SemanticNumber.Get(element, "LengthM"); if (length > 0d) element.SetQuantity("LengthM", length);
            var area = SemanticNumber.Get(element, "AreaM2"); if (area > 0d) element.SetQuantity("AreaM2", area);
            var volume = SemanticNumber.Get(element, "VolumeM3"); if (volume > 0d) element.SetQuantity("VolumeM3", volume);
        }
    }
}
