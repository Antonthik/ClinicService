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
            //��������� ������� ��� ������������� �� �����
            //��� �������� ����������� �������������� � ��������� ��������� Http2 
            //gRPC �������� ������ �� Http2
            //REST �������� ������ �� Http1
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, 5100, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });//��� ��� gRPC
                
                options.Listen(IPAddress.Any, 5101,listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });//��� ��� REST API
            });

            #endregion

            #region Configure EF DBContext Service (Database)

            builder.Services.AddDbContext<ClinicServiceDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration["Settings:DatabaseOptions:ConnectionString"]);//ClinicServiceDbContext ������� ���������� ������ �� ������� ����������� ����������� � appsettings.json 
            });
            #endregion

            # region Configure gRPC
            builder.Services.AddGrpc().AddJsonTranscoding();//Json ������� ����������� � �������� ���
            #endregion

            #region Configure Swager
            // https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding-openapi?view=aspnetcore-7.0
            builder.Services.AddGrpcSwagger();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new OpenApiInfo { Title = "ClinicService", Version = "v1" });

                //��� ���������� ����� ������������
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "ClinicServiceV2.xml");
                c.IncludeXmlComments(filePath);
                c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);
            });
            #endregion

           


            var app = builder.Build();//�� ����� �������(���,��� ���� ���� ������) ���� ���������������� �������.��� ������  ������� web ����������.
            //���, ��� ����� ���� ������, ���� ���������������� �������� �������� ��� ����������

            #region Configure Swager
            if (app.Environment.IsDevelopment())//������ ������� ��������� ��, ��� ��������� ����������� � ��������� ����������(Developmen),������� ������������� � launchSettings.json � �������(environmentVariables).��� �����,��� �� ���������� ��������� ������� ��� �����������.
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");//��� ��������� �� ����� ������-���������� ������ ��������(��� ������������� ������� � ������ �������� )
                });
            }
            #endregion

            // Configure the HTTP request pipeline.

            app.UseRouting();// ����� ������������ �������� ������
            app.UseGrpcWeb(new GrpcWebOptions {DefaultEnabled=true });//��������

            app.MapGrpcService<Services.Impl.ClinicClientService>();//����� ��������� �������� ()
            app.MapGet("/", ()=>"Communication with gRPC");//��� ��������� � ������� �� ������ ���������
            app.Run();
        }
    }
}