using Microsoft.EntityFrameworkCore;
using Jam.Models;

namespace Jam.DAL.AnswerOptionDAL;

public class AnswerOptionRepository : IAnswerOptionRepository
{
    private readonly StoryDbContext _db;
    private readonly ILogger<AnswerOptionRepository> _logger;

    public AnswerOptionRepository(
        StoryDbContext db,
        ILogger<AnswerOptionRepository> logger
    )
    {
        _db = db;
        _logger = logger;
    }


    // --------------------------------------- Read / GET ---------------------------------------

    public async Task<IEnumerable<AnswerOption>> GetAllAnswerOptions()
    {
        try
        {
            return await _db.AnswerOptions.ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AnswerOptionRepository -> GetAllAnswerOptions] An error occurred while retrieving all answer options.");
            return Enumerable.Empty<AnswerOption>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<AnswerOption>> GetAnswerOptionsByQuestionSceneId(int questionSceneId)
    {
        if (questionSceneId <= 0)
        {
            _logger.LogWarning("[AnswerOptionRepository -> GetAnswerOptionsByQuestionSceneId] Invalid questionSceneId provided: {questionSceneId}", questionSceneId);
            return Enumerable.Empty<AnswerOption>();
        }

        try
        {
            return await _db.AnswerOptions
                .Where(ao => ao.QuestionSceneId == questionSceneId)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AnswerOptionRepository -> GetAnswerOptionsByQuestionSceneId] An error occurred while retrieving answer options for QuestionSceneId: {questionSceneId}", questionSceneId);
            return Enumerable.Empty<AnswerOption>();
        }
    }

    public async Task<AnswerOption?> GetAnswerOptionById(int answerOptionId)
    {
        if (answerOptionId <= 0)
        {
            _logger.LogWarning("[AnswerOptionRepository -> GetAnswerOptionById] Invalid id provided: {answerOptionId}", answerOptionId);
            return null;
        }

        try
        {
            var answerOption = await _db.AnswerOptions.FindAsync(answerOptionId);
            if (answerOption == null)
            {
                _logger.LogWarning("[AnswerOptionRepository -> GetAnswerOptionById] No answer option found with id {answerOptionId}", answerOptionId);
            }

            return answerOption;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AnswerOptionRepository -> GetAnswerOptionById] An error occurred while retrieving answer option with id: {answerOptionId}", answerOptionId);
            return null;
        }
    }




    // --------------------------------------- Create ---------------------------------------

    public async Task<bool> AddAnswerOption(AnswerOption answerOption)
    {
        if (answerOption == null)
        {
            _logger.LogWarning("[AnswerOptionRepository -> AddAnswerOption] Attempted to add a null answer option.");
            return false;
        }

        try
        {
            _db.AnswerOptions.Add(answerOption);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AnswerOptionRepository -> AddAnswerOption] An error occurred while adding answer option {@answerOption}", answerOption);
            return false;
        }
    }



    // --------------------------------------- Update ---------------------------------------

    public async Task<bool> UpdateAnswerOption(AnswerOption answerOption)
    {
        if (answerOption == null)
        {
            _logger.LogWarning("[AnswerOptionRepository -> UpdateAnswerOption] Attempted to update a null answer option.");
            return false;
        }

        try
        {
            _db.AnswerOptions.Update(answerOption);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AnswerOptionRepository -> UpdateAnswerOption] An error occurred while updating answer option {@answerOption}", answerOption);
            return false;
        }
    }



    // --------------------------------------- Delete ---------------------------------------

    public async Task<bool> DeleteAnswerOption(int answerOptionId)
    {
        if (answerOptionId <= 0)
        {
            _logger.LogWarning("[AnswerOptionRepository -> DeleteAnswerOption] Invalid id provided: {answerOptionId}", answerOptionId);
            return false;
        }

        try
        {
            var answerOption = await _db.AnswerOptions.FindAsync(answerOptionId);
            if (answerOption == null)
            {
                _logger.LogWarning("[AnswerOptionRepository -> DeleteAnswerOption] No answer option found with id {answerOptionId} to delete", answerOptionId);
                return false;
            }

            _db.AnswerOptions.Remove(answerOption);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AnswerOptionRepository -> DeleteAnswerOption] An error occurred while deleting answer option with id: {answerOptionId}", answerOptionId);
            return false;
        }
    }
}