using System.Threading;
using System.IO;
using System;
using Grpc.Net.Client;
// using grpcFileTransportClient;
using static grpcFileTransportDownloadClient.FileService;
using Grpc.Core;
using System.Threading.Tasks;

namespace grpcDownloadClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new FileServiceClient(channel);

            string downloadPath = @"C:\Users\harmonyerp\Desktop\grpcFileTransfer\grpcDownloadClient\DownloadFiles";

            //download yapacağımız dosyayı bildirmek için

            var fileInfo = new grpcFileTransportDownloadClient.FileInfo
            {
                FileExtension = ".mp4",
                FileName = "gRPCKutuphanesi"
            };

            FileStream fileStream = null;
            var download = client.FileDownload(fileInfo);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            int count = 0;
            decimal chunkSize = 0;
            while (await download.ResponseStream.MoveNext(cancellationTokenSource.Token))
            {
                if (count++ == 0)
                {
                    fileStream = new FileStream(@$"{downloadPath}\{download.ResponseStream.Current.Info.FileName}{download.ResponseStream.Current.Info.FileExtension}", FileMode.CreateNew);
                    fileStream.SetLength(download.ResponseStream.Current.FileSize);
                }

                var buffer = download.ResponseStream.Current.Buffer.ToByteArray();
                await fileStream.WriteAsync(buffer, 0, download.ResponseStream.Current.ReadedByte);

                System.Console.WriteLine($"{Math.Round(((chunkSize += download.ResponseStream.Current.ReadedByte) * 100) / download.ResponseStream.Current.FileSize)}%");



            }
            System.Console.WriteLine("Yüklendi...");
            await fileStream.DisposeAsync();
            fileStream.Close();


        }
    }
}
