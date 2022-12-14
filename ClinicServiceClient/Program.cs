using Grpc.Net.Client;
using static ClinicServiceProtos.ClinicClientService;

namespace ClinicServiceClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);//работаем в контексте незащищенного соединения
            var channel = GrpcChannel.ForAddress("http://localhost:5001");//создаем канал 


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