using SecretCustomer.Core.DTOs.Question;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IQuestionAttachmentService
{
    Task<List<QuestionAttachmentDto>> GetByQuestionAsync(int questionId);
    Task<bool> QuestionExistsAsync(int questionId);
    Task<QuestionAttachmentDto> UploadAsync(int questionId, string fileName, string storedFileName, string filePath, long fileSize, string contentType, string? description, int? uploadedByUserId);
    Task<(string FilePath, string ContentType, string FileName)?> GetDownloadInfoAsync(int id);
    Task<(bool Success, string? FilePath)> DeleteAsync(int id);
    Task<bool> UpdateOrderAsync(int id, int newOrder);
}
