using Jam.Models;

namespace Jam.ViewModels.StoryPlaying;

public class StorySelectionViewModel
{
    public IEnumerable<Story> PublicStories { get; set; } = new List<Story>();
    public IEnumerable<Story> PrivateStories { get; set; } = new List<Story>();
}