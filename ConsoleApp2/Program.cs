using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace DriveFileUpload
{
    class Program
    {
        // The Main method is the entry point of the application.
        // It's declared 'async' to allow for awaiting asynchronous operations like file uploads.
        static async Task Main(string[] args)
        {
            Console.WriteLine("Google Drive File Uploader");
            Console.WriteLine("==========================");

            // --- CONFIGURATION ---
            // Path to the client_secret.json file downloaded from Google Cloud Console.
            // Ensure this file is set to "Copy to Output Directory" in its properties in Visual Studio.
            string credentialPath = "client_secret2.json";

            // The ID of the Google Drive folder where files will be uploaded.
            // You can get this from the URL of the folder in your browser.
            string folderId = "1IgxP5s8qN6J_9Fwnr88LAs9ziCNAyF35"; // Your specific folder ID

            // An array of local file paths to be uploaded.
            string[] filesToUpload = {
                "bbc.jpg",
                "bbc2.jpg"
            };
            // --- END CONFIGURATION ---

            try
            {
                // Call the main upload logic.
                await UploadFiles(credentialPath, folderId, filesToUpload);
            }
            catch (Exception ex)
            {
                // Catch and display any errors that occur.
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("\nAll tasks complete. Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Authenticates the user and uploads the specified files to Google Drive.
        /// </summary>
        static async Task UploadFiles(string credentialPath, string folderId, string[] filePaths)
        {
            UserCredential credential;

            // This is the core authentication logic from the Google documentation.
            // It reads the client secret file and initiates the OAuth 2.0 authorization flow.
            using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                // The 'FileDataStore' is used to store the user's access and refresh tokens.
                // This prevents the user from having to re-authorize every time the application runs.
                // The folder "token.json" is created in the %APPDATA% directory.
                string tokenPath = "token.json";
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.ScopeConstants.DriveFile }, // Defines the permission level (can manage files created by the app)
                    "user",                                         // A unique identifier for the user's token
                    CancellationToken.None,
                    new FileDataStore(tokenPath, true));            // The path where the token is stored
            }

            Console.WriteLine($"\nAuthentication successful. Token data stored at: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "token.json")}");

            // Create the Google Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "C# Drive Uploader",
            });

            Console.WriteLine($"Starting upload to folder ID: {folderId}\n");

            // Loop through each file path provided and upload it.
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[SKIPPED] File not found: {filePath}");
                    continue;
                }

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new List<string> { folderId }
                };

                FilesResource.CreateMediaUpload request;

                // Create a new stream for each file to be uploaded.
                using (var uploadStream = new FileStream(filePath, FileMode.Open))
                {
                    // Create the upload request. The third parameter is the file's MIME type (e.g., "image/jpeg").
                    request = service.Files.Create(fileMetadata, uploadStream, "application/octet-stream");
                    request.Fields = "id, name"; // Specify which fields of the file resource to return after upload.

                    Console.WriteLine($"Uploading '{Path.GetFileName(filePath)}'...");
                    
                    // Await the asynchronous upload process and get the result.
                    var uploadResult = await request.UploadAsync();

                    if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Failed)
                    {
                        Console.WriteLine($"[ERROR] Uploading '{request.ResponseBody.Name}': {uploadResult.Exception.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"[SUCCESS] File '{request.ResponseBody.Name}' uploaded with ID: {request.ResponseBody.Id}");
                    }
                }
            }
        }
    }
}