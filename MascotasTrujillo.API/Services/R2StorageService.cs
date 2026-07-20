using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;

namespace MascotasTrujillo.API.Services
{
    public class R2StorageService
    {
        private readonly IConfiguration _config;
        private readonly AmazonS3Client _s3Client;

        public R2StorageService(IConfiguration config)
        {
            _config = config;

            var accessKey = _config["CloudflareR2:AccessKey"];
            var secretKey = _config["CloudflareR2:SecretKey"];

            var s3Config = new AmazonS3Config
            {
                ServiceURL = _config["CloudflareR2:ServiceUrl"],
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        public async Task<string> SubirFotoAsync(IFormFile archivo, string carpeta = "")
        {
            if (archivo == null || archivo.Length == 0)
                throw new Exception("El archivo está vacío.");

            if (!archivo.ContentType.StartsWith("image/"))
                throw new Exception("El archivo debe ser una imagen.");

            const long tamanioMaximo = 5 * 1024 * 1024; // 5 MB

            if (archivo.Length > tamanioMaximo)
                throw new Exception("La imagen no debe superar los 5 MB.");

            var bucketName = _config["CloudflareR2:BucketName"];
            var dominioPublico = _config["CloudflareR2:PublicDomain"]?.TrimEnd('/');

            var extension = Path.GetExtension(archivo.FileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var nombreUnico = $"{Guid.NewGuid()}{extension}";

            string key = string.IsNullOrWhiteSpace(carpeta)
                ? nombreUnico
                : $"{carpeta.Trim('/')}/{nombreUnico}";

            using var newMemoryStream = new MemoryStream();
            await archivo.CopyToAsync(newMemoryStream);
            newMemoryStream.Position = 0;

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = newMemoryStream,
                Key = key,
                BucketName = bucketName,
                ContentType = archivo.ContentType,
                DisablePayloadSigning = true
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);

            return $"{dominioPublico}/{key}";
        }
    }
}
