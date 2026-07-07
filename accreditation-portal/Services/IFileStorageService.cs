namespace accreditation_portal.Services
{
    public record StoredFile(string StoredFileName, string FilePath, long FileSizeBytes, string ContentType);

    public interface IFileStorageService
    {
        Task<StoredFile> SaveAsync(IFormFile file, string subFolder, CancellationToken cancellationToken = default);

        void Delete(string filePath);

        Stream OpenRead(string filePath);
    }
}
