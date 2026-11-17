using Google.Apis.Upload;
using OOP_Lab1.Models;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Maui.Storage;


namespace OOP_Lab1.Services;

public class GoogleDriveService
{
    private DriveService? service;

    private static string CredentialsPath =>
        Path.Combine(
            FileSystem.AppDataDirectory,
            "google_client.json");

    private static string TokensPath =>
        Path.Combine(
            FileSystem.AppDataDirectory,
            "google_tokens");

    public bool IsConnected => service != null;

    public async Task ConnectAsync(Stream? credentials = null)
    {
        if (credentials != null)
        {
            using var output = File.Create(CredentialsPath);
            await credentials.CopyToAsync(output);
        }

        if (!File.Exists(CredentialsPath))
        {
            throw new FileNotFoundException(
                "Не знайдено налаштування Google OAuth");
        }

        using var input = File.OpenRead(CredentialsPath);

        var credential =
            await GoogleWebAuthorizationBroker.AuthorizeAsync(
                input,
                new[] { DriveService.Scope.DriveFile },
                "sheetflow-user",
                CancellationToken.None,
                new FileDataStore(TokensPath, true));

        service = new DriveService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "SheetFlow"
            });
    }

    public DriveService GetService()
    {
        return service ?? throw new InvalidOperationException(
            "Спочатку підключіть Google Drive");
    }

    public async Task<string> UploadTableAsync(
        Spreadsheet spreadsheet,
        string fileName)
    {
        using var stream = SpreadsheetStorage.CreateStream(spreadsheet);

        var file = new DriveFile
        {
            Name = fileName,
            MimeType = "application/json"
        };

        var request = GetService().Files.Create(
            file,
            stream,
            "application/json");

        request.Fields = "id,name";

        var progress = await request.UploadAsync();

        if (progress.Status != UploadStatus.Completed)
        {
            throw new IOException(
                progress.Exception?.Message ??
                "Не вдалося завантажити файл у Google Drive");
        }

        return request.ResponseBody?.Id
            ?? throw new IOException("Не отримано ідентифікатор файлу");
    }

    public async Task<IList<DriveFile>> ListTablesAsync()
    {
        var request = GetService().Files.List();

        request.Q = "mimeType = 'application/json' and trashed = false";
        request.Fields = "nextPageToken,files(id,name,mimeType)";
        request.OrderBy = "modifiedTime desc";
        request.PageSize = 100;

        var result = await request.ExecuteAsync();

        return result.Files ?? new List<DriveFile>();
    }

    public async Task<Spreadsheet> DownloadTableAsync(string fileId)
    {
        using var stream = new MemoryStream();

        var request = GetService().Files.Get(fileId);

        var progress = await request.DownloadAsync(stream);

        if (progress.Status != Google.Apis.Download.DownloadStatus.Completed)
        {
            throw new IOException(
                progress.Exception?.Message ??
                "Не вдалося завантажити файл");
        }

        stream.Position = 0;

        return await SpreadsheetStorage.LoadAsync(stream);
    }

}
