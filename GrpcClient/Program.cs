using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using PhotoAlbumApi.Proto;

namespace GrpcClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // The port number must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("http://localhost:3001");
            var client = new PhotoService.PhotoServiceClient(channel);

            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Get Photo");
                Console.WriteLine("2. Add Photo");
                Console.WriteLine("3. Update Photo");
                Console.WriteLine("4. Soft Delete Photo");
                Console.WriteLine("5. Exit");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await GetPhoto(client);
                        break;
                    case "2":
                        await AddPhoto(client);
                        break;
                    case "3":
                        await UpdatePhoto(client);
                        break;
                    case "4":
                        await SoftDeletePhoto(client);
                        break;
                    case "5":
                        return;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
                Console.WriteLine();
            }
        }

        static async Task GetPhoto(PhotoService.PhotoServiceClient client)
        {
            Console.Write("Enter Photo ID: ");
            var id = int.Parse(Console.ReadLine());

            var request = new GetPhotoRequest { Id = id };
            try
            {
                var response = await client.GetPhotoAsync(request);
                Console.WriteLine($"Photo ID: {response.Id}");
                Console.WriteLine($"Album ID: {response.AlbumId}");
                Console.WriteLine($"Title: {response.Title}");
                Console.WriteLine($"Description: {response.Description}");
                Console.WriteLine($"URL: {response.Url}");
                Console.WriteLine($"Extension: {response.Extension}");
            }
            catch (Grpc.Core.RpcException e)
            {
                Console.WriteLine($"Error: {e.Status.Detail}");
            }
            Console.WriteLine();
        }

        static async Task AddPhoto(PhotoService.PhotoServiceClient client)
        {
            Console.Write("Enter Album ID: ");
            var albumId = int.Parse(Console.ReadLine());

            Console.Write("Enter Title: ");
            var title = Console.ReadLine();

            Console.Write("Enter Description: ");
            var description = Console.ReadLine();

            Console.Write("Enter URL: ");
            var url = Console.ReadLine();

            var request = new AddPhotoRequest
            {
                AlbumId = albumId,
                Title = title,
                Description = description,
                Url = url
            };
            try
            {
                var response = await client.AddPhotoAsync(request);
                Console.WriteLine($"Success: {response.Success}");
                Console.WriteLine($"Message: {response.Message}");
            }
            catch (Grpc.Core.RpcException e)
            {
                Console.WriteLine($"Error: {e.Status.Detail}");
            }
            Console.WriteLine();
        }

        static async Task UpdatePhoto(PhotoService.PhotoServiceClient client)
        {
            Console.Write("Enter Photo ID: ");
            var id = int.Parse(Console.ReadLine());

            Console.Write("Enter Album ID: ");
            var albumId = int.Parse(Console.ReadLine());

            Console.Write("Enter Title: ");
            var title = Console.ReadLine();

            Console.Write("Enter Description: ");
            var description = Console.ReadLine();

            var request = new UpdatePhotoRequest
            {
                Id = id,
                AlbumId = albumId,
                Title = title,
                Description = description
            };
            try
            {
                var response = await client.UpdatePhotoAsync(request);
                Console.WriteLine($"Success: {response.Success}");
                Console.WriteLine($"Message: {response.Message}");
            }
            catch (Grpc.Core.RpcException e)
            {
                Console.WriteLine($"Error: {e.Status.Detail}");
            }
            Console.WriteLine();
        }

        static async Task SoftDeletePhoto(PhotoService.PhotoServiceClient client)
        {
            Console.Write("Enter Photo ID: ");
            var id = int.Parse(Console.ReadLine());

            var request = new SoftDeletePhotoRequest { Id = id };
            try
            {
                var response = await client.SoftDeletePhotoAsync(request);
                Console.WriteLine($"Success: {response.Success}");
                Console.WriteLine($"Message: {response.Message}");
            }
            catch (Grpc.Core.RpcException e)
            {
                Console.WriteLine($"Error: {e.Status.Detail}");
            }
            Console.WriteLine();
        }
    }
}