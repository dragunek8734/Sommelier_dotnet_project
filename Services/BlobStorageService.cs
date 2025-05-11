using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace dotnetprojekt.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _reviewImagesContainer;
        private readonly string _profileImagesContainer;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger;
            _configuration = configuration;
            try
            {
                var connectionString = configuration["AzureBlobStorage:ConnectionString"];
                _reviewImagesContainer = configuration["AzureBlobStorage:ContainerName"];
                _profileImagesContainer = configuration["AzureBlobStorage:ProfileImagesContainer"] ?? "profileimages";
                
                _logger.LogInformation($"Initializing BlobStorageService with review container: {_reviewImagesContainer} and profile container: {_profileImagesContainer}");
                
                _blobServiceClient = new BlobServiceClient(connectionString);
                
                // Ensure the containers exist (this won't throw if they already exist)
                // Use private access (default) since public access is not permitted on this storage account
                var reviewContainerClient = _blobServiceClient.GetBlobContainerClient(_reviewImagesContainer);
                reviewContainerClient.CreateIfNotExists();
                
                var profileContainerClient = _blobServiceClient.GetBlobContainerClient(_profileImagesContainer);
                profileContainerClient.CreateIfNotExists();
                
                _logger.LogInformation("BlobStorageService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing BlobStorageService");
                throw;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string fileName, bool isProfileImage = false)
        {
            try
            {
                string containerName = isProfileImage ? _profileImagesContainer : _reviewImagesContainer;
                _logger.LogInformation($"Uploading file {fileName} to {containerName} container");
                
                // Get a reference to the container and blob
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                
                // Set content type based on file extension
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                };
                
                // Upload the file
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, blobHttpHeaders);
                }
                
                // Generate a SAS token for the blob that will last for 1 year
                string sasUrl = GenerateSasUrl(fileName, TimeSpan.FromDays(365), isProfileImage);
                
                // Return the blob's URL with SAS token
                _logger.LogInformation($"File {fileName} uploaded successfully to {containerName}. URL with SAS token generated.");
                return sasUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {fileName} to blob storage");
                throw;
            }
        }

        public async Task<Stream> DownloadImageAsync(string fileName, bool isProfileImage = false)
        {
            try
            {
                string containerName = isProfileImage ? _profileImagesContainer : _reviewImagesContainer;
                _logger.LogInformation($"Downloading file {fileName} from {containerName} container");
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                var response = await blobClient.DownloadAsync();
                
                _logger.LogInformation($"File {fileName} downloaded successfully from {containerName}");
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {fileName} from blob storage");
                throw;
            }
        }

        public async Task DeleteImageAsync(string fileName, bool isProfileImage = false)
        {
            try
            {
                string containerName = isProfileImage ? _profileImagesContainer : _reviewImagesContainer;
                _logger.LogInformation($"Deleting file {fileName} from {containerName} container");
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                var response = await blobClient.DeleteIfExistsAsync();
                
                _logger.LogInformation($"File {fileName} deletion status from {containerName}: {(response.Value ? "Deleted" : "Not found")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName} from blob storage");
                throw;
            }
        }

        public bool BlobExists(string fileName, bool isProfileImage = false)
        {
            try
            {
                string containerName = isProfileImage ? _profileImagesContainer : _reviewImagesContainer;
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                return blobClient.Exists();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if blob {fileName} exists");
                throw;
            }
        }

        public string GetBlobUrl(string fileName, bool isProfileImage = false)
        {
            // Generate a SAS token with 1 year expiration
            return GenerateSasUrl(fileName, TimeSpan.FromDays(365), isProfileImage);
        }
        
        /// <summary>
        /// Extracts the blob filename from a URL, even if it has a SAS token
        /// </summary>
        /// <param name="url">Full URL with or without SAS token</param>
        /// <returns>Just the filename portion</returns>
        public static string ExtractBlobNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;
                
            try
            {
                // Parse the URL
                var uri = new Uri(url);
                
                // Get just the path portion
                string path = uri.AbsolutePath;
                
                // Get the last segment which is the filename
                return Path.GetFileName(path);
            }
            catch
            {
                // If parsing fails, just return the original string
                return url;
            }
        }
        
        /// <summary>
        /// Generates a URL with a SAS token that provides read access to a blob
        /// </summary>
        /// <param name="fileName">Name of the blob</param>
        /// <param name="expiryTime">How long the SAS token should be valid</param>
        /// <param name="isProfileImage">Whether this is a profile image or a review image</param>
        /// <returns>Full URL with SAS token</returns>
        private string GenerateSasUrl(string fileName, TimeSpan expiryTime, bool isProfileImage = false)
        {
            try
            {
                string containerName = isProfileImage ? _profileImagesContainer : _reviewImagesContainer;
                
                // Get a reference to the container and blob
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                
                // Create a SAS token that's valid for the specified time
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = fileName,
                    Resource = "b", // b for blob
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiryTime)
                };
                
                // Set permissions to read only
                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                
                // Generate the SAS token
                var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
                {
                    Sas = sasBuilder.ToSasQueryParameters(
                        new Azure.Storage.StorageSharedKeyCredential(
                            containerClient.AccountName,
                            GetAccountKey()
                        )
                    )
                };
                
                return blobUriBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating SAS URL for blob {fileName}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the account key from the connection string
        /// </summary>
        /// <returns>Storage account key</returns>
        private string GetAccountKey()
        {
            try
            {
                // Parse connection string to extract account key
                var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.StartsWith("AccountKey="))
                    {
                        return part.Substring("AccountKey=".Length);
                    }
                }
                throw new Exception("Account key not found in connection string");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting account key from connection string");
                throw;
            }
        }
    }
}