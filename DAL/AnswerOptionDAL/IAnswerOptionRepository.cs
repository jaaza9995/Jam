using Jam.Models;

namespace Jam.DAL.AnswerOptionDAL;

public interface IAnswerOptionRepository
{
    // Read / GET
    Task<IEnumerable<AnswerOption>> GetAllAnswerOptions();
    Task<IEnumerable<AnswerOption>> GetAnswerOptionsByQuestionSceneId(int questionSceneId);
    Task<AnswerOption?> GetAnswerOptionById(int answerOptionId);


    // Create
    Task<bool> AddAnswerOption(AnswerOption answerOption);


    // Update
    Task<bool> UpdateAnswerOption(AnswerOption answerOption);

    // Delete
    Task<bool> DeleteAnswerOption(int answerOptionId);
}