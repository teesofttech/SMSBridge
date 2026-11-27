using System.Diagnostics;
using System.Reflection;
using SmsBridge.Observability;

namespace SmsBridge.UnitTests;

public sealed class TelemetryTests
{
    [Fact]
    public void LegacyVersion_RemainsAvailableAsAConstant()
    {
        const string version = SmsBridgeTelemetry.Version;

        version.Should().Be("0.1.0");
    }

    [Fact]
    public void ActivitySource_Version_MatchesAssemblyInformationalVersionWithoutMetadata()
    {
        var expected = typeof(SmsBridgeTelemetry).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion
            .Split('+')[0];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SmsBridgeTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = SmsBridgeTelemetry.StartSendActivity("test");

        activity.Should().NotBeNull();
        activity!.Source.Version.Should().Be(expected);
        activity.Source.Version.Should().NotContain("+");
    }
}
