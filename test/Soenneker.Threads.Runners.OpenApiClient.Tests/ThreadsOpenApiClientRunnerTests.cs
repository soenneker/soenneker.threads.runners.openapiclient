using Soenneker.Tests.HostedUnit;

namespace Soenneker.Threads.Runners.OpenApiClient.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class ThreadsOpenApiClientRunnerTests : HostedUnitTest
{
    public ThreadsOpenApiClientRunnerTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
