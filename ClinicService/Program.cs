using ClinicService.Data;
using ClinicService.Services.Impl;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ClinicService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            #region Configure gRPC
            //Настройка сервиса для прослушивания на порту
            //все входящие подключения обрабатываются в контексте протокола Http2 
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
                options.UseSqlServer(builder.Configuration["Settings:DatabaseOptions:ConnectionString"]);//ClinicServiceDbContext вызовет обобщенный сервис со строкой подключения прописанной в appsettings.json 
            });

            builder.Services.AddGrpc();//добавить чтобы использовать Grpc (добавил после компиляции protos файла) - сервис сможет обрабатывать запросы
            #endregion

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.UseRouting();//чтобы организовать машруты для запросов

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ClinicClientService>();//точка обработки запросов (добавил вместе с  builder.Services.AddGrpc();)
            });

            app.Run();
        }
    }
}