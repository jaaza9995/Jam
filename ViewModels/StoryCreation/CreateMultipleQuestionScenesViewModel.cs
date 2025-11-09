using System.ComponentModel.DataAnnotations;

namespace Jam.ViewModels.StoryCreation;

public class CreateMultipleQuestionScenesViewModel
{
    // public int StoryId { get; set; } no longer need this
    // public int? PreviousSceneId { get; set; } no longer need this

    [MinLength(1, ErrorMessage = "Please add at least one question scene.")]
    public List<QuestionSceneBaseViewModel> QuestionScenes { get; set; } = new();
}