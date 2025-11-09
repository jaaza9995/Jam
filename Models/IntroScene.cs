
namespace Jam.Models;

public class IntroScene
{
    public int IntroSceneId { get; set; }
    public string IntroText { get; set; } = string.Empty;
    public int StoryId { get; set; }
    public Story Story { get; set; } = null!; // Navigation property
}