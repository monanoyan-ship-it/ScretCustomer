namespace SecretCustomer.Core.Interfaces.Services;

public interface IFileUploadService
{
    Task<FileUploadResult> UploadAnswerAttachmentAsync(Guid answerId, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAnswerAttachmentAsync(Guid answerId);
    Task<(Stream FileStream, string FileName, string ContentType)?> GetAnswerAttachmentAsync(Guid answerId);
}

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSize { get; set; }
}
