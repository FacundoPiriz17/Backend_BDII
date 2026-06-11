using System.Text;

using Backend_BDII.Common.Auditing;
using Backend_BDII.Common.Database;
using Backend_BDII.Common.Security;

using Backend_BDII.Modules.Auth.Repositories;
using Backend_BDII.Modules.Auth.Services;

using Backend_BDII.Modules.Usuarios.Repositories;
using Backend_BDII.Modules.Usuarios.Services;

using Backend_BDII.Modules.Compras.Repositories;
using Backend_BDII.Modules.Compras.Services;

using Backend_BDII.Modules.Transferencias.Repositories;
using Backend_BDII.Modules.Transferencias.Services;

using Backend_BDII.Modules.Validaciones.Repositories;
using Backend_BDII.Modules.Validaciones.Services;

using Backend_BDII.Common.Validators.Auth;
using Backend_BDII.Common.Validators.Compras;
using Backend_BDII.Common.Validators.Transferencias;
using Backend_BDII.Common.Validators.Validaciones;

using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.WriteTo.Console();
});

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();

//Validators
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearCompraRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearTransferenciaRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<EscanearQrRequestValidator>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

//Modulos comunes
builder.Services.AddSingleton<IDbConnectionFactory, RoleBasedNpgsqlConnectionFactory>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEntradaQrCodeService, EntradaQrCodeService>();

//Auth
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

//Usuarios
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

//Compras
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<ICompraService, CompraService>();

//Transferencias
builder.Services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();
builder.Services.AddScoped<ITransferenciaService, TransferenciaService>();

//Validaciones
builder.Services.AddScoped<IValidacionRepository, ValidacionRepository>();
builder.Services.AddScoped<IValidacionService, ValidacionService>();

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta configurar Jwt:Key.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Backend BDII",
        Version = "v1"
    });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Ingresar: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("bearer", document),
            []
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();