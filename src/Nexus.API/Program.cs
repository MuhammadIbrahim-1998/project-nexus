using Microsoft.EntityFrameworkCore;
using Nexus.Infrastructure.Hubs;
using Nexus.Application;
using Nexus.Application.Common.Interfaces;
using Nexus.Infrastructure.Agents.ContentGeneration;
using Nexus.Infrastructure.Agents.Discovery;
using Nexus.Infrastructure.Agents.Matching;
using Nexus.Infrastructure.Agents.Orchestrator;
using Nexus.Infrastructure.ExternalServices.DeepSeek;
using Nexus.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NexusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<INexusDbContext>(sp => sp.GetRequiredService<NexusDbContext>());

builder.Services.AddApplication();

builder.Services.AddScoped<IJobDiscoverySource, DummyJobDiscoverySource>();
builder.Services.AddSingleton<DiscoveryAgentService>();
builder.Services.AddSingleton<MatchingAgentService>();
builder.Services.AddSingleton<ContentGenerationAgentService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DiscoveryAgentService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MatchingAgentService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ContentGenerationAgentService>());
builder.Services.AddScoped<NexusOrchestratorService>();
builder.Services.AddHttpClient<DeepSeekMatchingClient>(client =>
{
    client.BaseAddress = new Uri("https://api.deepseek.com");
    client.Timeout = TimeSpan.FromSeconds(100);
});
builder.Services.AddHttpClient<DeepSeekContentClient>(client =>
{
    client.BaseAddress = new Uri("https://api.deepseek.com");
    client.Timeout = TimeSpan.FromSeconds(100);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.MapControllers();
app.MapHub<AgentStatusHub>("/hubs/agent-status");

app.Run();
