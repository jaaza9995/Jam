using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Jam.Models.Enums;

namespace Jam.ViewModels.StoryCreation;

public class CreateStoryAndIntroViewModel 
{
    [Required(ErrorMessage = "Please enter a title for your story.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The title must be between 2 and 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a description for your story.")]
    [StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a difficulty level.")]
    [Display(Name = "Difficulty Level")]
    public DifficultyLevel? DifficultyLevel { get; set; }

    [Required(ErrorMessage = "Please select the story's accessibility setting.")]
    [Display(Name = "Accessibility")]
    public Accessibility? Accessibility { get; set; }

    [Required(ErrorMessage = "The intro text cannot be empty.")]
    [StringLength(1500, ErrorMessage = "The intro text cannot exceed 1500 characters.")]
    [Display(Name = "Intro Text")]
    public string IntroText { get; set; } = string.Empty;

    // Dropdown data (not user input, so no validation)
    public List<SelectListItem> DifficultyLevelOptions { get; set; } = new();
    public List<SelectListItem> AccessibilityOptions { get; set; } = new();
}