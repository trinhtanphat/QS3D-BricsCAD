using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class SemanticPropertyEditPolicy
    {
        private static readonly HashSet<string> SourceDerivedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LengthM",
            "AreaM2",
            "VolumeM3",
            "PerimeterM",
            "Layer",
            MeasuredSolidQuantityPolicy.VolumeProperty,
            MeasuredSolidQuantityPolicy.SurfaceAreaProperty
        };

        private static readonly HashSet<string> ReservedIdentityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "ElementId",
            "Category",
            "FamilyId",
            "FloorId",
            "ZoneId"
        };

        public static bool IsEditablePropertyKey(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return false;
            return EditBlockReason(propertyName.Trim()) == null;
        }

        internal static string RequireEditablePropertyKey(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Property name is required.", nameof(propertyName));
            var key = propertyName.Trim();
            var blockReason = EditBlockReason(key);
            if (blockReason != null) throw new InvalidOperationException(blockReason + key + ".");
            return key;
        }

        private static string EditBlockReason(string key)
        {
            if (SourceDerivedKeys.Contains(key) || key.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase))
                return "Property is derived from CAD/source geometry and cannot be edited as a generic semantic property: ";
            if (ReservedIdentityKeys.Contains(key) || LooksLikeIdentityReferenceKey(key))
                return "Semantic identity/reference field cannot be edited as a generic property: ";
            if (key.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase))
                return "Native/generated ownership state cannot be edited as a generic semantic property: ";
            return null;
        }

        private static bool LooksLikeIdentityReferenceKey(string key)
        {
            return key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Ids", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Ref", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Refs", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("RefId", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("RefIds", StringComparison.OrdinalIgnoreCase);
        }
    }
}
