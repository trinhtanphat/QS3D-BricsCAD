using System;
using System.IO;
using System.Linq;
using QS3D.Core.Agent.Harness;

namespace QS3D.Code.Cli
{
    public sealed class ConsoleTraceRenderer
    {
        public void Render(HarnessExecutionSnapshot snapshot, TextWriter output)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.WriteLine("trace:");
            foreach (var item in snapshot.Trace.OrderBy(value => value.Sequence))
            {
                output.Write("- ");
                output.Write(item.Sequence);
                output.Write(":");
                output.Write(item.Kind);
                output.Write(" ");
                output.WriteLine(item.Summary);

                foreach (var metadata in item.Metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    output.WriteLine("  " + metadata.Key + "=" + metadata.Value);
            }
        }
    }
}
