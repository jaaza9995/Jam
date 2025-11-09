using Microsoft.EntityFrameworkCore;
using Jam.Models.Enums;
using Jam.Models;

namespace Jam.DAL.PlayingSessionDAL;

// Consider adding AsNoTracking() to read-only queries for performance boost

public class PlayingSessionRepository : IPlayingSessionRepository
{
    private readonly StoryDbContext _db;
    private readonly ILogger<PlayingSessionRepository> _logger;

    public PlayingSessionRepository(
        StoryDbContext db,
        ILogger<PlayingSessionRepository> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    // --------------------------------------- Read / GET ---------------------------------------

    public async Task<IEnumerable<PlayingSession>> GetAllPlayingSessions()
    {
        try
        {
            return await _db.PlayingSessions
                .Include(s => s.User) // eager load player
                .Include(s => s.Story) // eager load story
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> GetAllPlayingSessions] Error retrieving all playing sessions");
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<PlayingSession?> GetPlayingSessionById(int playingSessionId)
    {
        if (playingSessionId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetPlayingSessionById] Invalid id: {playingSessionId}", playingSessionId);
            return null;
        }

        try
        {
            var session = await _db.PlayingSessions.FindAsync(playingSessionId);
            if (session == null)
            {
                _logger.LogWarning("[PlayingSessionRepository -> GetPlayingSessionById] No playing session found with id: {playingSessionId}", playingSessionId);
            }

            return session;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> GetPlayingSessionById] Error retrieving playing session with id: {playingSessionId}", playingSessionId);
            return null;
        }
    }

    public async Task<IEnumerable<PlayingSession>> GetPlayingSessionsByUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetPlayingSessionsByUserId] Invalid userId: {userId}", userId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            return await _db.PlayingSessions
                .Where(ps => ps.UserId == userId)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> GetPlayingSessionsByUserId] Error retrieving playing sessions for userId: {userId}", userId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<PlayingSession>> GetPlayingSessionsByStoryId(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetPlayingSessionsByStoryId] Invalid storyId: {storyId}", storyId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            return await _db.PlayingSessions
                .Where(ps => ps.StoryId == storyId)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> GetPlayingSessionsByStoryId] Error retrieving playing sessions for storyId: {storyId}", storyId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<IEnumerable<PlayingSession>> GetPlayingSessionsByUserIdAndStoryId(string userId, int storyId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetPlayingSessionsByUserIdAndStoryId] Invalid userId: {userId}}", userId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }

        if (storyId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetPlayingSessionsByUserIdAndStoryId] Invalid storyId: {storyId}", storyId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            return await _db.PlayingSessions
                .Where(ps => ps.UserId == userId && ps.StoryId == storyId)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> GetPlayingSessionsByUserIdAndStoryId] Error retrieving playing sessions for userId: {userId} and storyId: {storyId}", userId, storyId);
            return Enumerable.Empty<PlayingSession>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<int?> GetUserHighScoreForStory(string userId, int storyId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetUserHighScoreForStory] Invalid userId: {userId}", userId);
            return null;
        }

        if (storyId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> GetUserHighScoreForStory] Invalid storyId: {storyId}", storyId);
            return null;
        }

        try
        {
            // returns null if no record exists (if 0 then that is the actual high score)
            var bestScore = await _db.PlayingSessions
                .Where(ps => ps.UserId == userId && ps.StoryId == storyId)
                .Select(ps => (int?)ps.Score)
                .OrderByDescending(score => score)
                .FirstOrDefaultAsync();

            return bestScore;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> GetUserHighScoreForStory] Error retrieving high score for userId: {userId} and storyId: {storyId}", userId, storyId);
            return null;
        }
    }




    // --------------------------------------- Create ---------------------------------------

    public async Task<bool> AddPlayingSession(PlayingSession playingSession)
    {
        if (playingSession == null)
        {
            _logger.LogWarning("[PlayingSessionRepository -> AddPlayingSession] Attempted to add a null playing session");
            return false;
        }

        try
        {
            _db.PlayingSessions.Add(playingSession);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> AddPlayingSession] Error adding new playing session {playingSession}", playingSession);
            return false;
        }
    }




    // --------------------------------------- Update ---------------------------------------

    // When user goes from IntroScene to first QuestionScene
    public async Task<bool> MoveToNextScene(int playingSessionId, int nextSceneId, SceneType newSceneType)
    {
        if (playingSessionId <= 0 || nextSceneId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> MoveToNextScene] Invalid playingSessionId: {playingSessionId} or nextSceneId: {nextSceneId}", playingSessionId, nextSceneId);
            return false;
        }

        if (newSceneType != SceneType.Question)
        {
            _logger.LogWarning("[PlayingSessionRepository -> MoveToNextScene] Invalid sceneType: {sceneType} for nextSceneId: {nextSceneId}", newSceneType, nextSceneId);
            return false;
        }

        return await UpdateSessionProgressAsync(playingSessionId, nextSceneId, newSceneType); // private method (see below)
    }

    // When user goes from a QuestionScene to another QuestionScene, or
    // when user goes from the last QuestionScene to an EndingScene
    public async Task<bool> AnswerQuestion(int playingSessionId, int nextSceneId, SceneType newSceneType, int newScore, int newLevel)
    {
        if (playingSessionId <= 0 || nextSceneId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> AnswerQuestion] Invalid playingSessionId: {playingSessionId} or nextSceneId: {nextSceneId}", playingSessionId, nextSceneId);
            return false;
        }

        if (newScore < 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> AnswerQuestion] Invalid newScore: {newScore} for playingSessionId: {playingSessionId}", newScore, playingSessionId);
            return false;
        }

        if (newLevel < 1 || newLevel > 3)
        {
            _logger.LogWarning("[PlayingSessionRepository -> AnswerQuestion] Invalid newLevel: {newLevel} for playingSessionId: {playingSessionId}", newLevel, playingSessionId);
            return false;
        }

        return await UpdateSessionProgressAsync(playingSessionId, nextSceneId, newSceneType, newScore, newLevel); // private method (see below)

    }

    // When user finishes the EndingScene (when user is done with the game)
    public async Task<bool> FinishSession(int playingSessionId, int finalScore, int finalLevel)
    {
        if (playingSessionId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> FinishSession] Invalid playingSessionId: {playingSessionId}", playingSessionId);
            return false;
        }

        if (finalScore < 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> FinishSession] Invalid finalScore: {finalScore} for playingSessionId: {playingSessionId}", finalScore, playingSessionId);
            return false;
        }

        if (finalLevel < 1 || finalLevel > 3)
        {
            _logger.LogWarning("[PlayingSessionRepository -> FinishSession] Invalid newLevel: {newLevel} for playingSessionId: {playingSessionId}", finalLevel, playingSessionId);
            return false;
        }

        return await UpdateSessionProgressAsync(playingSessionId, null, null, finalScore, finalLevel); // private method (see below)
    }


    // Private helper method to update session progress based on different scenarios
    private async Task<bool> UpdateSessionProgressAsync(
        int playingSessionId,
        int? nextSceneId,
        SceneType? newSceneType,
        int? newScore = null,
        int? newCurrentLevel = null)
    {
        try
        {
            var session = await _db.PlayingSessions.FindAsync(playingSessionId);
            if (session == null)
            {
                _logger.LogWarning("[PlayingSessionRepository -> UpdateSessionProgressAsync] No playing session found with playingSessionId: {playingSessionId}", playingSessionId);
                return false;
            }

            if (nextSceneId.HasValue)
                session.CurrentSceneId = nextSceneId;

            if (newSceneType != null)
                session.CurrentSceneType = newSceneType;

            if (newScore.HasValue)
                session.Score = newScore.Value;

            if (newCurrentLevel.HasValue)
                session.CurrentLevel = newCurrentLevel.Value;

            // If nextSceneId is null, the game has ended
            if (nextSceneId == null)
                session.EndTime = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> UpdateSessionProgressAsync] Error updating session progress for playingSessionId: {playingSessionId}", playingSessionId);
            return false;
        }
    }




    // --------------------------------------- Delete ---------------------------------------

    public async Task<bool> DeletePlayingSession(int playingSessionId)
    {
        if (playingSessionId <= 0)
        {
            _logger.LogWarning("[PlayingSessionRepository -> DeletePlayingSession] Invalid id: {playingSessionId}", playingSessionId);
            return false;
        }

        try
        {
            var playingSession = await _db.PlayingSessions.FindAsync(playingSessionId);
            if (playingSession == null)
            {
                _logger.LogWarning("[PlayingSessionRepository -> DeletePlayingSession] No playing session found with id: {playingSessionId}", playingSessionId);
                return false;
            }

            _db.PlayingSessions.Remove(playingSession);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PlayingSessionRepository -> DeletePlayingSession] Error deleting playing session with id: {playingSessionId}", playingSessionId);
            return false;
        }
    }
}