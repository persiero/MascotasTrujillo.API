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

            // Aquí le decimos a la librería de Amazon que apunte a Cloudflare
            var s3Config = new AmazonS3Config
            {
                ServiceURL = _config["CloudflareR2:ServiceUrl"],
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        public async Task<string> SubirFotoAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                throw new Exception("El archivo está vacío.");

            var bucketName = _config["CloudflareR2:BucketName"];
            var dominioPublico = _config["CloudflareR2:PublicDomain"];

            // Generamos un nombre único para que no se sobreescriban fotos con el mismo nombre
            var extension = Path.GetExtension(archivo.FileName);
            var nombreUnico = $"{Guid.NewGuid()}{extension}";

            using var newMemoryStream = new MemoryStream();
            await archivo.CopyToAsync(newMemoryStream);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = newMemoryStream,
                Key = nombreUnico,
                BucketName = bucketName,
                ContentType = archivo.ContentType,
                DisablePayloadSigning = true // Optimización recomendada para R2
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);

            // Devolvemos la URL pública final para guardarla en la base de datos
            return $"{dominioPublico}/{nombreUnico}";
        }
    }
}
