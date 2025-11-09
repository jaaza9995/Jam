using Microsoft.EntityFrameworkCore;
using Jam.Models;
using Jam.Models.Enums;

namespace Jam.DAL.StoryDAL;

// Consider adding AsNoTracking where appropriate for read-only queries
// to improve performance by disabling change tracking 

public class StoryRepository : IStoryRepository
{
    private readonly StoryDbContext _db;
    private readonly ILogger<StoryRepository> _logger;

    public StoryRepository(
        StoryDbContext db,
        ILogger<StoryRepository> logger
    )
    {
        _db = db;
        _logger = logger;
    }


    // --------------------------------------- Read / GET ---------------------------------------

    public async Task<IEnumerable<Story>> GetAllStories()
    {
        try
        {
            return await _db.Stories
                .Include(s => s.User) // eager load owner
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetAllStories] Failed to retrieve all stories");
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<Story>> GetAllPublicStories()
    {
        try
        {
            return await _db.Stories
                .Where(s => s.Accessible == Accessibility.Public)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetAllPublicStories] Failed to retrieve public stories");
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<Story>> GetAllPrivateStories()
    {
        try
        {
            return await _db.Stories
                .Where(s => s.Accessible == Accessibility.Private)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetAllPrivateStories] Failed to retrieve private stories");
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<Story>> GetStoriesByUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[StoryRepository -> GetStoriesByUserId] Invalid user id {userId} provided", userId);
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            return await _db.Stories
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetStoriesByUserId] Failed to retrieve stories for user id {userId}", userId);
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<Story>> GetMostRecentPlayedStories(string userId, int count)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[StoryRepository -> GetMostRecentPlayedStories] Invalid user id {userId} provided", userId);
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            var lastSessions = await _db.PlayingSessions
                .Where(ps => ps.UserId == userId)
                .OrderByDescending(ps => ps.EndTime ?? ps.StartTime) // use EndTime if available
                .Include(ps => ps.Story) // load related story in the same query
                .ToListAsync();

            return lastSessions
                .Select(ps => ps.Story!)
                .Distinct() // in case the same story appears multiple times
                .Take(count)
                .ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetMostRecentPlayedStories] Failed to retrieve most recent played stories for user id {userId}", userId);
            return Enumerable.Empty<Story>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<Story?> GetStoryById(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> GetStoryById] Invalid story id {storyId} provided", storyId);
            return null;
        }

        try
        {
            var story = await _db.Stories.FindAsync(storyId);
            if (story == null)
            {
                _logger.LogInformation("[StoryRepository -> GetStoryById] No story found with id {storyId}", storyId);
            }

            return story;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetStoryById] Failed to get story with id {storyId}", storyId);
            return null;
        }
    }

    public async Task<Story?> GetPublicStoryById(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> GetPublicStoryById] Invalid story id {storyId} provided", storyId);
            return null;
        }

        try
        {
            var story = await _db.Stories
                .FirstOrDefaultAsync(s => s.StoryId == storyId && s.Accessible == Accessibility.Public);

            if (story == null)
            {
                _logger.LogInformation("[StoryRepository -> GetPublicStoryById] No public story found with id {storyId}", storyId);
            }

            return story;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetPublicStoryById] Failed to get public story with id {storyId}", storyId);
            return null;
        }
    }

    public async Task<Story?> GetPrivateStoryByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("[StoryRepository -> GetPrivateStoryByCode] Invalid or empty code provided");
            return null;
        }

        code = code.ToUpperInvariant().Trim(); // normalize code format, just for safety

        try
        {
            var story = await _db.Stories
                .SingleOrDefaultAsync(s => s.Code == code && s.Accessible == Accessibility.Private);

            if (story == null)
            {
                _logger.LogInformation("[StoryRepository -> GetPrivateStoryByCode] No private story found with code {code}", code);
            }

            return story;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetPrivateStoryByCode] Failed to get private story with code {code}", code);
            return null;
        }
    }

    public async Task<int?> GetAmountOfQuestionsForStory(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> GetAmountOfQuestionsForStory] Invalid story id {storyId} provided", storyId);
            return 0;
        }

        try
        {
            var questionCount = await _db.QuestionScenes
                .Where(s => s.StoryId == storyId)
                .CountAsync();

            return questionCount;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetAmountOfQuestionsForStory] Failed to get question count for story id {storyId}", storyId);
            return null;
        }
    }

    public async Task<string?> GetCodeForStory(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> GetCodeForStory] Invalid story id {storyId} provided", storyId);
            return null;
        }

        try
        {
            var code = await _db.Stories
                .Where(s => s.StoryId == storyId && s.Accessible == Accessibility.Private)
                .Select(s => s.Code)
                .SingleOrDefaultAsync();

            if (code == null)
            {
                _logger.LogInformation("[StoryRepository -> GetCodeForStory] No private story code found for story id {storyId}", storyId);
            }

            return code;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> GetCodeForStory] Failed to get code for story id {storyId}", storyId);
            return null;
        }
    }




    // --------------------------------------- Creation Mode ---------------------------------------

    public async Task<bool> AddStory(Story story)
    {
        if (story == null)
        {
            _logger.LogWarning("[StoryRepository -> AddStory] Null story object provided");
            return false;
        }

        try
        {
            _db.Stories.Add(story);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> AddStory] Failed to add new story {story}", story);
            return false;
        }
    }

    public async Task<bool> AddFullStory(Story story)
    {
        if (story == null)
        {
            _logger.LogWarning("[StoryRepository -> AddStory] Null story object provided");
            return false;
        }

        try
        {
            // Step 1: Add the root entity (Story)
            //     EF Core will automatically detect and manage the related entities 
            //     (IntroScene, QuestionScenes, EndingScenes, and AnswerOptions) 
            //     because they are attached to the 'story' object
            _db.Stories.Add(story);

            // 2. Save all changes in one atomic (transactional) operation
            //     If any part of the save fails, the entire operation rolls back implicitly
            int changes = await _db.SaveChangesAsync();

            // 3. Returns true if everything went fine
            return changes > 0;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "[StoryRepository -> AddFullStory] Database update failed for story '{Title}'.", story.Title);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StoryRepository -> AddFullStory] Unexpected error while creating story '{Title}'.", story.Title);
            return false;
        }
    }

    public async Task<bool> UpdateStory(Story story)
    {
        if (story == null)
        {
            _logger.LogWarning("[StoryRepository -> UpdateStory] Null story object provided");
            return false;
        }

        try
        {
            _db.Stories.Update(story);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> UpdateStory] Failed to update story with id {storyId}", story.StoryId);
            return false;
        }
    }

    public async Task<bool> DeleteStory(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> DeleteStory] Invalid story id {storyId} provided", storyId);
            return false;
        }

        try
        {
            var story = await _db.Stories.FindAsync(storyId);
            if (story == null)
            {
                _logger.LogWarning("[StoryRepository -> DeleteStory] No story found with id {storyId}", storyId);
                return false;
            }

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> DeleteStory] Failed to delete story with id {storyId}", storyId);
            return false;
        }
    }

    public async Task<bool> DoesCodeExist(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("[StoryRepository -> DoesCodeExist] Invalid or empty code provided");
            return false;
        }

        code = code.ToUpperInvariant().Trim(); // normalize code format, just for safety

        try
        {
            var exists = await _db.Stories.AnyAsync(s => s.Code == code);
            return exists;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> DoesCodeExist] Failed to check existence of story code {code}", code);
            return false;
        }
    }




    // --------------------------------------- Playing Mode ---------------------------------------

    public async Task<bool> IncrementPlayed(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> IncrementPlayed] Invalid story id {storyId} provided", storyId);
            return false;
        }

        try
        {
            await _db.Stories
                .Where(s => s.StoryId == storyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(st => st.Played, st => st.Played + 1)
                    .SetProperty(st => st.Dnf, st => st.Dnf + 1));

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> IncrementPlayed] Failed to increment played count for story id {storyId}", storyId);
            return false;
        }
    }

    public async Task<bool> IncrementFinished(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> IncrementFinished] Invalid story id {storyId} provided", storyId);
            return false;
        }
        try
        {
            await _db.Stories
                .Where(s => s.StoryId == storyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(st => st.Finished, st => st.Finished + 1)
                    .SetProperty(st => st.Dnf, st => st.Dnf > 0 ? st.Dnf - 1 : 0));

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> IncrementFinished] Failed to increment finished count for story id {storyId}", storyId);
            return false;
        }
    }

    public async Task<bool> IncrementFailed(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryRepository -> IncrementFailed] Invalid story id {storyId} provided", storyId);
            return false;
        }

        try
        {
            await _db.Stories
                .Where(s => s.StoryId == storyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(st => st.Failed, st => st.Failed + 1)
                    .SetProperty(st => st.Dnf, st => st.Dnf > 0 ? st.Dnf - 1 : 0));

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryRepository -> IncrementFailed] Failed to increment failed count for story id {storyId}", storyId);
            return false;
        }
    }
}

