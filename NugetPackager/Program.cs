using Vstack.Extensions;

namespace NugetPackager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            args.ValidateNotNullParameter(nameof(args));
        }
    }
}
