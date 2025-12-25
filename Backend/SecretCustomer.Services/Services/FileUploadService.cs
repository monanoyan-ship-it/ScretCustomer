using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string _uploadPath;
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".mp3", ".wav", ".mp4" };
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50 MB

    public FileUploadService(
        IAnswerRepository answerRepository,
        IConfiguration configuration,
        ILogger<FileUploadService> logger)
    {
        _answerRepository = answerRepository;
        _configuration = configuration;
        _logger = logger;
        _uploadPath = _configuration["FileUpload:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "answers");

        // Ensure directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<FileUploadResult> UploadAnswerAttachmentAsync(int answerId, Stream fileStream, string fileName, string contentType)
    {
        try
        {
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
            var uniqueFileName = $"{answerId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            // Save file
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            // Update answer
            answer.AttachmentPath = filePath;
            answer.AttachmentFileName = fileName;
            await _answerRepository.UpdateAsync(answer);

            _logger.LogInformation("File uploaded for answer {AnswerId}: {FileName}", answerId, fileName);

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
            _logger.LogError(ex, "Error uploading file for answer {AnswerId}", answerId);
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

            _logger.LogInformation("File deleted for answer {AnswerId}", answerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file for answer {AnswerId}", answerId);
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
            _logger.LogError(ex, "Error getting file for answer {AnswerId}", answerId);
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
}
