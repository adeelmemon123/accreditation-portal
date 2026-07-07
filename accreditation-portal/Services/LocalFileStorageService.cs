namespace accreditation_portal.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        // Checked against the file's actual bytes, not just its extension/declared content-type.
        private static readonly Dictionary<string, byte[][]> SignaturesByExtension = new()
        {
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } }
        };

        private readonly string _rootPath;

        public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configuredPath = configuration["AppData:UploadsPath"] ?? "App_Data/uploads";
            _rootPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath);

            Directory.CreateDirectory(_rootPath);
        }

        public async Task<StoredFile> SaveAsync(IFormFile file, string subFolder, CancellationToken cancellationToken = default)
        {
            if (file.Length <= 0)
            {
                throw new ApplicationOperationException("The selected file is empty.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ApplicationOperationException("File exceeds the 5 MB size limit.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!SignaturesByExtension.TryGetValue(extension, out var validSignatures))
            {
                throw new ApplicationOperationException("Unsupported file type. Allowed formats: PDF, JPG, PNG.");
            }

            await using var stream = file.OpenReadStream();
            var header = new byte[8];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            var matchesSignature = validSignatures.Any(signature =>
                bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));

            if (!matchesSignature)
            {
                throw new ApplicationOperationException("File content does not match its extension.");
            }

            var folder = Path.Combine(_rootPath, subFolder);
            Directory.CreateDirectory(folder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(folder, storedFileName);

            stream.Seek(0, SeekOrigin.Begin);
            await using (var target = File.Create(fullPath))
            {
                await stream.CopyToAsync(target, cancellationToken);
            }

            var relativePath = Path.Combine(subFolder, storedFileName).Replace('\\', '/');
            return new StoredFile(storedFileName, relativePath, file.Length, file.ContentType);
        }

        public void Delete(string filePath)
        {
            var fullPath = Path.Combine(_rootPath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public Stream OpenRead(string filePath)
        {
            var fullPath = Path.Combine(_rootPath, filePath);
            return File.OpenRead(fullPath);
        }
    }
}
