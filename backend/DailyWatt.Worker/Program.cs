using DailyWatt.Infrastructure;
using DailyWatt.Worker;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddHostedService<ImportWorker>();
    });

var host = builder.Build();
host.Run();
