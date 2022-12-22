using ClinicServiceProtos;
using Grpc.Core;
using Grpc.Net.Client;
using static ClinicServiceProtos.AuthenticateService;
using static ClinicServiceProtos.ClinicClientService;

namespace ClinicServiceClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //AppContext.SetSwitch(
            //    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);//работаем в контексте незащищенного соединения
            //var channel = GrpcChannel.ForAddress("http://localhost:5001");//создаем канал 
            var channel = GrpcChannel.ForAddress("https://localhost:5001");//создаем канал 


            AuthenticateServiceClient authenticateServiceClient = new AuthenticateServiceClient(channel);


            var authenticationResponse = authenticateServiceClient.Login(new AuthenticationRequest
            {
                UserName = "sample@gmail.com",
                Password = "12345"
            });

            if (authenticationResponse.Status != 0)
            {
                Console.WriteLine("Authentication error.");
                Console.ReadKey();
                return;
            }


            Console.WriteLine($"Session token: {authenticationResponse.SessionContext.SessionToken}");

            //объект - в рамках канала передает заголовок
            var callCredentials = CallCredentials.FromInterceptor((c, m) =>
            {
                m.Add("Authorization",
                    $"Bearer {authenticationResponse.SessionContext.SessionToken}");
                return Task.CompletedTask;
            });

            var protectedChannel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), callCredentials)
            });






            ClinicClientServiceClient client = new ClinicClientServiceClient(channel);//создаем клиент

            var createClientResponse = client.CreateClient(new ClinicServiceProtos.CreateClientRequest//создаем клиент
            {
                Document = "PASS123",
                FirstName = "Антон",
                Surname = "Семенов",
                Patronymic = "Валерьевич"
            });

            Console.WriteLine($"Client ({createClientResponse.ClientId}) created successfully.");

            var getClientsResponse = client.GetClients(new ClinicServiceProtos.GetClientsRequest());//возвращаем список клентов

            Console.WriteLine("Clients:");
            Console.WriteLine("========\n");
            foreach (var clientObj in getClientsResponse.Clients)
            {
                Console.WriteLine($"{clientObj.Document} >> {clientObj.Surname} {clientObj.FirstName}");
            }

            Console.ReadKey();
        }
    }
}