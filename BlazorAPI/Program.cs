using BlazorAPI.Extensions;
using BlazorAPI.Interfaces.Autenticacao;
using BlazorAPI.Interfaces.Repository;
using BlazorAPI.Interfaces.Service;
using BlazorAPI.Models;
using BlazorAPI.Repository;
using BlazorAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace BlazorAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Registra o contexto do banco de dados com SQLite, usando a string de conex�o do appsettings.json.
            builder.Services.AddDbContext<BlazorAPIBancodbContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            //Pegando o link na appsettings
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

            //Permitir interacao com a aplicacao blazor
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("PermitirBlazor", policy =>
                {
                    policy.WithOrigins(allowedOrigins) // Porta do seu app Blazor
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            ////Permitir interacao com a aplicacao blazor
            //builder.Services.AddCors(options =>
            //{
            //    options.AddPolicy("PermitirBlazor", policy =>
            //    {
            //        policy.WithOrigins("https://localhost:7170") // Porta do seu app Blazor
            //              .AllowAnyHeader()
            //              .AllowAnyMethod();
            //    });
            //});

            //Limita��o de Taxa (Rate Limiting) - impedir que seja feita varias requisi��es em um curto periodo
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("ApiPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 50, // 50 requisi��es
                            Window = TimeSpan.FromMinutes(1), // por minuto
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        }));
            });

            // Configura��o do cache
            //builder.Services.AddStackExchangeRedisCache(options =>
            //{
            //    // Configura��o segura usando vari�veis de ambiente
            //    options.Configuration = builder.Configuration.GetConnectionString("Redis") ??
            //                          builder.Configuration["REDIS_CONNECTION_STRING"];
            //    options.InstanceName = "BlazorAPI_";
            //});

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            //Usuario
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            //Usuario
            builder.Services.AddScoped<ITarefaService, TarefaService>();
            builder.Services.AddScoped<ITarefaRepository, TarefaRepository>();

            //Autenticacao
            builder.Services.AddScoped<IAutenticacao, AutenticacaoService>();

            //Swagger
            builder.Services.AdicionarConfiguracaoSwagger();

            //JWT
            builder.Services.AdicionarConfiguracaoJwtEF(builder.Configuration);

            var app = builder.Build();

            //Permitir interacao com a aplicacao blazor
            app.UseCors("PermitirBlazor");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}