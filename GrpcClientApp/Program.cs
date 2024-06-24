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
        var serviceProvider = RegisterGrpcClient("Greeter");
        
        var grpcClientFactory = serviceProvider.GetRequiredService<GrpcClientFactory>();
        var grpcClient = grpcClientFactory.CreateClient<Greeter.GreeterClient>("Greeter");

        var reply = grpcClient.SayHello(new HelloRequest { Name = "Neo" });

        serviceProvider.Dispose();
        return reply.Message;
    })
    .WithName("GetGreetings")
    .WithOpenApi();

app.MapPut("/{endpoint}", (string endpoint) =>
    {
        var serviceProvider = RegisterGrpcClient(endpoint);
        
        var grpcClientFactory = serviceProvider.GetRequiredService<GrpcClientFactory>();
        var grpcClient = grpcClientFactory.CreateClient<Greeter.GreeterClient>(endpoint);

        var reply = grpcClient.SayHello(new HelloRequest { Name = "Neo" });

        serviceProvider.Dispose();
        return reply.Message;
    })
    .WithName("NewEndpoint")
    .WithOpenApi();

app.Run();

AutofacServiceProvider RegisterGrpcClient(string name)
{
    var scope = rootContainer.BeginLifetimeScope(b =>
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddGrpcClient<Greeter.GreeterClient>(name, options => options.Address = new Uri("dns:///localhost:5159"))
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
    
    return new AutofacServiceProvider(scope);
}