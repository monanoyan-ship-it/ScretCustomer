using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class AnswerRepository : IAnswerRepository
{
    private readonly ApplicationDbContext _context;

    public AnswerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Answer?> GetByIdAsync(int id)
    {
        return await _context.Answers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
    }

    public async Task<IEnumerable<Answer>> GetByEvaluationIdAsync(int evaluationId)
    {
        return await _context.Answers
            .Include(a => a.Question)
            .Where(a => a.EvaluationId == evaluationId && !a.IsDeleted)
            .OrderBy(a => a.Question.Order)
            .ToListAsync();
    }

    public async Task<Answer> CreateAsync(Answer answer)
    {
        _context.Answers.Add(answer);
        await _context.SaveChangesAsync();
        return answer;
    }

    public async Task<Answer> UpdateAsync(Answer answer)
    {
        answer.UpdatedAt = TurkeyTime.Now;
        _context.Answers.Update(answer);
        await _context.SaveChangesAsync();
        return answer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var answer = await _context.Answers.FindAsync(id);
        if (answer == null) return false;

        answer.IsDeleted = true;
        answer.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)
    {
        return await _context.Answers
            .Include(a => a.Evaluation)
            .Where(a => a.QuestionId == questionId && !a.IsDeleted)
            .ToListAsync();
    }
}
