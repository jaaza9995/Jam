using System.ComponentModel.DataAnnotations;

//namespace Jam.ViewModels.Shared;
namespace Jam.ViewModels;

public class EndingScenesViewModel
{
    public int? StoryId { get; set; } // nullable, only used in editing mode

    [Required(ErrorMessage = "Please write the good ending for your story.")]
    [StringLength(1500, ErrorMessage = "The good ending text cannot exceed 1500 characters.")]
    [Display(Name = "Good Ending")]
    public string GoodEnding { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write the neutral ending for your story.")]
    [StringLength(1500, ErrorMessage = "The neutral ending text cannot exceed 1500 characters.")]
    [Display(Name = "Neutral Ending")]
    public string NeutralEnding { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write the bad ending for your story.")]
    [StringLength(1500, ErrorMessage = "The bad ending text cannot exceed 1500 characters.")]
    [Display(Name = "Bad Ending")]
    public string BadEnding { get; set; } = string.Empty;

    // Flag to indicate whether user is in editing or creating mode 
    public bool IsEditMode { get; set; }
}