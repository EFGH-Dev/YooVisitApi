// --- N'oublie pas ces nouveaux usings ! ---
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options; // <-- Pour IOptions<T>
using System.Net;
using YooVisitApi.Configuration; // <-- Ta classe POCO
using YooVisitApi.Interfaces;
// (Tu n'as plus besoin de Amazon.Runtime ou Microsoft.AspNetCore.Mvc)

namespace YooVisitApi.Services
{
    public class S3StorageService : IObjectStorageService
    {
        // 1. DÉPENDANCES PROPRES (injectées)
        private readonly IAmazonS3 _s3Client;
        private readonly ObjectStorageSettings _settings;

        // 2. LE NOUVEAU CONSTRUCTEUR (propre)
        // Il reçoit les objets au lieu de les fabriquer.
        public S3StorageService(IAmazonS3 s3Client, IOptions<ObjectStorageSettings> storageOptions)
        {
            this._s3Client = s3Client; // Reçu de la DI
            this._settings = storageOptions.Value; // Reçu de la DI
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                // 3. UTILISATION FORTEMENT TYPÉE
                BucketName = this._settings.BucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = contentType,

                // 4. FIX DE SÉCURITÉ
                // Les fichiers sont privés. L'accès se fait par URL pré-signée.
                CannedACL = S3CannedACL.Private
            };

            PutObjectResponse response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return fileName;
            }

            throw new Exception($"Erreur lors de l'upload du fichier vers S3. Status: {response.HttpStatusCode}");
        }

        public async Task DeleteFileAsync(string fileKey)
        {
            DeleteObjectRequest request = new DeleteObjectRequest
            {
                BucketName = this._settings.BucketName, // Fortement typé
                Key = fileKey
            };
            await _s3Client.DeleteObjectAsync(request);
        }

        public string GeneratePresignedUploadUrl(string fileName, string contentType)
        {
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
            {
                BucketName = this._settings.BucketName, // Fortement typé
                Key = fileName,
                Verb = HttpVerb.PUT,
                ContentType = contentType,
                Expires = DateTime.UtcNow.AddMinutes(5)
            };

            return this._s3Client.GetPreSignedURL(request);
        }

        public string GeneratePresignedGetUrl(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey))
            {
                return string.Empty; // Gère le cas d'une image non définie
            }

            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
            {
                // 5. FIX DE BUG (plus de hardcoding)
                BucketName = this._settings.BucketName, // Fortement typé
                Key = fileKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(7)
            };
            return this._s3Client.GetPreSignedURL(request);
        }

        public async Task RenameFileAsync(string oldKey, string newKey)
        {
            // 5. FIX DE BUG (plus de hardcoding)
            string bucketName = this._settings.BucketName; // Fortement typé

            CopyObjectRequest copyRequest = new CopyObjectRequest
            {
                SourceBucket = bucketName,
                SourceKey = oldKey,
                DestinationBucket = bucketName,
                DestinationKey = newKey
            };
            await _s3Client.CopyObjectAsync(copyRequest);

            DeleteObjectRequest deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = oldKey
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}