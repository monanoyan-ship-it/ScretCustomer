namespace SecretCustomer.Core.DTOs.Question;

/// <summary>
/// Soru eki DTO
/// </summary>
public class QuestionAttachmentDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UploadedByUserName { get; set; }

    // Computed
    public string FileSizeFormatted => FormatFileSize(FileSize);
    public string DownloadUrl => $"/api/question-attachments/{Id}/download";

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Dosya yükleme sonucu
/// </summary>
public class UploadAttachmentResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public QuestionAttachmentDto? Attachment { get; set; }
}
