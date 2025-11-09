using System.ComponentModel.DataAnnotations;

namespace Jam.ViewModels; // Used by other ViewModels in different sub-directories 

// Used by CreateQuestionSceneViewModel and EditQuestionScenesViewModel
public class QuestionSceneBaseViewModel
{
    public int StoryId { get; set; }

    [Required(ErrorMessage = "Please provide the story text for this scene.")]
    [StringLength(1000, ErrorMessage = "The story text cannot exceed 1000 characters.")]
    [Display(Name = "Story Text")]
    public string StoryText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the question.")]
    [StringLength(300, ErrorMessage = "The question cannot exceed 300 characters.")]
    [Display(Name = "Question")]
    public string QuestionText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please add 4 answer options.")]
    public List<AnswerOptionInput> Answers { get; set; } = new()
    {
        new(), new(), new(), new()
    };

    // Represents the index (0–3) of the correct answer option
    [Range(0, 3, ErrorMessage = "Please select a valid correct answer.")]
    public int CorrectAnswerIndex { get; set; } = -1;

    // Used to toggle between Create/Edit mode in the shared partial view
    public bool IsEditing { get; set; }

     public int QuestionSceneId { get; set; }
     public bool MarkedForDeletion { get; set; } = false;

}

public class AnswerOptionInput
{
    [Required(ErrorMessage = "Each answer option must have text.")]
    [StringLength(200, ErrorMessage = "The answer text cannot exceed 200 characters.")]
    [Display(Name = "Answer Option")]
    public string AnswerText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the feedback text for this answer.")]
    [StringLength(500, ErrorMessage = "The feedback text cannot exceed 500 characters.")]
    [Display(Name = "Follow-up Text")]
    public string ContextText { get; set; } = string.Empty;
}

