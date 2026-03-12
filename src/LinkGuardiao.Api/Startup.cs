using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using LinkGuardiao.Api.Middleware;
using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using LinkGuardiao.Application.Services;
using LinkGuardiao.Application.Validation;
using LinkGuardiao.Infrastructure.Data;
using LinkGuardiao.Infrastructure.Messaging;
using LinkGuardiao.Infrastructure.Options;
using Amazon.SQS;
using LinkGuardiao.Infrastructure.PostgreSQL;
using LinkGuardiao.Infrastructure.Security;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Threading.RateLimiting;

namespace LinkGuardiao.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.MaxDepth = 32;
                });

            services.AddSingleton<Serilog.ILogger>(_ => Log.Logger);
            services.AddSingleton<Serilog.Extensions.Hosting.DiagnosticContext>();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validation error",
                        Type = "https://httpstatuses.com/400"
                    };

                    return new BadRequestObjectResult(problemDetails);
                };
            });

            services.AddFluentValidationAutoValidation();
            services.AddScoped<IValidator<UserRegisterDto>, UserRegisterDtoValidator>();
            services.AddScoped<IValidator<UserLoginDto>, UserLoginDtoValidator>();
            services.AddScoped<IValidator<LinkCreateDto>, LinkCreateDtoValidator>();
            services.AddScoped<IValidator<LinkUpdateDto>, LinkUpdateDtoValidator>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "LinkGuardiao API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.Configure<JwtOptions>(_configuration.GetSection(JwtOptions.SectionName));
            services.Configure<LinkLimitsOptions>(_configuration.GetSection(LinkLimitsOptions.SectionName));

            var jwtOptions = _configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
            {
                throw new InvalidOperationException("Jwt:Secret is required.");
            }
            if (jwtOptions.Secret.Length < 32)
            {
                throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");
            }
            if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
            {
                throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
            }
            if (jwtOptions.AccessTokenMinutes < 5 || jwtOptions.AccessTokenMinutes > 120)
            {
                throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 5 and 120 minutes.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("Auth");
                            logger.LogWarning("JWT authentication failed: {Message}", context.Exception.Message);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            var corsAllowedOrigin = _configuration["CORS_ALLOWED_ORIGIN"];
            var corsOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var allowedOrigins = !string.IsNullOrWhiteSpace(corsAllowedOrigin)
                ? new[] { corsAllowedOrigin }
                : corsOrigins;

            if (allowedOrigins.Length == 0 && !_environment.IsDevelopment())
            {
                throw new InvalidOperationException("Cors:AllowedOrigins is required in production.");
            }

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                            .WithHeaders("Authorization", "Content-Type", "Accept", "X-Request-Id", "X-Link-Password")
                            .SetPreflightMaxAge(TimeSpan.FromHours(1));
                    }
                    else
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });
            });

            // In development (including tests), use permissive limits to avoid interference
            var isDev = _environment.IsDevelopment();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientId(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = isDev ? 100_000 : 200,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.AddPolicy("auth", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientId(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = isDev ? 100_000 : 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.AddPolicy("link-create", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientId(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = isDev ? 100_000 : 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
                options.AddPolicy("stats", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientId(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = isDev ? 100_000 : 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.AddPolicy("redirect", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientId(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = isDev ? 100_000 : 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
                options.OnRejected = async (context, token) =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("RateLimiter");
                    logger.LogWarning("Rate limit exceeded for {Client} at {Path}", GetClientId(context.HttpContext), context.HttpContext.Request.Path);

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = "Too many requests. Please retry later."
                    }, cancellationToken: token);
                };
            });

            var dynamoOptions = new DynamoDbOptions
            {
                LinksTableName = _configuration["DDB_TABLE_LINKS"] ?? _configuration["DynamoDb:LinksTableName"] ?? string.Empty,
                UsersTableName = _configuration["DDB_TABLE_USERS"] ?? _configuration["DynamoDb:UsersTableName"] ?? string.Empty,
                AccessTableName = _configuration["DDB_TABLE_ACCESS"] ?? _configuration["DynamoDb:AccessTableName"] ?? string.Empty,
                DailyLimitsTableName = _configuration["DDB_TABLE_DAILY_LIMITS"] ?? _configuration["DynamoDb:DailyLimitsTableName"] ?? string.Empty,
                RefreshTokensTableName = _configuration["DDB_TABLE_REFRESH_TOKENS"] ?? _configuration["DynamoDb:RefreshTokensTableName"] ?? string.Empty,
                EmailLocksTableName = _configuration["DDB_TABLE_EMAIL_LOCKS"] ?? _configuration["DynamoDb:EmailLocksTableName"] ?? string.Empty,
                AccessRetentionDays = _configuration.GetValue<int?>("DynamoDb:AccessRetentionDays") ?? 30
            };

            services.Configure<DynamoDbOptions>(options =>
            {
                options.LinksTableName = dynamoOptions.LinksTableName;
                options.UsersTableName = dynamoOptions.UsersTableName;
                options.AccessTableName = dynamoOptions.AccessTableName;
                options.DailyLimitsTableName = dynamoOptions.DailyLimitsTableName;
                options.RefreshTokensTableName = dynamoOptions.RefreshTokensTableName;
                options.EmailLocksTableName = dynamoOptions.EmailLocksTableName;
                options.AccessRetentionDays = dynamoOptions.AccessRetentionDays;
            });

            if (!_environment.IsDevelopment())
            {
                if (string.IsNullOrWhiteSpace(dynamoOptions.LinksTableName) ||
                    string.IsNullOrWhiteSpace(dynamoOptions.UsersTableName) ||
                    string.IsNullOrWhiteSpace(dynamoOptions.AccessTableName) ||
                    string.IsNullOrWhiteSpace(dynamoOptions.DailyLimitsTableName))
                {
                    throw new InvalidOperationException("DynamoDb table names are required in production.");
                }
            }

            var storageProvider = _configuration["STORAGE_PROVIDER"] ?? "dynamodb";
            if (storageProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = _configuration.GetConnectionString("PostgreSQL")
                    ?? _configuration["POSTGRESQL_CONNECTION_STRING"]
                    ?? throw new InvalidOperationException("PostgreSQL connection string is required when STORAGE_PROVIDER=postgresql.");
                services.AddPostgreSQLInfrastructure(connectionString);
            }
            else
            {
                services.AddAWSService<IAmazonDynamoDB>();
                services.AddSingleton<ILinkRepository, DynamoDbLinkRepository>();
                services.AddSingleton<IUserRepository, DynamoDbUserRepository>();
                services.AddSingleton<IAccessLogRepository, DynamoDbAccessLogRepository>();
                services.AddSingleton<IDailyLimitStore, DynamoDbDailyLimitStore>();
                services.AddSingleton<IRefreshTokenRepository, DynamoDbRefreshTokenRepository>();
            }

            services.AddAWSService<IAmazonSQS>();
            var sqsQueueUrl = _configuration["SQS_ANALYTICS_QUEUE_URL"];
            services.Configure<SqsOptions>(options => options.AnalyticsQueueUrl = sqsQueueUrl ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(sqsQueueUrl))
            {
                services.AddSingleton<IAnalyticsQueue, SqsAnalyticsQueue>();
            }
            else
            {
                services.AddSingleton<IAnalyticsQueue, NoOpAnalyticsQueue>();
            }

            services.AddScoped<ILinkService, LinkService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStatsService, StatsService>();
            services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownProxies.Add(IPAddress.Loopback);
            forwardedHeadersOptions.KnownProxies.Add(IPAddress.IPv6Loopback);
            app.UseForwardedHeaders(forwardedHeadersOptions);

            app.UseMiddleware<RequestIdMiddleware>();
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    diagnosticContext.Set("UserId", userId ?? "anonymous");
                    diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
                };
            });
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }))
                    .AllowAnonymous();
            });
        }

        private static string GetClientId(HttpContext context)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }

            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(ip))
            {
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwardedFor))
                {
                    ip = forwardedFor.Split(',')[0].Trim();
                }
            }

            return string.IsNullOrWhiteSpace(ip) ? "unknown" : $"ip:{ip}";
        }
    }
}
