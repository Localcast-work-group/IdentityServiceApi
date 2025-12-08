using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using FluentValidation;
using FluentValidation.AspNetCore;
using IdentityService.Api.Data;
using IdentityService.Api.Extensions;
using IdentityService.Api.Filters;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Repositories;
using IdentityService.Api.Services;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Reflection;
using System.Text;
namespace IdentityService.Api

{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            AuthenticationSettings authenticationSettings = new AuthenticationSettings();
            configuration.GetSection("JWT").Bind(authenticationSettings);
            var connectionString = builder.Configuration.GetConnectionString("IdentityDb");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
              options.UseNpgsql(connectionString));
            builder.Services.AddCors(options =>
            {

                options.AddPolicy("AllowAllOrigins",

                    builder =>
                    {

                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .WithExposedHeaders("www-authenticate");
                    });
            });
            builder.Services.AddSingleton(authenticationSettings);
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IUserContext, UserContext>();
            builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IOAuthService, OAuthService>();

            builder.Services.AddFluentValidationAutoValidation(configuration =>
            {
                configuration.DisableBuiltInModelValidation = true;

                configuration.ValidationStrategy = SharpGrip.FluentValidation.AutoValidation.Mvc.Enums.ValidationStrategy.All;
                configuration.EnableBodyBindingSourceAutomaticValidation = true;

                configuration.EnableFormBindingSourceAutomaticValidation = true;

                configuration.EnableQueryBindingSourceAutomaticValidation = true;

                configuration.EnablePathBindingSourceAutomaticValidation = true;

                configuration.EnableCustomBindingSourceAutomaticValidation = true;

            });
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiExceptionFilter>();
            });
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            }).AddJwtBearer(cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;
                cfg.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidIssuer = authenticationSettings.Issuer,
                    ValidAudience = authenticationSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationSettings.Key))
                };
                cfg.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($" >>> AUTH FAILED: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($" >>> TOKEN VALIDATED: {context.Principal.Identity.Name}");
                        return Task.CompletedTask;
                    }
                    ,
                    OnChallenge = context =>
                    {
                        Log.Error(">>> CHALLENGE (401/403) triggered. Error: {Error}, Description: {Desc}",
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });
            builder.Services.AddMassTransit(
                options => {
                    options.SetKebabCaseEndpointNameFormatter();
                    options.AddEntityFrameworkOutbox<ApplicationDbContext>(outboxConfig =>
                    {
                        outboxConfig.UsePostgres();
                        outboxConfig.UseBusOutbox();
                    });
                    options.UsingRabbitMq((context, cfg) =>
                    {
                        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
                        cfg.Host(new Uri(connectionString));

                        cfg.ConfigureEndpoints(context);
                    });

                });



            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            builder.Host.UseSerilog((ctx, lc) =>
                lc.ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(new[] { new Uri(builder.Configuration["Elastic:Uri"]) }, opts =>
                {
                    opts.DataStream = new DataStreamName("logs", "identity-service");

                }
                , transport =>
                {
                    transport.Authentication(new BasicAuthentication(
                        "elastic",
                        builder.Configuration["Elastic:Password"]));
                }
                )
               .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
             );
            var app = builder.Build();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                    options.RoutePrefix = string.Empty;
                });
                DatabaseManagmentService.MigrationInitialisation(app);
            }
            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();

            app.UseAuthorization();




            app.MapControllers();

            app.Run();
        }

    }
}
