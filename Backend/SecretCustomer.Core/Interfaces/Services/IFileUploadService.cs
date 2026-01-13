namespace SecretCustomer.Core.Interfaces.Services;

public interface IFileUploadService
{
    // Answer attachments (soru bazlı - eski)
    Task<FileUploadResult> UploadAnswerAttachmentAsync(int answerId, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAnswerAttachmentAsync(int answerId);
    Task<(Stream FileStream, string FileName, string ContentType)?> GetAnswerAttachmentAsync(int answerId);

    // Evaluation attachments (değerlendirme geneli - yeni)
    Task<FileUploadResult> UploadEvaluationAttachmentAsync(int evaluationId, Stream fileStream, string fileName, string contentType, int? uploadedByUserId = null);
    Task<bool> DeleteEvaluationAttachmentAsync(int attachmentId);
    Task<(Stream FileStream, string FileName, string ContentType)?> GetEvaluationAttachmentAsync(int attachmentId);
}

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSize { get; set; }
    public int? AttachmentId { get; set; }
}
