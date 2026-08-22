using System;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationConstructorIntegritySmoke
    {
        internal static void Run()
        {
            Throws<ArgumentNullException>(() =>
                new RegenerationEngine(null!, Array.Empty<IElementRegenerator>()));

            Throws<ArgumentNullException>(() =>
                new RegenerationEngine(new DependencyGraph(), null!));

            Throws<ArgumentException>(() =>
                new RegenerationEngine(
                    new DependencyGraph(),
                    new IElementRegenerator[] { null! }));

            _ = new RegenerationEngine(
                new DependencyGraph(),
                Array.Empty<IElementRegenerator>());
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
