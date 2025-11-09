using Microsoft.EntityFrameworkCore;
using Jam.Models;
using Jam.Models.Enums;

namespace Jam.DAL.SceneDAL;

// Might want to consider avoiding logging entire EF entities
// Consider adding AsNoTracking() to read-only queries for performance boost

// I am including IntroScene as Entities in some of the log messages, that is
// fine, but for QuestionScene I think I will avoid this for now, due to its 
// potentially large size (?), especially when including AnswerOptions

public class SceneRepository : ISceneRepository
{
    private readonly StoryDbContext _db;
    private readonly ILogger<SceneRepository> _logger;

    public SceneRepository(
        StoryDbContext db,
        ILogger<SceneRepository> logger
    )
    {
        _db = db;
        _logger = logger;
    }


    // --------------------------------- INTRO SCENE ---------------------------------

  
    public async Task<IntroScene?> GetIntroSceneByStoryId(int storyId)
    {
        if (storyId <= 0)

        {
            _logger.LogWarning("[SceneRepository -> GetIntroSceneByStoryId] Invalid storyId provided: {storyId}", storyId);
            return null;
        }

        try
        {
            var intro = await _db.IntroScenes
               .AsNoTracking()
               .FirstOrDefaultAsync(s => s.StoryId == storyId);

            if (intro == null)
            {
                _logger.LogInformation("[SceneRepository -> GetIntroSceneByStoryId] No IntroScene found for storyId {storyId}", storyId);
            }

            return intro;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetIntroSceneByStoryId] Error retrieving IntroScene for storyId {storyId}", storyId);
            return null;
        }
    }

    public async Task<IntroScene?> GetIntroSceneById(int introSceneId)
    {
        if (introSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetIntroSceneById] Invalid IntroSceneId provided: {introSceneId}", introSceneId);
            return null;
        }

        try
        {
            var intro = await _db.IntroScenes.FindAsync(introSceneId);
            if (intro == null)
            {
                _logger.LogInformation("[SceneRepository -> GetIntroSceneById] No IntroScene found with id {introSceneId}", introSceneId);
            }

            return intro;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetIntroSceneById] Error retrieving IntroScene with id {introSceneId}", introSceneId);
            return null;
        }
    }

    public async Task<bool> AddIntroScene(IntroScene introScene)
    {
        if (introScene == null)
        {
            _logger.LogWarning("[SceneRepository -> AddIntroScene] Cannot add a null IntroScene");
            return false;
        }

        try
        {
            _db.IntroScenes.Add(introScene);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> AddIntroScene] Error adding IntroScene {introScene}", introScene);
            return false;
        }
    }

    public async Task<bool> UpdateIntroScene(IntroScene introScene)
    {
        if (introScene == null)
        {
            _logger.LogWarning("[SceneRepository -> UpdateIntroScene] Cannot update a null IntroScene");
            return false;
        }

        try
        {
            _db.IntroScenes.Update(introScene);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> UpdateIntroScene] Error updating IntroScene {introScene}", introScene);
            return false;
        }
    }

    public async Task<bool> DeleteIntroScene(int introSceneId)
    {
        if (introSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> DeleteIntroScene] Invalid IntroScene id provided: {introSceneId}", introSceneId);
            return false;
        }

        try
        {
            var intro = await _db.IntroScenes.FindAsync(introSceneId);
            if (intro == null)
            {
                _logger.LogInformation("[SceneRepository -> DeleteIntroScene] No IntroScene found with id {introSceneId}", introSceneId);
                return false;
            }

            _db.IntroScenes.Remove(intro);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> DeleteIntroScene] Error deleting IntroScene with id {introSceneId}", introSceneId);
            return false;
        }
    }




    // --------------------------------- Question SCENE ---------------------------------

    public async Task<IEnumerable<QuestionScene>> GetQuestionScenesByStoryId(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetQuestionScenesByStoryId] Invalid storyId provided: {storyId}", storyId);
            return Enumerable.Empty<QuestionScene>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            // Step 1: Eagerly load ALL QuestionScenes and their AnswerOptions for the story
            var allQuestionScenes = await _db.QuestionScenes
                .Where(q => q.StoryId == storyId)
                .Include(q => q.AnswerOptions) // eager load AnswerOptions (not sure if wanted here)
                .ToListAsync();

            if (!allQuestionScenes.Any())
            {
                _logger.LogWarning("[SceneRepository -> GetQuestionScenesByStoryId] No QuestionScenes found for story with id {storyId}", storyId);
                return Enumerable.Empty<QuestionScene>();
            }

            // Step 2: Find the starting scene using the existing logic 
            var firstScene = await GetFirstQuestionSceneByStoryId(storyId);

            if (firstScene == null)
            {
                _logger.LogWarning("[SceneRepository -> GetQuestionScenesByStoryId] Could not find starting scene for storyId {storyId}. Returning list sorted by QuestionSceneId", storyId);
                return allQuestionScenes.OrderBy(s => s.QuestionSceneId); // fallback to return the list sorted by QuestionSceneId (not ideal, but better than nothing)
            }

            // Step 3: Chain the scenes in memory
            return ChainScenes(allQuestionScenes, firstScene.QuestionSceneId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetQuestionScenesByStoryId] Error retrieving QuestionScenes for storyId {storyId}", storyId);
            return Enumerable.Empty<QuestionScene>(); ; // not returning null to avoid null reference exceptions
        }
    }

    // Sorts the list of scenes into the correct order based on the NextQuestionSceneId chain.
    // <param name="allScenes">All QuestionScene objects belonging to the story.</param>
    // <param name="startingSceneId">The ID of the first scene in the sequence.</param>
    // <returns>An IEnumerable of QuestionScene objects in the correct story order.</returns>
    private IEnumerable<QuestionScene> ChainScenes(List<QuestionScene> allQuestionScenes, int startingSceneId)
    {
        if (allQuestionScenes == null || !allQuestionScenes.Any())
        {
            return Enumerable.Empty<QuestionScene>();
        }

        // Convert the list to a Dictionary for O(1) lookup speed (by ID)
        var questionSceneMap = allQuestionScenes.ToDictionary(s => s.QuestionSceneId);

        var orderedScenes = new List<QuestionScene>();
        int? currentId = startingSceneId;

        // Walk the chain until the current ID is null or we hit a scene not in the map
        while (currentId.HasValue && questionSceneMap.TryGetValue(currentId.Value, out var currentScene))
        {
            orderedScenes.Add(currentScene);
            currentId = currentScene.NextQuestionSceneId;

            // Safety break to prevent infinite loop if the chain is circular
            if (orderedScenes.Count > allQuestionScenes.Count)
            {
                _logger.LogWarning("[SceneRepository -> ChainScenes] Infinite loop detected in scene chain for scene {id}.", startingSceneId);
                break;
            }
        }

        return orderedScenes;
    }

    public async Task<QuestionScene?> GetFirstQuestionSceneByStoryId(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetFirstQuestionSceneByStoryId] Invalid storyId provided: {storyId}", storyId);
            return null;
        }

        try
        {
            // Step 1: Collect all NextQuestionSceneIds for the given story
            var nonStartingIds = await _db.QuestionScenes
                .Where(qs => qs.StoryId == storyId && qs.NextQuestionSceneId.HasValue)
                .Select(qs => qs.NextQuestionSceneId!.Value)
                .Distinct()
                .ToListAsync();

            // Step 2: Find the single QuestionScene that belongs to the story 
            // AND whose ID is NOT in the list of non-starting IDs collected above
            var firstQuestionScene = await _db.QuestionScenes
                .Where(qs => qs.StoryId == storyId && !nonStartingIds.Contains(qs.QuestionSceneId))
                .FirstOrDefaultAsync();

            return firstQuestionScene;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetFirstQuestionSceneByStoryId] Error retrieving first QuestionScene for storyId {storyId}", storyId);
            return null;
        }
    }

    public async Task<QuestionScene?> GetQuestionSceneById(int questionSceneId)
    {
        if (questionSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetQuestionSceneById] Invalid QuestionSceneId provided: {questionSceneId}", questionSceneId);
            return null;
        }

        try
        {
             var questionScene = await _db.QuestionScenes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuestionSceneId == questionSceneId);

            if (questionScene == null)
            {
                _logger.LogWarning("[SceneRepository -> GetQuestionSceneById] No QuestionScene found with id {questionSceneId}", questionSceneId);
            }

            return questionScene;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetQuestionSceneById] Error retrieving QuestionScene with id {questionSceneId}", questionSceneId);
            return null;
        }
    }

    // New method to get QuestionScene with its AnswerOptions eagerly loaded
    public async Task<QuestionScene?> GetQuestionSceneWithAnswerOptionsById(int questionSceneId)
    {
        if (questionSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetQuestionSceneWithAnswerOptionsById] Invalid QuestionSceneId provided: {questionSceneId}", questionSceneId);
            return null;
        }

        try
        {
            var questionScene = await _db.QuestionScenes
                .Include(q => q.AnswerOptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuestionSceneId == questionSceneId);

            if (questionScene == null)
            {
                _logger.LogInformation("[SceneRepository -> GetQuestionSceneWithAnswerOptionsById] No QuestionScene found with id {questionSceneId}", questionSceneId);
            }

            return questionScene;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetQuestionSceneWithAnswerOptionsById] Error retrieving QuestionScene with id {questionSceneId}", questionSceneId);
            return null;
        }
    }


    public async Task<bool> AddQuestionScene(QuestionScene questionScene)
    {
        if (questionScene == null)
        {
            _logger.LogWarning("[SceneRepository -> AddQuestionScene] Cannot add a null QuestionScene");
            return false;
        }

        try
        {
            _db.QuestionScenes.Add(questionScene);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> AddQuestionScene] Error adding QuestionScene for storyId {storyId}", questionScene.StoryId);
            return false;
        }
    }

    public async Task<bool> UpdateQuestionScene(QuestionScene questionScene)
    {
        if (questionScene == null)
        {
            _logger.LogWarning("[SceneRepository -> UpdateQuestionScene] Cannot update a null QuestionScene");
            return false;
        }

        try
        {
            _db.QuestionScenes.Update(questionScene);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> UpdateQuestionScene] Error updating QuestionScene with id {questionSceneId}", questionScene.QuestionSceneId);
            return false;
        }
    }

    public async Task<bool> UpdateQuestionScenes(IEnumerable<QuestionScene> questionScenes)
    {
        if (!questionScenes.Any())
        {
            _logger.LogWarning("[SceneRepository -> UpdateQuestionScenes] No QuestionScenes provided");
            return false;
        }

        try
        {
            var storyId = questionScenes.First().StoryId;

            // Hent eksisterende scener
            var existingScenes = await _db.QuestionScenes
                .Include(q => q.AnswerOptions)
                .Where(q => q.StoryId == storyId)
                .ToListAsync();

            int addedCount = 0;
            int updatedCount = 0;
            int deletedCount = 0;

            // --- Finn og slett scener som er fjernet fra modellen ---
            var modelIds = questionScenes.Where(q => q.QuestionSceneId != 0)
                                         .Select(q => q.QuestionSceneId)
                                         .ToHashSet();

            var toDelete = existingScenes.Where(s => !modelIds.Contains(s.QuestionSceneId)).ToList();
            if (toDelete.Any())
            {
                _db.QuestionScenes.RemoveRange(toDelete);
                deletedCount = toDelete.Count;
            }

            // --- Legg til eller oppdater scener ---
            foreach (var updatedScene in questionScenes)
            {
                if (updatedScene.QuestionSceneId == 0)
                {
                    // 🔹 Ny scene
                    foreach (var answer in updatedScene.AnswerOptions)
                        answer.QuestionScene = updatedScene;

                    _db.QuestionScenes.Add(updatedScene);
                    addedCount++;
                }
                else
                {
                    // 🔹 Eksisterende scene
                    var existingScene = existingScenes.FirstOrDefault(q => q.QuestionSceneId == updatedScene.QuestionSceneId);
                    if (existingScene == null) continue;

                    existingScene.SceneText = updatedScene.SceneText;
                    existingScene.Question = updatedScene.Question;

                    // Fjern gamle svaralternativer
                    _db.AnswerOptions.RemoveRange(existingScene.AnswerOptions);

                    // Knytt nye svar til scenen
                    foreach (var answer in updatedScene.AnswerOptions)
                        answer.QuestionSceneId = existingScene.QuestionSceneId;

                    existingScene.AnswerOptions = updatedScene.AnswerOptions;
                    _db.QuestionScenes.Update(existingScene);
                    updatedCount++;
                }
            }

            await _db.SaveChangesAsync();

            // --- Logg resultatet ---
            _logger.LogInformation(
                "[🔹SceneRepository -> UpdateQuestionScenes] Endringer lagret for storyId={storyId}. " +
                "🔹Added={addedCount}, Updated={updatedCount}, Deleted={deletedCount}",
                storyId, addedCount, updatedCount, deletedCount
            );

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[🔹SceneRepository -> UpdateQuestionScenes] Error updating QuestionScenes");
            return false;
        }
    }




    // This might be considered business logic and should be moved to the service layer or controller
    public async Task<bool> DeleteQuestionScene(int questionSceneId, int? previousSceneId)
    {
        if (questionSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> DeleteQuestionScene] Invalid QuestionSceneId {questionSceneId}", questionSceneId);
            return false;
        }

        try
        {
            var scene = await _db.QuestionScenes.FindAsync(questionSceneId);
            if (scene == null)
            {
                _logger.LogWarning("[SceneRepository -> DeleteQuestionScene] Scene not found with id {questionSceneId}", questionSceneId);
                return false;
            }

            var nextSceneId = scene.NextQuestionSceneId;

            // 🔹 Hvis det finnes en forrige scene, koble den til neste
            if (previousSceneId.HasValue)
            {
                var previous = await _db.QuestionScenes.FindAsync(previousSceneId.Value);
                if (previous != null)
                {
                    previous.NextQuestionSceneId = nextSceneId;
                    _db.QuestionScenes.Update(previous);
                }
            }
            else
            {
                // 🔹 Hvis ingen forrige scene, betyr det at dette var den første scenen.
                // Da lar vi neste scene være "ny start" (du kan senere oppdatere dette i Story om du vil)
                if (nextSceneId.HasValue)
                {
                    var nextScene = await _db.QuestionScenes.FindAsync(nextSceneId.Value);
                    if (nextScene != null)
                    {
                        nextScene.NextQuestionSceneId = nextScene.NextQuestionSceneId;
                        _db.QuestionScenes.Update(nextScene);
                    }
                }
            }

            _db.QuestionScenes.Remove(scene);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[SceneRepository -> DeleteQuestionScene] Deleted scene {Id}, linked previous={Prev} → next={Next}",
                questionSceneId, previousSceneId, nextSceneId);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> DeleteQuestionScene] Error deleting scene {Id}", questionSceneId);
            return false;
        }
    }









    // --------------------------------- Ending SCENE ---------------------------------

    public async Task<IEnumerable<EndingScene>> GetEndingScenesByStoryId(int storyId)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetEndingScenesByStoryId] Invalid storyId provided: {storyId}", storyId);
            return Enumerable.Empty<EndingScene>(); // not returning null to avoid null reference exceptions
        }

        try
        {
            return await _db.EndingScenes
                .Where(e => e.StoryId == storyId)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetEndingScenesByStoryId] Error retrieving EndingScenes for storyId {storyId}", storyId);
            return Enumerable.Empty<EndingScene>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<EndingScene?> GetEndingSceneById(int endingSceneId)
    {
        if (endingSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetEndingSceneById] Invalid EndingSceneId provided: {endingSceneId}", endingSceneId);
            return null;
        }

        try
        {
           var ending = await _db.EndingScenes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EndingSceneId == endingSceneId);

            if (ending == null)
            {
                _logger.LogWarning("[SceneRepository -> GetEndingSceneById] No EndingScene found with id {endingSceneIdid}", endingSceneId);
            }

            return ending;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetEndingSceneById] Error retrieving EndingScene with id {endingSceneId}", endingSceneId);
            return null;
        }
    }

    public async Task<EndingScene?> GetGoodEndingSceneByStoryId(int storyId)
    {
        return await GetEndingSceneByTypeAsync(storyId, EndingType.Good, "Good");
    }

    public async Task<EndingScene?> GetNeutralEndingSceneByStoryId(int storyId)
    {
        return await GetEndingSceneByTypeAsync(storyId, EndingType.Neutral, "Neutral");
    }

    public async Task<EndingScene?> GetBadEndingSceneByStoryId(int storyId)
    {
        return await GetEndingSceneByTypeAsync(storyId, EndingType.Bad, "Bad");
    }

    // Added this private method to reduce redundancy in the three methods above
    private async Task<EndingScene?> GetEndingSceneByTypeAsync(int storyId, EndingType type, string typeName)
    {
        if (storyId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> GetEndingSceneByTypeAsync] Invalid storyId provided: {storyId}", storyId);
            return null;
        }

        try
        {
            var ending = await _db.EndingScenes
               .AsNoTracking()
               .FirstOrDefaultAsync(e => e.StoryId == storyId && e.EndingType == type);

            if (ending == null)
            {
                _logger.LogInformation("[SceneRepository -> GetEndingSceneByTypeAsync] No {typeName} EndingScene found for storyId {storyId}", typeName, storyId);
            }

            return ending;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> GetEndingSceneByTypeAsync] Error retrieving {typeName} EndingScene for storyId {storyId}", typeName, storyId);
            return null;
        }
    }

    public async Task<bool> AddEndingScene(EndingScene endingScene)
    {
        if (endingScene == null)
        {
            _logger.LogWarning("[SceneRepository -> AddEndingScene] Cannot add a null EndingScene");
            return false;
        }

        try
        {
            _db.EndingScenes.Add(endingScene);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> AddEndingScene] Error adding EndingScene {endingScene}", endingScene);
            return false;
        }
    }

    public async Task<bool> UpdateEndingScene(EndingScene endingScene)
    {
        if (endingScene == null)
        {
            _logger.LogWarning("[SceneRepository -> UpdateEndingScene] Cannot update a null EndingScene");
            return false;
        }

        try
        {
            _db.EndingScenes.Update(endingScene);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> UpdateEndingScene] Error updating EndingScene {endingScene}", endingScene);
            return false;
        }
    }

    public async Task<bool> UpdateEndingScenes(IEnumerable<EndingScene> endingScenes)
    {
        var endingScenesList = endingScenes?.ToList() ?? new List<EndingScene>();
        if (!endingScenesList.Any())
        {
            _logger.LogWarning("[SceneRepository -> UpdateEndingScenes] No EndingScenes provided");
            return false;
        }

        try
        {
            var ids = endingScenesList.Select(e => e.EndingSceneId).ToList();
            var existingScenes = await _db.EndingScenes
                .Where(e => ids.Contains(e.EndingSceneId))
                .ToListAsync();

            foreach (var updated in endingScenesList)
            {
                var existing = existingScenes.FirstOrDefault(e => e.EndingSceneId == updated.EndingSceneId);
                if (existing == null)
                {
                    _logger.LogWarning("[SceneRepository -> UpdateEndingScenes] EndingScene {EndingSceneId} not found", updated.EndingSceneId);
                    continue;
                }

                existing.EndingText = updated.EndingText;
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> EditEndingScenes] Error updating one or more EndingScenes");
            return false;
        }
    }

    public async Task<bool> DeleteEndingScene(int endingSceneId)
    {
        if (endingSceneId <= 0)
        {
            _logger.LogWarning("[SceneRepository -> DeleteEndingScene] Invalid EndingSceneId provided: {endingSceneId}", endingSceneId);
            return false;
        }

        try
        {
            var ending = await _db.EndingScenes.FindAsync(endingSceneId);
            if (ending == null)
            {
                _logger.LogWarning("[SceneRepository -> DeleteEndingScene] No EndingScene found with id {endingSceneId}", endingSceneId);
                return false;
            }

            _db.EndingScenes.Remove(ending);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SceneRepository -> DeleteEndingScene] Error deleting EndingScene with id {endingSceneId}", endingSceneId);
            return false;
        }
    }
}

