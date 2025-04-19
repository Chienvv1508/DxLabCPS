using JobService;
using JobService.BackGroundTask;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddHostedService<Worker>();
        services.AddHostedService<DepreciationTHBackGroundTask>();
        services.AddHostedService<UltilizationRateBackGroundTask>();
    })
    .Build();

await host.RunAsync();
