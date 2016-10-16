using System.Diagnostics.CodeAnalysis;

namespace NugetPackager
{
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "Zero does not make sense for TimeUnit.")]
    public enum TimeUnit
    {
        Minute = 60000,
        Second = 1000,
        Millisecond = 1
    }
}