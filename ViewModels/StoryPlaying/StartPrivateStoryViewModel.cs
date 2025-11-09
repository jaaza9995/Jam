using Jam.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Jam.ViewModels.StoryPlaying;

public class StartPrivateStoryViewModel
{
    public int StoryId { get; set; }

    [Display(Name = "Story Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Accessibility")]
    public Accessibility Accessibility { get; set; }

    [Required(ErrorMessage = "Please enter the access code to start this story.")]
    [StringLength(10, ErrorMessage = "The access code cannot exceed 10 characters.")]
    [Display(Name = "Access Code")]
    public string? Code { get; set; } // user input if story is private

    public string? ErrorMessage { get; set; } // if user types in the wrong code
}