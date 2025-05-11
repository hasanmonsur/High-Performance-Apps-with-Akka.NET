using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Processing.Actors;
using Processing.Api.Services;
using Processing.Core;
using static Processing.Core.Messages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register your dependencies first
builder.Services.AddSingleton<IDataProcessor, DataProcessorImplementation>();
builder.Services.AddSingleton<ILogger<DataProcessorActor>>(provider =>
    provider.GetRequiredService<ILoggerFactory>().CreateLogger<DataProcessorActor>());

// Configure Akka.NET
var akkaConfig = ConfigurationFactory.ParseString(@"
    akka {
        actor {
            provider = cluster
            serializers {
                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
            }
            serialization-bindings {
                ""System.Object"" = hyperion
            }
        }
        remote {
            dot-netty.tcp {
                port = 8081
                hostname = 0.0.0.0
                public-hostname = ""localhost"" // Important for Docker/container setups
            }
        }
        cluster {
            roles = [api]
            seed-nodes = [""akka.tcp://ProcessingSystem@localhost:8081""]
            shutdown-after-unsuccessful-join-seed-nodes = 30s
        }
    }");

// Register ActorSystem with DI
builder.Services.AddSingleton(provider =>
{
    var setup = BootstrapSetup.Create()
        .WithConfig(akkaConfig)
        .And(DependencyResolverSetup.Create(provider));

    return ActorSystem.Create("ProcessingSystem", setup);
});

// Register processor actor factory
builder.Services.AddSingleton(provider =>
{
    var system = provider.GetRequiredService<ActorSystem>();
    var resolver = DependencyResolver.For(system);

    return system.ActorOf(
        resolver.Props<ProcessingPoolActor>(
            provider.GetRequiredService<IDataProcessor>(),
            provider.GetRequiredService<ILogger<DataProcessorActor>>()
        ),
        "processor"
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () =>
{
    var system = app.Services.GetRequiredService<ActorSystem>();
    return system.WhenTerminated.IsCompleted ?
        Results.StatusCode(503) :
        Results.Ok();
});

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    var system = app.Services.GetRequiredService<ActorSystem>();
    CoordinatedShutdown.Get(system)
        .Run(CoordinatedShutdown.ClrExitReason.Instance)
        .Wait(TimeSpan.FromSeconds(30));
});

app.Run();