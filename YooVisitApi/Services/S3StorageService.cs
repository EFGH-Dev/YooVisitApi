using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using YooVisitApi.Dtos.Storage;
using YooVisitApi.Interfaces;

namespace YooVisitApi.Services
{
    public class S3StorageService : IObjectStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName = string.Empty;
        private readonly string _serviceUrl = string.Empty;

        public S3StorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            var accessKey = _configuration["ObjectStorage:AccessKey"];
            var secretKey = _configuration["ObjectStorage:SecretKey"];
            _serviceUrl = _configuration["ObjectStorage:ServiceUrl"] ?? throw new ArgumentNullException("ServiceUrl");
            _bucketName = _configuration["ObjectStorage:BucketName"] ?? throw new ArgumentNullException("BucketName");

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = _serviceUrl,
                AuthenticationRegion = "GRA",
                ForcePathStyle = true,
            };
            _s3Client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var fileKey = $"{Guid.NewGuid()}-{fileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            var response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return fileKey; // On retourne seulement la clé unique du fichier
            }

            throw new Exception($"Erreur lors de l'upload du fichier vers S3. Status: {response.HttpStatusCode}");
        }

        public async Task DeleteFileAsync(string fileKey)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };
            await _s3Client.DeleteObjectAsync(request);
        }

        public string GeneratePresignedUploadUrl(string fileName, string contentType)
        {
            var fileKey = $"{Guid.NewGuid()}-{fileName}";

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                Verb = HttpVerb.PUT,
                ContentType = contentType,
                Expires = DateTime.UtcNow.AddMinutes(5) // L'URL est valide pour 5 minutes
            };

            string presignedUrl = _s3Client.GetPreSignedURL(request);
            return presignedUrl;
        }

        public string GeneratePresignedGetUrl(string fileKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = "yoovisit-photos",
                Key = fileKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(7) // URL valide 7 heures
            };
            return _s3Client.GetPreSignedURL(request);
        }

        public async Task RenameFileAsync(string oldKey, string newKey)
        {
            var bucketName = "yoovisit-photos"; // Ou depuis ta config

            // Étape A : Copier l'objet avec le nouveau nom
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = bucketName,
                SourceKey = oldKey,
                DestinationBucket = bucketName,
                DestinationKey = newKey
            };
            await _s3Client.CopyObjectAsync(copyRequest);

            // Étape B : Supprimer l'objet original
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = oldKey
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}