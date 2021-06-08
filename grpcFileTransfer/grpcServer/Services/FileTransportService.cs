using System;
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using grpcFileTransportServer;
using Microsoft.AspNetCore.Hosting;

namespace grpcServer.Services
{
    public class FileTransportService : FileService.FileServiceBase
    {

        //wwwrooot klasörüne ulaşmamızı sağlayan gerekli dizinin pathini bize vericek olan fonksiyon

        readonly IWebHostEnvironment _webHostEnvironment;
        public FileTransportService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }


        public override async Task<Google.Protobuf.WellKnownTypes.Empty> FileUpload(Grpc.Core.IAsyncStreamReader<BytesContent> requestStream, Grpc.Core.ServerCallContext context)
        {

            //srteam'in yapılacağı dizini belirleyebilmek için 
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");
            //böyle bir klasör yoksa oluştur
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);


            //stream edilecek datayı alacak file stream nesnesini oluşturmak için
            FileStream fileStream = null;
            try
            {
                int count = 0;
                decimal chunkSize = 0;
                //while döngüsüyle gelecek olan stream datayı yakalarız
                while (await requestStream.MoveNext())
                {
                    if (count++ == 0)
                    {
                        fileStream = new FileStream($"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtension}", FileMode.CreateNew);
                        //gerekli olan alan tahsisinde bulunabilmek için 
                        fileStream.SetLength(requestStream.Current.FileSize);
                    }

                    //gelen bufferları yakalamak için
                    var buffer = requestStream.Current.Buffer.ToByteArray();

                    //ilgili streamde tektek gelen parçaları topluyorum

                    await fileStream.WriteAsync(buffer, 0, buffer.Length);

                    System.Console.WriteLine($"{Math.Round(((chunkSize += requestStream.Current.ReadedByte) * 100) / requestStream.Current.FileSize)}%");



                }
            }
            catch
            {
                // TODO
            }
            await fileStream.DisposeAsync();
            fileStream.Close();
            return new Google.Protobuf.WellKnownTypes.Empty();

        }


        public override async Task FileDownload(grpcFileTransportServer.FileInfo request, Grpc.Core.IServerStreamWriter<BytesContent> responseStream, Grpc.Core.ServerCallContext context)
        {
            //path tanımlamak için
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");

            using FileStream fileStream = new FileStream($"{path}/{request.FileName}{request.FileExtension}", FileMode.Open, FileAccess.Read);

            //ne kadar bytelık bir dosya göndereceksek bufferimizi tasarlamamız gerekiyor

            byte[] buffer = new byte[2048];

            BytesContent content = new BytesContent
            {
                FileSize = fileStream.Length,
                Info = new grpcFileTransportServer.FileInfo { FileName = Path.GetFileNameWithoutExtension(fileStream.Name), FileExtension = Path.GetExtension(fileStream.Name) },
                ReadedByte = 0
            };

            while ((content.ReadedByte = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                content.Buffer = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(content);
            }
            fileStream.Close();


        }
    }

}