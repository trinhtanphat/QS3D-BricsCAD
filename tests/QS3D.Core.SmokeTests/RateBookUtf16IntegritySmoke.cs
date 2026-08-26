using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookUtf16IntegritySmoke
    {
        internal static void Run()
        {
            MalformedSurrogatesFailClosedAcrossCanonicalTokens();
            ValidSupplementaryUnicodeIsPreserved();
        }

        private static void MalformedSurrogatesFailClosedAcrossCanonicalTokens()
        {
            var high = "\uD800";
            var low = "\uDC00";
            var utc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Throws<ArgumentException>(() => new CostCode("CONC" + high));
            Throws<ArgumentException>(() => new CostCode("CONC" + low));
            Throws<ArgumentException>(() => new RateBook("BOOK" + high, Array.Empty<RateItem>()));
            Throws<ArgumentException>(() => new RateBook("BOOK" + low, Array.Empty<RateItem>()));

            Throws<ArgumentException>(() => new RateItem("RATE" + high, new CostCode("CONC"), "m3", "VND", 1m, utc, "v1"));
            Throws<ArgumentException>(() => new RateItem("RATE", new CostCode("CONC"), "m3" + low, "VND", 1m, utc, "v1"));
            Throws<ArgumentException>(() => new RateItem("RATE", new CostCode("CONC"), "m3", "VN" + high, 1m, utc, "v1"));
            Throws<ArgumentException>(() => new RateItem("RATE", new CostCode("CONC"), "m3", "VND", 1m, utc, "v1" + low));
        }

        private static void ValidSupplementaryUnicodeIsPreserved()
        {
            var supplementary = char.ConvertFromUtf32(0x1F680);
            var utc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var costCode = new CostCode("CONC" + supplementary);
            var item = new RateItem(
                "RATE" + supplementary,
                costCode,
                "m3",
                "VND",
                1m,
                utc,
                "v1" + supplementary);
            var book = new RateBook("BOOK" + supplementary, new[] { item });

            Equal("CONC" + supplementary, costCode.Value, "Supplementary CostCode text must remain exact.");
            Equal("BOOK" + supplementary, book.RateBookId, "Supplementary RateBook identity must remain exact.");
            Equal("RATE" + supplementary, book.Items[0].RateItemId, "Supplementary RateItem identity must remain exact.");
            Equal("v1" + supplementary, book.Items[0].Version, "Supplementary RateItem version must remain exact.");

            var resolved = book.Resolve(new CostCode("conc" + supplementary), "m3", "VND", utc);
            True(resolved.IsMatched, "Valid supplementary CostCode identity must remain resolvable.");
            Equal("RATE" + supplementary, resolved.Item!.RateItemId, "Resolved supplementary identity must remain exact.");
        }

        private static void Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }

    internal static class RateBookUtf16IntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookUtf16IntegritySmoke.Run();
        }
    }
}