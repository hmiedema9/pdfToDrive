using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using static System.Web.MimeMapping;
using System.Linq;
using System.Threading;

namespace SaveToDrive
{
    class Program
    {
        
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "Save to Drive Application";
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify a file to upload");
                Environment.Exit(0);
            }
            string path = args[0];
            string mime = GetMimeMapping(path);
            // command line argument for id of dest. folder via Google Drive (i.e. https://drive.google.com/drive/u/0/folders/0ByaNmaYYqIiYYko4ZnROdTgydm8 w/ 0ByaNmaYYqIiYYko4ZnROdTgydm8 being the id)
            string folder = args[1];
            UserCredential credential;
            using (var stream =
                new System.IO.FileStream("client_secret.json", System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = System.IO.Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            var fileId = doesFileExist(System.IO.Path.GetFileName(path), folder, listFiles(service, folder));

            if (!fileId.Equals("0"))
            {
                updateFile(service, path, fileId, mime);
            }
            else {
                createFile(service,folder,path,mime);
            }
            //_parent is the id of the folder, _fileId is the fileId shareable link

            //TODO: Check if file exists on Drive, if it does not create it, if it does call update instead
            //updateFile(service, path, "0ByaNmaYYqIiYYko4ZnROdTgydm8", "0ByaNmaYYqIiYSlptcmQwRVJtckU", mime);
            //createFile(service,folder, path, mime);
            Console.WriteLine("Hooray! You uploaded your file");
            Console.Read();
        }

        /// <summary>
        /// The createFile method is called if the file does not yet exist. The method uses the CreateMediaUpload method to 
        /// create a request to make a new file. The file is made with the parsed file name, and the mime type of the file.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="folder"></param>
        /// <param name="createFile"></param>
        /// <param name="mime"></param>
        /// <returns></returns>
        public static File createFile(DriveService service, string folder, string createFile, string mime) {
            if (System.IO.File.Exists(createFile))
            {
                // creates a new file and adds info to it with the body information
                File body = new File();

                body.Name = System.IO.Path.GetFileName(createFile);
                body.Description = "Created File from automatic Drive Script";
                body.MimeType = mime;
                body.Parents = new List<string>{ folder } ;
                // File's content.
                byte[] byteArray = System.IO.File.ReadAllBytes(createFile);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.CreateMediaUpload request = service.Files.Create(body, stream, mime);
                    request.KeepRevisionForever = true;
                    request.Upload();
                    return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            else
            {
                Console.WriteLine("File does not exist: " + createFile);
                return null;
            }
        }

        /// <summary>
        /// The updateFile method is called only when the file already exists within the given folder on Drive.
        /// The logic of the method is simple. It takes the appropriate information about a file and uses the 
        /// UpdateMediaUpload method to create a request that will update the given file with the correct ID.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="uploadFile"></param>
        /// <param name="fileId"></param>
        /// <param name="mime"></param>
        /// <returns></returns>
        public static File updateFile(DriveService service, string uploadFile, string fileId, string mime)
        {
            if (System.IO.File.Exists(uploadFile))
            {
                // creates a new file and adds info to it with the body information
                File body = new File();

                body.Name = System.IO.Path.GetFileName(uploadFile);
                body.Description = "Updated File from automatic Drive Script";
                body.MimeType = mime;

                // File's content.
                byte[] byteArray = System.IO.File.ReadAllBytes(uploadFile);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.UpdateMediaUpload request = service.Files.Update(body, fileId, stream, body.MimeType);
                    request.KeepRevisionForever = true;
                    request.Upload();
                    return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            else
            {
                Console.WriteLine("File does not exist: " + uploadFile);
                return null;
            }
        }

        /// <summary>
        /// Helper method to check if the command line file given exists on Drive yet. If the file
        /// does exist, the id of the file within Drive is returned. If the file does not exist,
        /// a 0 string is returned which is used as a flag to call the createFile method rather than
        /// the updateFile method.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="parent"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string doesFileExist(string fileName,string parent,IList<File> list) {
            if (list != null && list.Count > 0)
            {
                foreach (var file in list)
                {
                    //check if file exists in given folder
                    if ((file.Parents != null) && fileName.Equals(file.Name) && parent.Equals(file.Parents[0])) {
                        return file.Id;
                    }
                }
            }
            return "0";
        }

        /// <summary>
        /// Helper method used to iterate through all of the files in Drive.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static IList<File> listFiles(DriveService service, string folder)
        {
            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();

            // used for only generating a list of the files in the given folder (i.e. https://developers.google.com/drive/v3/web/search-parameters#examples)
            listRequest.Q = "'" + folder + "'" + " in parents";
            listRequest.Fields = "nextPageToken, files(id, name, parents)";
           
            // List files.
            var files = listRequest.Execute().Files;
            Console.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    try
                    {

                        Console.WriteLine("{0} id:({1}) parent:({2})", file.Name, file.Id, file.Parents[0]);
                    }
                    catch(NullReferenceException e)
                    {
                        Console.WriteLine("{0} id:({1})", file.Name, file.Id);
                    }
                }
            }
            else
            {
                Console.WriteLine("No files found.");
            }
            return files;
        } 
    }
}
