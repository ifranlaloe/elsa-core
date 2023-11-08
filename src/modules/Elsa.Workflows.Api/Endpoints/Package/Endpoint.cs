using System.Reflection;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Package;

/// <summary>
/// Returns the package informational version.
/// </summary>
[PublicAPI]
internal class Version : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/package/version");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = RemoveAutoGeneratedPostfix(versionAttribute);

        await SendOkAsync(new Response(version), cancellationToken);
    }

    private static string RemoveAutoGeneratedPostfix(AssemblyInformationalVersionAttribute? versionAttribute) =>
        versionAttribute?.InformationalVersion.Split("+")[0]!;
}