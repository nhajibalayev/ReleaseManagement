namespace ReleaseManagement.Application.Abstractions;

public sealed record FileStorageResult(
    string StoragePath,
    string StoredFileName,
    long FileSize,
    string ContentType);

public interface IFileStorageService
{
    Task<FileStorageResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
