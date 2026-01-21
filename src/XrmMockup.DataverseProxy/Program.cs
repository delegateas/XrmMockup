using System.CommandLine;
using DataverseConnection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using XrmMockup.DataverseProxy;

// Define CLI options
var urlOption = new Option<string?>("--url", "-u")
{
    Description = "Dataverse environment URL (e.g., https://org.crm.dynamics.com)",
    Arity = ArgumentArity.ExactlyOne
};

var pipeOption = new Option<string?>("--pipe", "-p")
{
    Description = "Named pipe name for IPC communication",
    Arity = ArgumentArity.ExactlyOne
};

var mockDataFileOption = new Option<string?>("--mock-data-file", "-m")
{
    Description = "Path to JSON file containing mock data. When set, the proxy loads entities from this file instead of connecting to Dataverse. Used for testing.",
    Arity = ArgumentArity.ZeroOrOne
};

// Build root command
var rootCommand = new RootCommand("XrmMockup Dataverse Proxy - Out-of-process bridge for online data fetching")
{
    urlOption,
    pipeOption,
    mockDataFileOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var url = parseResult.GetValue(urlOption);
    var pipeName = parseResult.GetValue(pipeOption);
    var mockDataFile = parseResult.GetValue(mockDataFileOption);

    if (string.IsNullOrEmpty(pipeName))
    {
        Console.Error.WriteLine("Error: --pipe is required");
        return 1;
    }

    // Read auth token from stdin (with timeout) - more secure than command line args
    string? authToken;
    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
    {
        try
        {
            authToken = await Console.In.ReadLineAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Error: Timeout waiting for auth token on stdin");
            return 1;
        }
    }

    if (string.IsNullOrEmpty(authToken))
    {
        Console.Error.WriteLine("Error: Auth token is required (pass via stdin)");
        return 1;
    }

    // Set up DI
    var services = new ServiceCollection();

    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    // Check if running in mock mode or real Dataverse mode
    var useMockData = !string.IsNullOrEmpty(mockDataFile);

    if (!useMockData && string.IsNullOrEmpty(url))
    {
        Console.Error.WriteLine("Error: --url is required unless --mock-data-file is specified");
        return 1;
    }

    if (!useMockData)
    {
        // Configure DataverseConnection with the URL passed via command line
        services.AddDataverse(options => options.DataverseUrl = url!);
    }

    await using var serviceProvider = services.BuildServiceProvider();

    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        IDataverseServiceFactory serviceFactory;

        if (useMockData)
        {
            logger.LogInformation("Starting XrmMockup Dataverse Proxy in mock mode with data file: {MockDataFile}", mockDataFile);
            serviceFactory = new MockDataServiceFactory(mockDataFile);
        }
        else
        {
            logger.LogInformation("Starting XrmMockup Dataverse Proxy for {Url}", url);
            var serviceClient = serviceProvider.GetRequiredService<ServiceClient>();

            // Verify connection
            var whoAmI = serviceClient.Execute(new Microsoft.Crm.Sdk.Messages.WhoAmIRequest());
            logger.LogInformation("Connected to Dataverse as user {UserId}", ((Microsoft.Crm.Sdk.Messages.WhoAmIResponse)whoAmI).UserId);

            serviceFactory = new DataverseServiceFactory(serviceClient);
        }

        // Start the proxy server
        var proxyServer = new ProxyServer(serviceFactory, pipeName, authToken, serviceProvider.GetRequiredService<ILogger<ProxyServer>>());
        await proxyServer.RunAsync(cancellationToken);

        return 0;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Proxy server failed");
        return 1;
    }
});

return await rootCommand.Parse(args).InvokeAsync();
