using InnoviaHub.Api.Data;
using InnoviaHub.Api.Extensions;
using InnoviaHub.Api.Handler;
using InnoviaHub.DataAccess;
using InnoviaHub.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using InnoviaHub.Api.Hubs;
using Scalar.AspNetCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var frontendUrl = builder.Configuration["FRONTEND_URL"]
    ?? "http://localhost:5173";

var openAiApiKey = builder.Configuration["OPENAI_API_KEY"]
    ?? throw new InvalidOperationException("OPENAI_API_KEY IS MISSING");

builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiApiKey);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddCors(options =>
{
   options.AddPolicy("Frontend",policy =>
   {
       policy
       .WithOrigins(frontendUrl)
       .AllowAnyHeader()
       .AllowAnyMethod()
       .AllowCredentials();
   });
});

var connectionString = builder.Configuration["SQL_ConnectionString"]
    ?? throw new InvalidOperationException("SQL_CONNECTION_STRING IS MISSING");

builder.Services.AddDbContext<InnoviaHubDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<InnoviaHubDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddApplicationServices();
builder.Services.AddApplicationRepositories();
builder.Services.AddSignalR();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider.GetRequiredService<InnoviaHubDbContext>();

    await dbContext.Database.MigrateAsync();

    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    await IdentitySeeder.SeedRolesAsync(roleManager);
    await IdentitySeeder.SeedAdminAsync(userManager, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");// med detta får klienten anslutning till loclahosten

app.MapGet("/api/ping", () => Results.Ok(new
{
    Message = "API is working",
    Timestamp = DateTime.UtcNow
}));

app.Run();
