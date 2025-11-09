
namespace Jam.Models;

public class QuestionScene
{
    public int QuestionSceneId { get; set; }
    public string SceneText { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public int? NextQuestionSceneId { get; set; }
    public QuestionScene? NextQuestionScene { get; set; } // Navigation property
    public int StoryId { get; set; }
    public Story Story { get; set; } = null!; // Navigation property
    public List<AnswerOption> AnswerOptions { get; set; } = new(); // Navigation property
}