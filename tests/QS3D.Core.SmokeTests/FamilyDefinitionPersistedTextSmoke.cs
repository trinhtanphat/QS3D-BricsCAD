using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyDefinitionPersistedTextSmoke
    {
        public static void Run()
        {
            PreservesExistingCanonicalizationAndUnicode();
            RejectsControlCharactersAtConstruction();
            RejectsXmlInvalidTextAtConstruction();
            RejectedSetterMutationIsAtomic();
        }

        private static void PreservesExistingCanonicalizationAndUnicode()
        {
            var family = new FamilyDefinition("  Cửa đi 😀  ", ElementCategory.Door, "  Gỗ óc chó 😀  ");
            Equal("Cửa đi 😀", family.Name, "Family name trim/Unicode semantics changed.");
            Equal("Gỗ óc chó 😀", family.Material, "Family material trim/Unicode semantics changed.");

            family.Material = " \t\r\n ";
            Equal("Khác", family.Material, "Blank family material must keep the existing default semantics.");
        }

        private static void RejectsControlCharactersAtConstruction()
        {
            Throws<ArgumentException>(() => new FamilyDefinition("Door\n01", ElementCategory.Door));
            Throws<ArgumentException>(() => new FamilyDefinition("Door\t01", ElementCategory.Door));
            Throws<ArgumentException>(() => new FamilyDefinition("Door\0" + "01", ElementCategory.Door));
            Throws<ArgumentException>(() => new FamilyDefinition("Door", ElementCategory.Door, "Steel\rGrade"));
        }

        private static void RejectsXmlInvalidTextAtConstruction()
        {
            Throws<ArgumentException>(() => new FamilyDefinition("Bad" + '\uD800', ElementCategory.Door));
            Throws<ArgumentException>(() => new FamilyDefinition("Bad" + '\uFFFF', ElementCategory.Door));
            Throws<ArgumentException>(() => new FamilyDefinition("Door", ElementCategory.Door, "Bad" + '\uDFFF'));
            Throws<ArgumentException>(() => new FamilyDefinition("Door", ElementCategory.Door, "Bad" + '\uFFFE'));
        }

        private static void RejectedSetterMutationIsAtomic()
        {
            var family = new FamilyDefinition("Door 01", ElementCategory.Door, "Steel");

            Throws<ArgumentException>(() => family.Name = "Corrupt\nName");
            Equal("Door 01", family.Name, "Rejected family name mutation changed the previous valid value.");

            Throws<ArgumentException>(() => family.Name = "Corrupt" + '\uD800');
            Equal("Door 01", family.Name, "Rejected XML-invalid family name mutation changed the previous valid value.");

            Throws<ArgumentException>(() => family.Material = "Corrupt\tMaterial");
            Equal("Steel", family.Material, "Rejected family material mutation changed the previous valid value.");

            Throws<ArgumentException>(() => family.Material = "Corrupt" + '\uFFFF');
            Equal("Steel", family.Material, "Rejected XML-invalid family material mutation changed the previous valid value.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
