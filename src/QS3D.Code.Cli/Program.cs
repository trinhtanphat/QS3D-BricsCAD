using System;

namespace QS3D.Code.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return new Qs3dCliApplication().Run(args, Console.Out, Environment.CurrentDirectory);
        }
    }
}
