using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementCategoryIntegritySmoke
    {
        public static void Run()
        {
            ConstructorRejectsUndefinedCategory();
            SetterRejectsUndefinedCategoryWithoutMutation();
        }

        private static void ConstructorRejectsUndefinedCategory()
        {
            var invalid = (ElementCategory)int.MaxValue;
            Throws<ArgumentOutOfRangeException>(() =>
                new ProjectElement("BAD", invalid, string.Empty, string.Empty, string.Empty));
        }

        private static void SetterRejectsUndefinedCategoryWithoutMutation()
        {
            var element = new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var invalid = (ElementCategory)int.MaxValue;

            Throws<ArgumentOutOfRangeException>(() => element.Category = invalid);
            if (element.Category != ElementCategory.Room)
                throw new Exception("Rejected ProjectElement category assignment mutated the previous valid category.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
