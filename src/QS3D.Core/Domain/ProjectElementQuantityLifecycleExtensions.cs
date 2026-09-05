using System;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    /// <summary>
    /// Lifecycle-aware quantity mutations that complement ProjectElement.SetQuantity
    /// without exposing raw dictionary removal to adapter callers.
    /// </summary>
    public static class ProjectElementQuantityLifecycleExtensions
    {
        public static bool RemoveQuantity(this ProjectElement element, string name)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Quantity name is required.", nameof(name));
            if (name.Any(char.IsControl)) throw new ArgumentException("Quantity name cannot contain control characters.", nameof(name));

            var key = name.Trim();
            try
            {
                XmlConvert.VerifyXmlChars(key);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Quantity name contains characters that are invalid in XML.", nameof(name), ex);
            }

            if (!element.Quantities.Remove(key)) return false;
            element.MarkDirty(ElementDirtyFlags.Quantity);
            return true;
        }
    }
}
