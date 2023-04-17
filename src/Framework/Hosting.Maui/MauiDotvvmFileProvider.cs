using DotVVM.Framework.Configuration;

namespace DotVVM.Framework.Hosting.Maui;

public class MauiDotvvmFileProvider : IDotvvmFileProvider
{
    private readonly DotvvmConfiguration configuration;

    public MauiDotvvmFileProvider(DotvvmConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public Task<Stream> OpenFileAsync(string path)
        => FileSystem.OpenAppPackageFileAsync(path);

    public Task<bool> FileExistsAsync(string path)
        => FileSystem.AppPackageFileExistsAsync(path);

    public async Task CopyFileToAppDataAsync(string path)
    {
        // check if file is bundled
        var fileExists = await FileExistsAsync(path);
        if (!fileExists)
        {
            throw new FileNotFoundException();
        }

        // read file from app package
        var fileStream = await OpenFileAsync(path);
        using var reader = new StreamReader(fileStream);
        var fileContent = await reader.ReadToEndAsync();

        // create directories in app data
        var dirPath = Path.GetDirectoryName(path);
        var appDataDirPath = Path.Combine(configuration.ApplicationPhysicalPath, dirPath);
        Directory.CreateDirectory(appDataDirPath);

        // create file in app data
        var appDataFilePath = Path.Combine(configuration.ApplicationPhysicalPath, path);
        await File.WriteAllTextAsync(appDataFilePath, fileContent);
    }
}
