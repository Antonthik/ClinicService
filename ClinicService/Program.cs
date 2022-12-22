using ClinicService.Data;
using ClinicService.Services;
using ClinicService.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Web;
using System.Net;
using System.Text;

namespace ClinicService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            #region Configure gRPC
            //Ќастройка сервиса дл€ прослушивани€ на порту
            //все вход€щие подключени€ обрабатываютс€ в контексте протокола Http2 
            //gRPC работает только по Http2
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, 5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            builder.Services.AddGrpc();

            #endregion

            #region Configure EF DBContext Service (Database)

            builder.Services.AddDbContext<ClinicServiceDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration["Settings:DatabaseOptions:ConnectionString"]);//ClinicServiceDbContext вызовет обобщенный сервис со строкой подключени€ прописанной в appsettings.json 
            });

            builder.Services.AddGrpc();//добавить чтобы использовать Grpc (добавил после компил€ции protos файла) - сервис сможет обрабатывать запросы
            #endregion


            #region Configure logging service

            builder.Services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.All | HttpLoggingFields.RequestQuery;
                logging.RequestBodyLogLimit = 4096;
                logging.ResponseBodyLogLimit = 4096;
                logging.RequestHeaders.Add("Authorization");
                logging.RequestHeaders.Add("X-Real-IP");
                logging.RequestHeaders.Add("X-Forwarded-For");
            });


            builder.Host.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();

            }).UseNLog(new NLogAspNetCoreOptions() { RemoveLoggerFactoryFilter = true });

            #endregion

            #region Configure Repository Services

            builder.Services.AddScoped<IPetRepository, PetRepository>();
            builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();
            builder.Services.AddScoped<IClientRepository, ClientRepository>();

            #endregion



            #region Configure Services

            builder.Services.AddSingleton<IAuthenticateService, AuthenticateService>();

            #endregion

            #region Configure JWT

            //дл€ всех вход€щих запросов необходимо проходить идентификацию.
            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
             
            // параметры токенов, в том числе правила валидаци€ токена, чтобы не прописывать дл€ каждого метода - централизовано указываем здесь
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters{
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AuthenticateService.SecretKey)),// SecretKey-используем дл€ валидации тот же ключ, который используем при генерации
                    ValidateIssuer = false, // true
                    ValidateAudience = false, // true
                    ValidIssuer = builder.Configuration["Settings:Security:Issuer"],
                    ValidAudience = builder.Configuration["Settings:Security:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

            #endregion




            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "—ервис клиники",
                    Version = "v1",
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme(Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            //app.UseHttpLogging();
            app.UseWhen( // ѕообещали починить в 7 .net !
                ctx => ctx.Request.ContentType != "application/grpc",
                builder =>
                {
                    builder.UseHttpLogging();
                }
            );

            app.MapControllers();

           // app.UseRouting();//чтобы организовать машруты дл€ запросов

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ClinicClientService>();//точка обработки запросов (добавил вместе с  builder.Services.AddGrpc();)
            });

            app.Run();
        }
    }
}