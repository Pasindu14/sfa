using Microsoft.EntityFrameworkCore;
using Serilog;
using sfa_api.Common.Audit;
using sfa_api.Common.Extensions;
using sfa_api.Common.Middleware;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Logging;
using sfa_api.Infrastructure.Persistence;
using sfa_api.Features.Users.Repositories;
using sfa_api.Features.Users.Services;
using sfa_api.Features.Users.Validators;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Distributors.Services;
using sfa_api.Features.Distributors.Validators;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Auth.Repositories;
using sfa_api.Features.Auth.Services;
using sfa_api.Features.Auth.Validators;
using sfa_api.Features.Auth.Requests;
using FluentValidation;

// Bootstrap logger
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ──────────────────────────────────────────────────────────
    SerilogConfig.Apply(builder);

    // ── Database ─────────────────────────────────────────────────────────
    builder.Services.AddScoped<AuditInterceptor>();
    builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

    // ── Caching ──────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<ICacheService, MemoryCacheService>();

    // ── Idempotency ───────────────────────────────────────────────────────
    builder.Services.AddScoped<IIdempotencyService, PostgresIdempotencyService>();
    builder.Services.AddHostedService<IdempotencyCleanupService>();

    // ── JWT Revocation ────────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenRevocationService, PostgresTokenRevocationService>();

    // ── Distributed Locking ───────────────────────────────────────────────
    builder.Services.AddSingleton<IDistributedLockService, PostgresAdvisoryLockService>();

    // ── Auth ──────────────────────────────────────────────────────────────
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    // ── HTTP & API ────────────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers();
    builder.Services.AddSFACors(builder.Configuration);
    builder.Services.AddSFARateLimiting(builder.Configuration);
    builder.Services.AddSFASwagger();
    builder.Services.AddSFAHealthChecks(builder.Configuration);

    // ── Features (added here as features are built) ───────────────────────

    // ── Auth Feature ──────────────────────────────────────────────────────
    builder.Services.AddScoped<IAuthRepository, AuthRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IJwtTokenHelper, JwtTokenHelper>();
    builder.Services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
    builder.Services.AddScoped<IValidator<RefreshRequest>, RefreshValidator>();

    // ── Users Feature ─────────────────────────────────────────────────────
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserValidator>();
    builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserValidator>();
    builder.Services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordValidator>();
    builder.Services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordValidator>();

    // ── Distributors Feature ──────────────────────────────────────────────
    builder.Services.AddScoped<IDistributorRepository, DistributorRepository>();
    builder.Services.AddScoped<IDistributorService, DistributorService>();
    builder.Services.AddScoped<IValidator<CreateDistributorRequest>, CreateDistributorValidator>();
    builder.Services.AddScoped<IValidator<UpdateDistributorRequest>, UpdateDistributorValidator>();

    var app = builder.Build();

    // ── Seed ──────────────────────────────────────────────────────────────
    await DataSeeder.SeedAsync(app.Services, app.Logger);

    // ── Middleware Pipeline (ORDER MATTERS) ───────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();    // 1. Correlation ID first
    app.UseMiddleware<GlobalExceptionMiddleware>();  // 2. Catch all exceptions
    app.UseSerilogRequestLogging();                 // 3. Log every request
    app.UseHttpsRedirection();                      // 4. HTTPS only
    app.UseCors("SFAPolicy");                       // 5. CORS
    app.UseRateLimiter();                           // 6. Rate limiting
    app.UseAuthentication();                        // 7. Validate JWT
    app.UseAuthorization();                         // 8. Permissions

    // ── Endpoints ─────────────────────────────────────────────────────────
    app.MapControllers();
    app.MapSFAHealthChecks();

    // ── Swagger ───────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SFA API v1"));
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}
