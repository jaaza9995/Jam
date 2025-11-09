using Jam.Models;
using Jam.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Jam.ViewModels.StoryPlaying;

public class PlaySceneViewModel : IValidatableObject
{
    public int SessionId { get; set; }
    public int SceneId { get; set; }
    public string SceneText { get; set; } = string.Empty;
    public SceneType SceneType { get; set; }


    // For QuestionScenes
    public string? Question { get; set; }
    public IEnumerable<AnswerOption>? AnswerOptions { get; set; }
    public int? SelectedAnswerId { get; set; } // user’s selected answer in the form 
    public Guid AnswerRandomSeed { get; set; } // in case client-side validation fails 


    // Playing stats
    public int CurrentScore { get; set; }
    public int MaxScore { get; set; }
    public int CurrentLevel { get; set; }


    // Conditional validation
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SceneType == SceneType.Question && SelectedAnswerId == null)
        {
            yield return new ValidationResult(
                "You must select an answer before continuing.",
                new[] { nameof(SelectedAnswerId) }
            );
        }
    }
}