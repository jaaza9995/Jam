using Jam.Models;

namespace Jam.DAL.StoryDAL;

public interface IStoryRepository
{
    // Read / GET
    Task<IEnumerable<Story>> GetAllStories();
    Task<IEnumerable<Story>> GetAllPublicStories();
    Task<IEnumerable<Story>> GetAllPrivateStories(); 
    Task<IEnumerable<Story>> GetStoriesByUserId(string userId);
    Task<IEnumerable<Story>> GetMostRecentPlayedStories(string userId, int count = 5);
    Task<Story?> GetStoryById(int storyId);
    Task<Story?> GetPublicStoryById(int storyId); // not in use
    Task<Story?> GetPrivateStoryByCode(string code); // not in use
    Task<int?> GetAmountOfQuestionsForStory(int storyId); 
    Task<string?> GetCodeForStory(int storyId); // not in use



    // Creation mode
    Task<bool> AddStory(Story story); // not in use
    Task<bool> AddFullStory(Story story); // new method used to add an entire story with all scenes 
    Task<bool> UpdateStory(Story story);
    Task<bool> DeleteStory(int storyId);
    Task<bool> DoesCodeExist(string code);


    // Playing mode
    Task<bool> IncrementPlayed(int storyId);
    Task<bool> IncrementFinished(int storyId);
    Task<bool> IncrementFailed(int storyId);
} 