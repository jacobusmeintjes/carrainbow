using System.Threading.Channels;
using carrainbow.service.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(Channel.CreateUnbounded<Car>(new UnboundedChannelOptions() { SingleReader = true }));
        services.AddSingleton(svc => svc.GetRequiredService<Channel<Car>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<Car>>().Writer);

        services.AddHostedService<MultipleReaderService>();
        services.AddHostedService<WriterService>();
    })
    .Build();



await host.RunAsync();