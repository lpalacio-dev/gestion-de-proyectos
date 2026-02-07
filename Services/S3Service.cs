using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace gestion_de_proyectos.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _bucketName = _configuration["S3_BUCKET_NAME"]
                ?? throw new InvalidOperationException("S3_BUCKET_NAME no configurado");
        }

        public async Task<string> UploadFileAsync(
            Stream stream,
            string fileName,
            string folder,
            string contentType)
        {
            // Generar un nombre único para el archivo
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var fileKey = $"{folder}/{uniqueFileName}";

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = fileKey,
                BucketName = _bucketName,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private // Archivo privado
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);

            // Retornar la clave del archivo (no la URL pública porque es privado)
            return fileKey;
        }

        public async Task<string> GetPresignedUrlAsync(string fileKey, int expiresInMinutes = 60)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
                Verb = HttpVerb.GET
            };

            return await Task.FromResult(_s3Client.GetPreSignedURL(request));
        }

        public async Task DeleteFileAsync(string fileKey)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
        }

        public async Task<bool> FileExistsAsync(string fileKey)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}