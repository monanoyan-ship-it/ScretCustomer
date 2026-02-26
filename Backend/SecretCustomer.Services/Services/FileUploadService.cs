using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IAnswerRepository _answerRepository;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogService _auditLogService;
    private readonly string _answersUploadPath;
    private readonly string _evaluationsUploadPath;
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".mp3", ".wav", ".mp4" };
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50 MB

    public FileUploadService(
        IAnswerRepository answerRepository,
        ApplicationDbContext context,
        IConfiguration configuration,
        IAuditLogService auditLogService)
    {
        _answerRepository = answerRepository;
        _context = context;
        _configuration = configuration;
        _auditLogService = auditLogService;
        var basePath = _configuration["FileUpload:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _answersUploadPath = Path.Combine(basePath, "answers");
        _evaluationsUploadPath = Path.Combine(basePath, "evaluations");
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public async Task<FileUploadResult> UploadAnswerAttachmentAsync(int answerId, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // Ensure directory exists
            EnsureDirectoryExists(_answersUploadPath);

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Dosya uzantısı desteklenmiyor. İzin verilen uzantılar: {string.Join(", ", _allowedExtensions)}"
                };
            }

            // Validate file size
            if (fileStream.Length > _maxFileSize)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Dosya boyutu çok büyük. Maksimum boyut: {_maxFileSize / (1024 * 1024)} MB"
                };
            }

            // Get answer
            var answer = await _answerRepository.GetByIdAsync(answerId);
            if (answer == null)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Cevap bulunamadı"
                };
            }

            // Delete existing file if any
            if (!string.IsNullOrEmpty(answer.AttachmentPath) && File.Exists(answer.AttachmentPath))
            {
                File.Delete(answer.AttachmentPath);
            }

            // Generate unique filename
            var uniqueFileName = $"{answerId}_{TurkeyTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(_answersUploadPath, uniqueFileName);

            // Save file
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            // Update answer
            answer.AttachmentPath = filePath;
            answer.AttachmentFileName = fileName;
            await _answerRepository.UpdateAsync(answer);

            await _auditLogService.LogInfoAsync($"File uploaded for answer {answerId}: {fileName}", "FileUpload");

            return new FileUploadResult
            {
                Success = true,
                FilePath = filePath,
                FileName = fileName,
                FileSize = fileStream.Length
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error uploading file for answer {answerId}", "FileUpload", ex);
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "Dosya yüklenirken bir hata oluştu"
            };
        }
    }

    public async Task<bool> DeleteAnswerAttachmentAsync(int answerId)
    {
        try
        {
            var answer = await _answerRepository.GetByIdAsync(answerId);
            if (answer == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(answer.AttachmentPath) && File.Exists(answer.AttachmentPath))
            {
                File.Delete(answer.AttachmentPath);
            }

            answer.AttachmentPath = null;
            answer.AttachmentFileName = null;
            await _answerRepository.UpdateAsync(answer);

            await _auditLogService.LogInfoAsync($"File deleted for answer {answerId}", "FileUpload");
            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting file for answer {answerId}", "FileUpload", ex);
            return false;
        }
    }

    public async Task<(Stream FileStream, string FileName, string ContentType)?> GetAnswerAttachmentAsync(int answerId)
    {
        try
        {
            var answer = await _answerRepository.GetByIdAsync(answerId);
            if (answer == null || string.IsNullOrEmpty(answer.AttachmentPath))
            {
                return null;
            }

            if (!File.Exists(answer.AttachmentPath))
            {
                return null;
            }

            var fileStream = new FileStream(answer.AttachmentPath, FileMode.Open, FileAccess.Read);
            var fileName = answer.AttachmentFileName ?? Path.GetFileName(answer.AttachmentPath);
            var contentType = GetContentType(answer.AttachmentPath);

            return (fileStream, fileName, contentType);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting file for answer {answerId}", "FileUpload", ex);
            return null;
        }
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }

    // ===== EVALUATION ATTACHMENT METHODS =====

    public async Task<FileUploadResult> UploadEvaluationAttachmentAsync(int evaluationId, Stream fileStream, string fileName, string contentType, int? uploadedByUserId = null)
    {
        try
        {
            // Ensure directory exists
            EnsureDirectoryExists(_evaluationsUploadPath);

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Dosya uzantısı desteklenmiyor. İzin verilen uzantılar: {string.Join(", ", _allowedExtensions)}"
                };
            }

            // Validate file size
            if (fileStream.Length > _maxFileSize)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Dosya boyutu çok büyük. Maksimum boyut: {_maxFileSize / (1024 * 1024)} MB"
                };
            }

            // Check evaluation exists
            var evaluation = await _context.Evaluations.FindAsync(evaluationId);
            if (evaluation == null)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Değerlendirme bulunamadı"
                };
            }

            // Generate unique filename
            var uniqueFileName = $"eval_{evaluationId}_{TurkeyTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_evaluationsUploadPath, uniqueFileName);

            // Save file
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            // Create attachment record
            var attachment = new EvaluationAttachment
            {
                EvaluationId = evaluationId,
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileStream.Length,
                ContentType = contentType,
                UploadedByUserId = uploadedByUserId
            };

            _context.EvaluationAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            await _auditLogService.LogInfoAsync($"File uploaded for evaluation {evaluationId}: {fileName}, AttachmentId: {attachment.Id}", "FileUpload");

            return new FileUploadResult
            {
                Success = true,
                FilePath = filePath,
                FileName = fileName,
                FileSize = fileStream.Length,
                AttachmentId = attachment.Id
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error uploading file for evaluation {evaluationId}", "FileUpload", ex);
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "Dosya yüklenirken bir hata oluştu"
            };
        }
    }

    public async Task<bool> DeleteEvaluationAttachmentAsync(int attachmentId)
    {
        try
        {
            var attachment = await _context.EvaluationAttachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return false;
            }

            // Delete physical file
            if (!string.IsNullOrEmpty(attachment.FilePath) && File.Exists(attachment.FilePath))
            {
                File.Delete(attachment.FilePath);
            }

            // Delete record
            _context.EvaluationAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            await _auditLogService.LogInfoAsync($"File deleted: AttachmentId {attachmentId}, EvaluationId {attachment.EvaluationId}", "FileUpload");
            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting attachment {attachmentId}", "FileUpload", ex);
            return false;
        }
    }

    public async Task<(Stream FileStream, string FileName, string ContentType)?> GetEvaluationAttachmentAsync(int attachmentId)
    {
        try
        {
            var attachment = await _context.EvaluationAttachments.FindAsync(attachmentId);
            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath))
            {
                return null;
            }

            if (!File.Exists(attachment.FilePath))
            {
                return null;
            }

            var fileStream = new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);
            return (fileStream, attachment.FileName, attachment.ContentType);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting attachment {attachmentId}", "FileUpload", ex);
            return null;
        }
    }
}
