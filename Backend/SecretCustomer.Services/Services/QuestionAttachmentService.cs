using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Question;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class QuestionAttachmentService : IQuestionAttachmentService
{
    private readonly ApplicationDbContext _context;

    public QuestionAttachmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuestionAttachmentDto>> GetByQuestionAsync(int questionId)
    {
        var attachments = await _context.QuestionAttachments
            .Where(a => a.QuestionId == questionId)
            .OrderBy(a => a.Order)
            .Select(a => new QuestionAttachmentDto
            {
                Id = a.Id,
                QuestionId = a.QuestionId,
                FileName = a.FileName,
                FilePath = a.FilePath,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                Description = a.Description,
                Order = a.Order,
                CreatedAt = a.CreatedAt,
                UploadedByUserName = a.UploadedByUser != null
                    ? a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName
                    : null
            })
            .ToListAsync();

        return attachments;
    }

    public async Task<bool> QuestionExistsAsync(int questionId)
    {
        var question = await _context.Questions.FindAsync(questionId);
        return question != null;
    }

    public async Task<QuestionAttachmentDto> UploadAsync(int questionId, string fileName, string storedFileName, string filePath, long fileSize, string contentType, string? description, int? uploadedByUserId)
    {
        // Sıra numarası
        var maxOrder = await _context.QuestionAttachments
            .Where(a => a.QuestionId == questionId)
            .MaxAsync(a => (int?)a.Order) ?? 0;

        // Veritabanına kaydet
        var attachment = new QuestionAttachment
        {
            QuestionId = questionId,
            FileName = fileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            FileSize = fileSize,
            ContentType = contentType,
            Description = description,
            Order = maxOrder + 1,
            UploadedByUserId = uploadedByUserId
        };

        _context.QuestionAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return new QuestionAttachmentDto
        {
            Id = attachment.Id,
            QuestionId = attachment.QuestionId,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            Description = attachment.Description,
            Order = attachment.Order,
            CreatedAt = attachment.CreatedAt
        };
    }

    public async Task<(string FilePath, string ContentType, string FileName)?> GetDownloadInfoAsync(int id)
    {
        var attachment = await _context.QuestionAttachments.FindAsync(id);
        if (attachment == null)
        {
            return null;
        }

        return (attachment.FilePath, attachment.ContentType, attachment.FileName);
    }

    public async Task<(bool Success, string? FilePath)> DeleteAsync(int id)
    {
        var attachment = await _context.QuestionAttachments.FindAsync(id);
        if (attachment == null)
        {
            return (false, null);
        }

        var filePath = attachment.FilePath;

        // Veritabanından sil (soft delete)
        attachment.IsDeleted = true;
        attachment.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, filePath);
    }

    public async Task<bool> UpdateOrderAsync(int id, int newOrder)
    {
        var attachment = await _context.QuestionAttachments.FindAsync(id);
        if (attachment == null)
        {
            return false;
        }

        attachment.Order = newOrder;
        attachment.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }
}
