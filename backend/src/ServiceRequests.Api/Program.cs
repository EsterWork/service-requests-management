using ServiceRequests.Infrastructure;
using ServiceRequests.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("ServiceRequestsDb")
    ?? throw new InvalidOperationException("Connection string 'ServiceRequestsDb' is not configured."));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("seed"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(CancellationToken.None);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;
