using ClinicService.Data;
using ClinicServiceProtos;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Net;

namespace ClinicServiceV2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            #region Configure Kestrel
            //Ќастройка сервиса дл€ прослушивани€ на порту
            //все вход€щие подключени€ обрабатываютс€ в контексте протокола Http2 
            //gRPC работает только по Http2
            //REST работает только по Http1
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, 5100, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });//это дл€ gRPC
                
                options.Listen(IPAddress.Any, 5101,listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });//это дл€ REST API
            });

            #endregion

            #region Configure EF DBContext Service (Database)

            builder.Services.AddDbContext<ClinicServiceDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration["Settings:DatabaseOptions:ConnectionString"]);//ClinicServiceDbContext вызовет обобщенный сервис со строкой подключени€ прописанной в appsettings.json 
            });
            #endregion

            # region Configure gRPC
            builder.Services.AddGrpc().AddJsonTranscoding();//Json запросы перобразуем в бинарный вид
            #endregion

            #region Configure Swager
            // https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding-openapi?view=aspnetcore-7.0
            builder.Services.AddGrpcSwagger();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new OpenApiInfo { Title = "ClinicService", Version = "v1" });

                //дл€ сохранени€ файла документации
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "ClinicServiceV2.xml");
                c.IncludeXmlComments(filePath);
                c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);
            });
            #endregion

           


            var app = builder.Build();//до этого момента(все,что выше этой строки) идет конфигурирование сервиса.Ёта строка  создает web приложение.
            //¬се, что после этой строки, идет конфигурирование вход€щих запросов веб приложени€

            #region Configure Swager
            if (app.Environment.IsDevelopment())//данное условие провер€ет то, что параметры подключени€ в состо€нии разработки(Developmen),которые настраиваютс€ в launchSettings.json в разделе(environmentVariables).Ёто нужно,что не раскрывать структуру сервиса дл€ посторонних.
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");//при обращении по этому адресу-приложение отдаст описание(веб представление методов и прочих объектов )
                });
            }
            #endregion

            // Configure the HTTP request pipeline.

            app.UseRouting();// будем обрабатывать вход€щие запрсы
            app.UseGrpcWeb(new GrpcWebOptions {DefaultEnabled=true });//ƒобавили

            app.MapGrpcService<Services.Impl.ClinicClientService>();//точка обработки запросов ()
            app.MapGet("/", ()=>"Communication with gRPC");//ѕри обращении к сервису он выдаст сообщение
            app.Run();
        }
    }
}