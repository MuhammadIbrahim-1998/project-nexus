using Microsoft.EntityFrameworkCore;
using Nexus.Application;
using Nexus.Application.Common.Interfaces;
using Nexus.Infrastructure.Agents.Discovery;
using Nexus.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NexusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<INexusDbContext>(sp => sp.GetRequiredService<NexusDbContext>());

builder.Services.AddApplication();

builder.Services.AddScoped<IJobDiscoverySource, DummyJobDiscoverySource>();
builder.Services.AddHostedService<DiscoveryAgentService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
