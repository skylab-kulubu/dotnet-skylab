using Skylab.Forms.Infrastructure.Storage;
using Skylab.Forms.Infrastructure.Storage.Repositories;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Services;
using Skylab.Forms.Application.Mail;
using Skylab.Feedbacks.Infrastructure.Storage;
using Skylab.Feedbacks.Infrastructure.Storage.Repositories;
using Skylab.Feedbacks.Application.Abstractions.Storage;
using Skylab.Feedbacks.Application.Services;
using Skylab.Exports.Application.Services;
using Skylab.Shared.Application.Caching;
using Skylab.Shared.Application.Services;
using Skylab.Shared.Infrastructure.Auth;
using Skylab.Shared.Infrastructure.Caching;
using Skylab.Shared.Infrastructure.Mail;
using Skylab.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.HttpClients;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN") ?? "http://localhost:3000";

var redisConnection = Environment.GetEnvironmentVariable("Redis__ConnectionString") ?? "localhost:6379";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policyBuilder =>
    {
        policyBuilder.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDbContext<FormsDbContext>(options =>
{
    options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING"), 
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: new[] { "28P01" }); 
        });
});

builder.Services.AddDbContext<FeedbacksDbContext>(options =>
{
    options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING"),
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: new[] { "28P01" });
        });
});

builder.Services.AddEurekaDiscoveryClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddScoped<IFormRepository, FormRepository>();
builder.Services.AddScoped<IFormResponseRepository, FormResponseRepository>();
builder.Services.AddScoped<IComponentGroupRepository, ComponentGroupRepository>();
builder.Services.AddScoped<IFormMetricsRepository, FormMetricsRepository>();
builder.Services.AddScoped<IFormsUnitOfWork, FormsUnitOfWork>();

builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<IFormResponseService, FormResponseService>();
builder.Services.AddScoped<IFormMetricService, FormMetricService>();
builder.Services.AddScoped<IFormDraftService, FormDraftService>();
builder.Services.AddScoped<IComponentGroupService, ComponentGroupService>();

builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFeedbacksUnitOfWork, FeedbacksUnitOfWork>();

builder.Services.AddScoped<IFeedbackService, FeedbackService>();

builder.Services.AddScoped<IExcelService, ExcelService>();

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.Services.AddScoped<ICurrentUserService, JwtCurrentUserService>();
builder.Services.AddHttpClient<IExternalUserService, ExternalUserService>(client => { client.BaseAddress = new Uri("http://super-skylab"); }).AddServiceDiscovery();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.AddHttpClient("keycloak");
builder.Services.AddSingleton<IServiceTokenProvider, KeycloakServiceTokenProvider>();
builder.Services.AddTransient<ServiceTokenHandler>();

builder.Services.AddHttpClient<ISkyMailService, SkyMailClient>(client => { client.BaseAddress = new Uri("http://skymail"); })
    .AddServiceDiscovery()
    .AddHttpMessageHandler<ServiceTokenHandler>();

builder.Services.AddSingleton<ChannelMailDispatcher>();
builder.Services.AddSingleton<IMailDispatcher>(sp => sp.GetRequiredService<ChannelMailDispatcher>());
builder.Services.AddHostedService<MailWorker>();

builder.Services.Configure<FormMailOptions>(builder.Configuration.GetSection(FormMailOptions.SectionName));
builder.Services.AddScoped<IFormMailNotifier, FormMailNotifier>();
builder.Services.AddHostedService<PendingResponseReminderWorker>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var formsContext = services.GetRequiredService<FormsDbContext>();
        formsContext.Database.Migrate();

        var feedbacksContext = services.GetRequiredService<FeedbacksDbContext>();
        feedbacksContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Startup] Database migration failed: {ex.Message}");
        Console.ResetColor();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.MapFormAdminEndpoints();
app.MapFormEndpoints();
app.MapFeedbackEndpoints();
app.MapExportEndpoints();

app.Run();