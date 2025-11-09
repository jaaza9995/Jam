using Jam.Models;
using Jam.Models.Enums;

namespace Jam.DAL.PlayingSessionDAL;

public interface IPlayingSessionRepository
{
    // Read / GET
    Task<IEnumerable<PlayingSession>> GetAllPlayingSessions();
    Task<PlayingSession?> GetPlayingSessionById(int playingSessionId);
    Task<IEnumerable<PlayingSession>> GetPlayingSessionsByUserId(string userId);
    Task<IEnumerable<PlayingSession>> GetPlayingSessionsByStoryId(int storyId);
    Task<IEnumerable<PlayingSession>> GetPlayingSessionsByUserIdAndStoryId(string userId, int storyId);
    Task<int?> GetUserHighScoreForStory(string userId, int storyId);



    // Create
    Task<bool> AddPlayingSession(PlayingSession playingSession);



    // Update
    Task<bool> MoveToNextScene(int playingSessionId, int nextSceneId, SceneType newSceneType);
    Task<bool> AnswerQuestion(int playingSessionId, int nextSceneId, SceneType newSceneType, int newScore, int newLevel); // added newSceneType
    Task<bool> FinishSession(int playingSessionId, int finalScore, int finalLevel);



    // Delete
    Task<bool> DeletePlayingSession(int playingSessionId);
} 