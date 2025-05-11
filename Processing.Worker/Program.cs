using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Processing.Actors;
using Processing.Core;
using Processing.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Akka.NET ActorSystem with Dependency Injection
        services.AddSingleton(provider =>
        {
            var bootstrap = BootstrapSetup.Create();
            var diSetup = DependencyResolverSetup.Create(provider);
            var actorSystemSetup = bootstrap.And(diSetup);
            return ActorSystem.Create("MyActorSystem", actorSystemSetup);
        });

        // Register application services
        services.AddSingleton<IDataProcessor, DataProcessorImplementation>();
        services.AddHostedService<ProcessingWorker>();
    })
    .Build();

await host.RunAsync();