using Grpc.Core;
using Grpc.Net.ClientFactory;
using GrpcService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddGrpcClient<Greeter.GreeterClient>("Greeter", options =>
    {
        options.Address = new Uri("http://localhost:5159");
        options.ChannelOptionsActions.Add(channelOptions =>
        {
            channelOptions.Credentials = ChannelCredentials.Insecure;
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

app.MapGet("/greetings", () =>
    {
        using var scope = scopeFactory.CreateScope();
        var grpcClientFactory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();
        var grpcClient = grpcClientFactory.CreateClient<Greeter.GreeterClient>("Greeter");

        var reply = grpcClient.SayHello(new HelloRequest { Name = "Neo" });
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