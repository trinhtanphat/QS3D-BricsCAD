using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Extensions
{
    public interface IElementModule
    {
        string Id { get; }
        IReadOnlyCollection<ElementCategory> Categories { get; }
        void Validate(ProjectState project, ProjectElement element, IList<string> errors);
        void Regenerate(ProjectState project, ProjectElement element);
    }
}
