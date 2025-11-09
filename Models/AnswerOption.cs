
namespace Jam.Models;

public class AnswerOption
{
    public int AnswerOptionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string FeedbackText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int QuestionSceneId { get; set; }
    public QuestionScene QuestionScene { get; set; } = null!; // Navigation property
}