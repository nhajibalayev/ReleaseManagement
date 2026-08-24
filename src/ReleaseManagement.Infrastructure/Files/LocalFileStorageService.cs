using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.Files;

public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".zip", ".log"
    };

    private readonly FileStorageOptions _options;
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<FileStorageResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed.");
        }

        if (stream.CanSeek && stream.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The uploaded file exceeds the allowed size.");
        }

        var root = ResolveRootPath();
        Directory.CreateDirectory(root);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var absoluteFolder = Path.Combine(root, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, storedFileName);
        await using (var fileStream = File.Create(absolutePath))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var fileInfo = new FileInfo(absolutePath);
        if (fileInfo.Length > _options.MaxFileSizeBytes)
        {
            File.Delete(absolutePath);
            throw new InvalidOperationException("The uploaded file exceeds the allowed size.");
        }

        var storagePath = Path.Combine(relativeFolder, storedFileName).Replace('\\', '/');
        return new FileStorageResult(
            storagePath,
            storedFileName,
            fileInfo.Length,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(storagePath);
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(storagePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string ResolveRootPath()
    {
        return Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(_environment.ContentRootPath, _options.RootPath);
    }

    private string GetAbsolutePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath) ||
            storagePath.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        var absolutePath = Path.GetFullPath(
            Path.Combine(ResolveRootPath(), storagePath.Replace('/', Path.DirectorySeparatorChar)));

        var root = Path.GetFullPath(ResolveRootPath());
        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return absolutePath;
    }
}
