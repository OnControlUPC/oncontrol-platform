using Microsoft.OpenApi.Models;
using oncontigo_platform.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using oncontigo_platform.Shared.Infrastructure.Persistence.EPC.Configuration;
using Microsoft.EntityFrameworkCore;
using oncontigo_platform.IAM.Application.Internal.CommandServices;
using oncontigo_platform.IAM.Application.Internal.OutboundServices;
using oncontigo_platform.IAM.Application.Internal.QueryServices;
using oncontigo_platform.IAM.Domain.Repositories;
using oncontigo_platform.IAM.Domain.Services;
using oncontigo_platform.IAM.Infrastructure.Hashing.BCrypt.Services;
using oncontigo_platform.IAM.Infrastructure.Persistence.EFC.Repositories;
using oncontigo_platform.IAM.Infrastructure.Tokens.JWT.Configuration;
using oncontigo_platform.IAM.Infrastructure.Tokens.JWT.Services;
using oncontigo_platform.IAM.Interfaces.ACL.Services;
using oncontigo_platform.IAM.Interfaces.ACL;
using oncontigo_platform.Shared.Domain.Repositories;
using oncontigo_platform.Shared.Infrastructure.Persistence.EPC.Repositories;
using oncontigo_platform.Shared.Interfaces.ASP.Configuration;
using oncontigo_platform.HealthTracking.Domain.Repositories;
using oncontigo_platform.HealthTracking.Infrastructure;
using oncontigo_platform.HealthTracking.Domain.Services;
using oncontigo_platform.HealthTracking.Application.Internal.CommandServices;
using oncontigo_platform.HealthTracking.Application.Internal.QueryServices;
using oncontigo_platform.Profiles.Domain.Model.Aggregates;
using oncontigo_platform.Profiles.Domain.Repositories;
using oncontigo_platform.Profiles.Infrastructure;
using oncontigo_platform.Profiles.Domain.Services;
using oncontigo_platform.Profiles.Application.QueryServices;
using oncontigo_platform.Profiles.Application.CommandServices;
using oncontigo_platform.HealthTracking.Application.Internal.OutbondServices.ACL;
using oncontigo_platform.Profiles.Interfaces.ACL;
using oncontigo_platform.Profiles.Interfaces.ACL.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()));
var connectionString = Environment.GetEnvironmentVariable("AZURE_MYSQL_CONNECTIONSTRING");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString == null) return;
    if (builder.Environment.IsDevelopment())
        options.UseMySQL(connectionString)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors();
    else if (builder.Environment.IsProduction())
        options.UseMySQL(connectionString)
            .LogTo(Console.WriteLine, LogLevel.Error)
            .EnableDetailedErrors();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "OnContigo.API",
            Version = "v1",
            Description = "OnContigo API",
        });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                },
                Array.Empty<string>()
            }
        });
});
// Shared Bounded Context Dependency Injections
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// IAM Bounded Context Injection Configuration
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

builder.Services.AddScoped<IPatientFollowUpRepository, PatientFollowUpRepository>();
builder.Services.AddScoped<IPatientFollowUpCommandService, PatientFollowUpCommandService>();
builder.Services.AddScoped<IPatientFollowUpQueryService, PatientFollowUpQueryService>();

builder.Services.AddScoped<IMedicineRepository,MedicineRepository>();
builder.Services.AddScoped<IMedicineCommandService,MedicineCommandService>();
builder.Services.AddScoped<IMedicineQueryService, MedicineQueryService>();
builder.Services.AddScoped<IProfilesContextFacade, ProfilesContextFacade>();
builder.Services.AddScoped<IExternalProfileService, ExternalProfileService>();

builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorQueryService, DoctorQueryService>();
builder.Services.AddScoped<IDoctorCommandService, DoctorCommandService>();

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPatientQueryService, PatientQueryService>();
builder.Services.AddScoped<IPatientCommandService, PatientCommandService>();




builder.Services.AddRouting(o => o.LowercaseUrls = true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS Policy
app.UseCors("AllowedAllPolicy");

// Add Authorization Middleware to the Request Pipeline
app.UseRequestAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
