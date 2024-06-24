using Autofac;
using Autofac.Extensions.DependencyInjection;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Grpc.Net.ClientFactory;
using GrpcService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var containerBuilder = new ContainerBuilder();
containerBuilder.Populate(builder.Services);
var rootContainer = containerBuilder.Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/greetings", () =>
    {
        var scope = rootContainer.BeginLifetimeScope(b =>
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddGrpcClient<Greeter.GreeterClient>("Greeter", options => options.Address = new Uri("dns:///localhost:5159"))
                .ConfigureChannel(options =>
                {
                    options.Credentials = ChannelCredentials.Insecure;
                    options.ServiceConfig = new ServiceConfig
                    {
                        LoadBalancingConfigs = { new RoundRobinConfig() }
                    };
                });
            b.Populate(serviceCollection);
        });
        
        var serviceProvider = new AutofacServiceProvider(scope);

        var grpcClientFactory = serviceProvider.GetRequiredService<GrpcClientFactory>();
        var grpcClient = grpcClientFactory.CreateClient<Greeter.GreeterClient>("Greeter");

        var reply = grpcClient.SayHello(new HelloRequest { Name = "Neo" });

        serviceProvider.Dispose();
        scope.Dispose();
        rootContainer.Dispose();
        
        return reply.Message;
    })
    .WithName("GetGreetings")
    .WithOpenApi();

app.MapPut("/{endpoint}", (string endpoint) =>
    {
        using var scope = scopeFactory.CreateScope();
        var grpcClientFactory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();
        var grpcClient = grpcClientFactory.CreateClient<Greeter.GreeterClient>(endpoint);

        var reply = grpcClient.SayHello(new HelloRequest { Name = "Neo" });
        return reply.Message;
    })
    .WithName("NewEndpoint")
    .WithOpenApi();

app.Run();