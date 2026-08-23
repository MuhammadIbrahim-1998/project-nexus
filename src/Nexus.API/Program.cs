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

builder.Services.AddHttpClient<RemoteOkJobDiscoverySource>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<HimalayasJobDiscoverySource>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<ArbeitnowJobDiscoverySource>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
// Adzuna temporarily disabled: force-sets IsRemote=true + queries US job board only, which
// polluted the jobs table with non-global-remote jobs. Re-enable when source reflects remote status.
builder.Services.AddScoped<IJobDiscoverySource, CompositeJobDiscoverySource>();

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
builder.Services.AddScoped<IProjectSuggestionService, ProjectSuggestionService>();

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
