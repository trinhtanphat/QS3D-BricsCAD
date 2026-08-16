using System;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileIdentityTextSmoke
    {
        internal static void Run()
        {
            CanonicalVietnameseAndSupplementaryUnicodeRemainAccepted();
            SurroundingSpacesRemainNormalized();
            ConstructorRejectsControlCharacters();
            ConstructorRejectsXmlInvalidText();
            MutableNameRejectsInvalidTextAtomically();
            WhitespaceOnlyIdentityRemainsRejected();
        }

        private static void CanonicalVietnameseAndSupplementaryUnicodeRemainAccepted()
        {
            var profile = new TemplateProfile("MẪU-🏗️-01", "Mẫu kết cấu 🏢");
            Equal("MẪU-🏗️-01", profile.Id, "Valid Unicode template id changed.");
            Equal("Mẫu kết cấu 🏢", profile.Name, "Valid Unicode template name changed.");
            profile.Name = "Mẫu kiến trúc 🏠";
            Equal("Mẫu kiến trúc 🏠", profile.Name, "Valid Unicode mutable name changed.");
        }

        private static void SurroundingSpacesRemainNormalized()
        {
            var profile = new TemplateProfile("  TEMPLATE-01  ", "  Mẫu chuẩn  ");
            Equal("TEMPLATE-01", profile.Id, "Template id trim semantics changed.");
            Equal("Mẫu chuẩn", profile.Name, "Template name trim semantics changed.");
            profile.Name = "  Tên cập nhật  ";
            Equal("Tên cập nhật", profile.Name, "Mutable template name trim semantics changed.");
        }

        private static void ConstructorRejectsControlCharacters()
        {
            foreach (var control in new[] { '\0', '\u0001', '\t', '\n', '\u001F', '\u007F', '\u0085', '\u009F' })
            {
                Throws<ArgumentException>(() => new TemplateProfile("ID" + control + "X", "Valid"));
                Throws<ArgumentException>(() => new TemplateProfile("VALID-ID", "Name" + control + "X"));
            }
        }

        private static void ConstructorRejectsXmlInvalidText()
        {
            var loneHighSurrogate = "ID-" + new string(new[] { '\uD800' });
            var loneLowSurrogate = "Name-" + new string(new[] { '\uDC00' });
            var nonCharacter = "ID-" + new string(new[] { '\uFFFE' });
            Throws<ArgumentException>(() => new TemplateProfile(loneHighSurrogate, "Valid"));
            Throws<ArgumentException>(() => new TemplateProfile("VALID-ID", loneLowSurrogate));
            Throws<ArgumentException>(() => new TemplateProfile(nonCharacter, "Valid"));
        }

        private static void MutableNameRejectsInvalidTextAtomically()
        {
            var profile = new TemplateProfile("TEMPLATE-ATOMIC", "Tên ban đầu");
            Throws<ArgumentException>(() => profile.Name = "Tên\nkhông hợp lệ");
            Equal("Tên ban đầu", profile.Name, "Rejected control-character name mutated existing state.");
            var loneSurrogate = "Tên " + new string(new[] { '\uD800' });
            Throws<ArgumentException>(() => profile.Name = loneSurrogate);
            Equal("Tên ban đầu", profile.Name, "Rejected invalid UTF-16 name mutated existing state.");
            var nonCharacter = "Tên " + new string(new[] { '\uFFFF' });
            Throws<ArgumentException>(() => profile.Name = nonCharacter);
            Equal("Tên ban đầu", profile.Name, "Rejected XML noncharacter name mutated existing state.");
        }

        private static void WhitespaceOnlyIdentityRemainsRejected()
        {
            Throws<ArgumentException>(() => new TemplateProfile("   ", "Valid"));
            Throws<ArgumentException>(() => new TemplateProfile("VALID-ID", "   "));
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message + " Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + " was not thrown.");
        }
    }
}
