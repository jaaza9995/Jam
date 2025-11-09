using System.ComponentModel.DataAnnotations;
using Jam.Models.Enums;

namespace Jam.ViewModels.StoryEditing;

public class EditStoryViewModel
{
    public int StoryId { get; set; }

    [Required(ErrorMessage = "Please enter a story title.")]
    [StringLength(100, ErrorMessage = "The title cannot exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a description for your story.")]
    [StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a difficulty level.")]
    [Display(Name = "Difficulty Level")]
    public DifficultyLevel DifficultyLevel { get; set; }

    [Required(ErrorMessage = "Please select an accessibility option.")]
    [Display(Name = "Accessibility")]
    public Accessibility Accessibility { get; set; }
}