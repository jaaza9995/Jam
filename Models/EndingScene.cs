using Jam.Models.Enums;

namespace Jam.Models;

public class EndingScene
{
    public int EndingSceneId { get; set; }
    public EndingType EndingType { get; set; } // Good || Neutral || Bad
    public string EndingText { get; set; } = string.Empty;
    public int StoryId { get; set; }
    public Story Story { get; set; } = null!; // Navigation property
}