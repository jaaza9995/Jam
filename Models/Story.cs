using Jam.Models.Enums;


namespace Jam.Models;

public class Story
{
    public int StoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DifficultyLevel DifficultyLevel { get; set; } // Easy || Medium || Hard
    public Accessibility Accessible { get; set; } // Public || Private
    public string? Code { get; set; } // if Accessible == Private -> Code is required
    public int Played { get; set; } // how many times the story has been played
    public int Finished { get; set; } // how many times the story has been finished
    public int Failed { get; set; } // how many times the story has been failed
    public int Dnf { get; set; } // how many times the story has been abandoned
    public string? UserId { get; set; } // Foreign key to User, nullable to allow User deletion without deleting stories
    public ApplicationUser? User { get; set; } // Navigation property 
    public IntroScene IntroScene { get; set; } = new(); // Navigation property
    public List<QuestionScene> QuestionScenes { get; set; } = new(); // Navigation property
    public List<EndingScene> EndingScenes { get; set; } = new(); // Navigation property
    public List<PlayingSession> PlayingSessions { get; set; } = new(); // Navigation property
}
