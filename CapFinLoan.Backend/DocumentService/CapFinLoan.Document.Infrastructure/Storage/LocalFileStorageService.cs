using CapFinLoan.Document.Application.Interfaces;

namespace CapFinLoan.Document.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRootPath;

    public LocalFileStorageService(string storageRootPath)
    {
        _storageRootPath = storageRootPath;
        if (!Directory.Exists(_storageRootPath))
        {
            Directory.CreateDirectory(_storageRootPath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_storageRootPath, uniqueFileName);

        using var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);

        return uniqueFileName;
    }

    public async Task<byte[]> GetFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storageRootPath, storagePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found in storage.");
        }

        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storageRootPath, storagePath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
