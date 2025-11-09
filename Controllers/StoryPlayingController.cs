using Microsoft.AspNetCore.Mvc;
using Jam.Models.Enums;
using Jam.DAL.StoryDAL;
using Jam.ViewModels;
using Jam.ViewModels.StoryPlaying;
using Jam.DAL.PlayingSessionDAL;
using Jam.DAL.SceneDAL;
using Jam.Models;
using Jam.DAL.AnswerOptionDAL;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Jam.Controllers;

/*
    StoryPlayingController
        Handles all logic for users (kids/players) who are playing stories.
        This controller deals with a number of scenarios related to story-playing, like:
            * Starting a new PlayingSession between User and Story
            * Loading the correct Scene for the Story
            * Loading a Question Scene with its Question + AnswerOptions
            * Showing the right amount of AnswerOptions based on user's level
            * Handling user choices (answers, score, moving to next scene, etc.)
            * Ending the story by showing the right Ending Scene
            * Completing a story playing session by showing a final summary view

        Basically, everything that belongs to the “learning experience” side of the app.
        This controller will communicate mainly with these repositories:
            * PlayingSessionRepository
            * StoryRepository 
            * SceneRepository
            * AnswerOptionRepository    
*/

public class StoryPlayingController : Controller
{
    private readonly IPlayingSessionRepository _playingSessionRepository;
    private readonly IStoryRepository _storyRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IAnswerOptionRepository _answerOptionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly ILogger<StoryPlayingController> _logger;

    public StoryPlayingController(
        IPlayingSessionRepository playingSessionRepository,
        IStoryRepository storyRepository,
        ISceneRepository sceneRepository,
        IAnswerOptionRepository answerOptionRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<StoryPlayingController> logger
    )
    {
        _playingSessionRepository = playingSessionRepository;
        _storyRepository = storyRepository;
        _sceneRepository = sceneRepository;
        _answerOptionRepository = answerOptionRepository;
        _userManager = userManager;
        _logger = logger;
    }

    /*
        This could be a view which shows all stories in the application/db to the user.
        This is the view the user is redirected to when clicking: "Play New Story" in home.
        Here, the user can browser all of the public stories to find one they want to play. 
        They can also brows private stories, but these stories requires a code to be played.
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SelectStory(string? search)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);

            var publicStories = await _storyRepository.GetAllPublicStories();
            var privateStories = await _storyRepository.GetAllPrivateStories();

            if (!string.IsNullOrWhiteSpace(search))
            {
                publicStories = publicStories
                    .Where(s => s.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var viewModel = new StorySelectionViewModel
            {
                PublicStories = publicStories,
                PrivateStories = privateStories
            };

            ViewData["Search"] = search;

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> SelectStory:GET] Error while fetching stories.");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while loading stories. Please try again later."
            });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> JoinPrivateStory(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "Please enter a code.";
            return RedirectToAction("SelectStory");
        }

        try
        {
            // Bruk eksisterende metode fra IStoryRepository
            var story = await _storyRepository.GetPrivateStoryByCode(code.Trim().ToUpper());

            if (story == null)
            {
                TempData["ErrorMessage"] = "No story found with that code.";
                return RedirectToAction("SelectStory");
            }

            // Hvis historien er funnet → send brukeren til StartStory
            return RedirectToAction("StartStory", new { storyId = story.StoryId });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> JoinPrivateStory:POST] Failed to join private story with code {code}", code);
            TempData["ErrorMessage"] = "Something went wrong while searching for the story.";
            return RedirectToAction("SelectStory");
        }
    }









    // ======================================================================================
    //   GET and POST for when a user wants to play a Story
    // ======================================================================================

    /*
        This is for the first view the user meets when wanting to play a story
        It is the view where the user clicks on a story to play it and:

            A new Story PlayingSession is created between that User and Story

            If the story is public:
                Then the story immediately starts -> PlayScene()

            If the story is private:
                Then the user will meet a form before PlayScene()
                This form is for the user to write the code for the private story
                If the code is correct, they will start playing the story -> PlayScene()
                If the code is incorrect, they must try again (get correct code from teacher)
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ConfirmStartStory(int storyId)
    {
        var story = await _storyRepository.GetStoryById(storyId);
        if (story == null) return NotFound();

        var playingSession = await BeginPlayingSessionAsync(story);

        return RedirectToAction("PlayScene", "StoryPlaying", new
        {
            sceneId = playingSession.CurrentSceneId,
            SceneType = playingSession.CurrentSceneType,
            sessionId = playingSession.PlayingSessionId
        });
    }


    [HttpGet]
    [Authorize]
    public async Task<IActionResult> StartStory(int storyId)
    {
        try
        {
            var story = await _storyRepository.GetStoryById(storyId);
            if (story == null)
            {
                _logger.LogWarning("[StoryPlayingController -> StartStory:GET] StoryId {storyId} not found.", storyId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Story not found",
                    ErrorMessage = "We couldn't find the story you're trying to play. Please check and try again."
                });
            }

            // If the story is public, skip the code entry step entirely 
            if (story.Accessible == Accessibility.Public)
            {
                var playingSession = await BeginPlayingSessionAsync(story);

                return RedirectToAction("PlayScene", "StoryPlaying", new
                {
                    sceneId = playingSession.CurrentSceneId,
                    SceneType = playingSession.CurrentSceneType,
                    sessionId = playingSession.PlayingSessionId
                });
            }

            // If the story is private, create the viewmodel for code-entry view
            var viewModel = new StartPrivateStoryViewModel
            {
                StoryId = storyId,
                Title = story.Title,
                Accessibility = story.Accessible
            };

            // Show the view which contains the code-entry form 
            // (Views/StoryPlaying/StartPrivateStory.cshtml)
            return View("StartPrivateStory", viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> StartStory:GET] Unexpected error for storyId {StoryId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while starting your story. Please try again later."
            });
        }
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> StartPrivateStory(StartPrivateStoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StoryPlayingController -> StartPrivateStory:POST] Model state invalid for storyId={StoryId}", model.StoryId);
            return View(model);
        }

        try
        {
            var story = await _storyRepository.GetStoryById(model.StoryId);
            if (story == null)
            {
                _logger.LogWarning("[StoryPlayingController -> StartStory:POST] StoryId {StoryId} not found.", model.StoryId);
                return NotFound("Story not found.");
            }

            // If story is private, validate code
            if (story.Accessible == Accessibility.Private)
            {
                if (string.IsNullOrWhiteSpace(model.Code) || model.Code != story.Code)
                {
                    model.Title = story.Title;
                    model.Accessibility = story.Accessible;
                    model.ErrorMessage = "Invalid access code, try again.";
                    return View(model);
                }
            }

            var playingSession = await BeginPlayingSessionAsync(story);

            return RedirectToAction("PlayScene", "StoryPlaying", new
            {
                sceneId = playingSession.CurrentSceneId,
                SceneType = playingSession.CurrentSceneType,
                sessionId = playingSession.PlayingSessionId
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> StartStory:POST] Unexpected error for StoryId {StoryId}", model.StoryId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while starting your story. Please try again later."
            });
        }
    }


    private async Task<PlayingSession> BeginPlayingSessionAsync(Story story)
    {
        // Get the logged-in user
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            throw new Exception("User not logged in.");
        
  
        
        // Increment 'Played' and 'Dnf' for the Story
        var incremented = await _storyRepository.IncrementPlayed(story.StoryId);
        if (!incremented)
            throw new Exception($"Failed to increment stats for StoryId {story.StoryId}");

        // Fetch the IntroScene for the Story
        var introScene = await _sceneRepository.GetIntroSceneByStoryId(story.StoryId)
            ?? throw new Exception("IntroScene not found for this story.");

        // Get the amount of questions for the Story
        var amountOfQuestions = await _storyRepository.GetAmountOfQuestionsForStory(story.StoryId)
            ?? throw new Exception("Could not get question count for this story.");

        // Calculate maximum score (safe fallback)
        var maxScore = Math.Max(amountOfQuestions * 10, 0);

        // Create a new playing session
        var playingSession = new PlayingSession
        {
            StartTime = DateTime.UtcNow,
            Score = 0,
            MaxScore = maxScore,
            CurrentLevel = 3,
            CurrentSceneId = introScene.IntroSceneId,
            CurrentSceneType = SceneType.Intro,
            StoryId = story.StoryId,
            UserId = user.Id
        };

        var sessionAdded = await _playingSessionRepository.AddPlayingSession(playingSession);
        if (!sessionAdded)
            throw new Exception("Failed to create playing session.");

        return playingSession;
    }





    // ======================================================================================
    //   GET and POST for showing a scene (Introduction, Question, Ending) to the user
    // ======================================================================================

    /*
        This is the second view the user meets when playing a story
        This is the core gameplay loop endpoint, used to show scenes

        Responsibilities:

            GET:
                Display the current scene (Intro, Question, or Ending)

            POST:
                Handle player input:
                    If intro → move to first question scene
                    If question → process answer, update score, and move to next scene
                    If ending → finalize session and redirect to result/summary screen
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PlayScene(int sceneId, SceneType sceneType, int sessionId)
    {
        try
        {
            // Fetch the scene based on SceneType
            object? scene = sceneType switch
            {
                SceneType.Intro => await _sceneRepository.GetIntroSceneById(sceneId),
                SceneType.Question => await _sceneRepository.GetQuestionSceneWithAnswerOptionsById(sceneId),
                SceneType.Ending => await _sceneRepository.GetEndingSceneById(sceneId),
                _ => null
            };

            if (scene == null)
            {
                _logger.LogWarning("[StoryPlayingController -> PlayScene:GET] Scene not found. SceneId={SceneId}, Type={SceneType}", sceneId, sceneType);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Scene not found",
                    ErrorMessage = "We could not load the requested scene. Please try again later."
                });
            }

            var session = await _playingSessionRepository.GetPlayingSessionById(sessionId);
            if (session == null)
            {
                _logger.LogWarning("[StoryPlayingController -> PlayScene:GET] Session not found. SessionId={SessionId}", sessionId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Session not found",
                    ErrorMessage = "Your playing session could not be found. Please start the story again."
                });
            }

            // Default to all SceneTypes
            IEnumerable<AnswerOption>? filteredOptions = null;
            Guid answerRandomSeed = Guid.NewGuid(); // generate new seed 

            if (scene is QuestionScene questionScene)
            {
                // The variable 'questionScene' is now safely typed as QuestionScene
                // I can therefore access the AnswerOptions navigation property
                // Apply filtering using the new seed
                filteredOptions = FilterAnswerOptionsByLevel(
                    questionScene.AnswerOptions,
                    session.CurrentLevel,
                    answerRandomSeed
                );
            }

            // Extract the scene text 
            var sceneText = sceneType switch
            {
                SceneType.Intro => ((IntroScene)scene).IntroText,
                SceneType.Question => ((QuestionScene)scene).SceneText,
                SceneType.Ending => ((EndingScene)scene).EndingText,
                _ => string.Empty
            };

            var question = sceneType == SceneType.Question
                ? ((QuestionScene)scene).Question
                : null;

            var viewModel = new PlaySceneViewModel
            {
                SessionId = sessionId,
                SceneId = sceneId,
                SceneText = sceneText,
                SceneType = sceneType,
                Question = question,
                AnswerOptions = filteredOptions,
                AnswerRandomSeed = answerRandomSeed,
                CurrentScore = session.Score,
                MaxScore = session.MaxScore,
                CurrentLevel = session.CurrentLevel
            };

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> PlayScene:GET] Unexpected error for SceneId={SceneId}, Type={SceneType}, SessionId={SessionId}", sceneId, sceneType, sessionId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while loading the scene. Please try again later."
            });
        }
    }


    // Private method to decide the amount of AnswerOptions given to a user in a Question Scene
    // Level 3 --> show all AnswerOptions (user has a 25% chance of getting it right)
    // Level 2 --> show 3/4 AnswerOptions (user has a 33.33% chance of getting it right)
    // Level 1 --> show 2/4 AnswerOptions (user has a 50% chance of getting it right)
    private static IEnumerable<AnswerOption> FilterAnswerOptionsByLevel(
        IEnumerable<AnswerOption> allOptions,
        int currentLevel,
        Guid seed
    )
    {
        var allOptionsList = allOptions?.ToList() ?? new List<AnswerOption>();
        if (allOptionsList.Count == 0)
            return Enumerable.Empty<AnswerOption>();

        // Use the Guid's hash code to create a stable Random object
        // Note: HashCode is sufficient for seeding the Random class.
        var random = new Random(seed.GetHashCode());

        // Extract the one correct AnswerOption
        var correctOption = allOptionsList.FirstOrDefault(ao => ao.IsCorrect);
        if (correctOption == null)
            throw new InvalidOperationException("QuestionScene has no correct AnswerOption.");

        // Extract the three remaining incorrect AnswerOptions
        var wrongOptions = allOptionsList.Where(ao => !ao.IsCorrect).ToList();
        if (wrongOptions.Count < 1)
            throw new InvalidOperationException("QuestionScene must have at least one incorrect AnswerOption.");

        // Decide how many AnswerOptions to show in the UI based on user's current level
        int totalOptions = currentLevel switch
        {
            3 => Math.Min(4, allOptionsList.Count), // on level 3: the 1 correct + 3 wrong
            2 => Math.Min(3, allOptionsList.Count), // on level 2: the 1 correct + 2 wrong
            1 => Math.Min(2, allOptionsList.Count), // on level 1: the 1 correct + 1 wrong
            _ => Math.Min(4, allOptionsList.Count)  // default fallback (should never happen if data is valid)
        };

        // Create a list with the correct AnswerOption 
        var selectedOptions = new List<AnswerOption> { correctOption };

        // Randomly select and insert the subset of incorrect AnswerOptions
        // Use the seeded 'random' object for predictable ordering
        var randomWrong = wrongOptions.OrderBy(_ => random.Next()).Take(totalOptions - 1);
        selectedOptions.AddRange(randomWrong);

        // Shuffle final list to avoid predictable positioning of the correct answer
        // Use the seeded 'random' object for predictable shuffling
        return selectedOptions.OrderBy(_ => random.Next());
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlayScene(PlaySceneViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // ModelState is only invalid when it is a QuestionScene without a chosen answer option
            // In that case, I am re-fetching the QuestionScene to repopulate the Question and AnswerOptions 
            if (model.SceneType == SceneType.Question)
            {
                var questionScene = await _sceneRepository.GetQuestionSceneWithAnswerOptionsById(model.SceneId);
                if (questionScene != null)
                {
                    model.Question = questionScene.Question;
                    model.AnswerOptions = FilterAnswerOptionsByLevel(
                        questionScene.AnswerOptions,
                        model.CurrentLevel,
                        model.AnswerRandomSeed
                    );
                }
            }

            _logger.LogWarning("[StoryPlayingController -> PlayScene:POST] Model state invalid");
            return View(model);
        }

        try
        {
            // Fetch the scene based on SceneType
            object? scene = model.SceneType switch
            {
                SceneType.Intro => await _sceneRepository.GetIntroSceneById(model.SceneId),
                SceneType.Question => await _sceneRepository.GetQuestionSceneById(model.SceneId),
                SceneType.Ending => await _sceneRepository.GetEndingSceneById(model.SceneId),
                _ => null
            };

            if (scene == null)
            {
                _logger.LogWarning("[StoryPlayingController -> PlayScene:POST] Scene of type {sceneType} could not be found for id SceneId={SceneId}",
                    model.SceneType, model.SceneId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Scene not found",
                    ErrorMessage = "We could not load the requested scene. Please try again later."
                });
            }

            return await (scene switch
            {
                IntroScene introScene
                    => HandleIntroScene(model, introScene),
                QuestionScene questionScene
                    => HandleQuestionScene(model, questionScene),
                EndingScene endingScene
                    => HandleEndingScene(model, endingScene),
                _ => Task.FromResult<IActionResult>(BadRequest("Invalid scene type."))
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> PlayScene:POST] Unexpected error for SceneId={SceneId}, SceneType={SceneType}, SessionId={SessionId}",
                model.SceneId, model.SceneType, model.SessionId);

            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while processing your action. Please try again later."
            });
        }
    }


    // When user advances from Intro Scene to the first Question Scene
    private async Task<IActionResult> HandleIntroScene(PlaySceneViewModel model, IntroScene introScene)
    {
        var nextScene = await _sceneRepository.GetFirstQuestionSceneByStoryId(introScene.StoryId);
        if (nextScene == null)
            throw new Exception($"No QuestionScene found for StoryId {introScene.StoryId}.");

        // Move the session to the next scene
        var moved = await _playingSessionRepository.MoveToNextScene(model.SessionId, nextScene.QuestionSceneId, SceneType.Question);
        if (!moved)
            throw new Exception($"Failed to move playing session {model.SessionId} to QuestionScene {nextScene.QuestionSceneId}.");

        // Redirect the user to the next scene
        return RedirectToAction("PlayScene", "StoryPlaying", new
        {
            sceneId = nextScene.QuestionSceneId,
            sceneType = SceneType.Question,
            sessionId = model.SessionId
        });
    }


    // When user advances from a Question Scene to: Next Question Scene || An Ending Scene
    private async Task<IActionResult> HandleQuestionScene(PlaySceneViewModel model, QuestionScene questionScene)
    {
        if (model.SelectedAnswerId == null)
            throw new Exception("No answer selected.");

        var selectedAnswer = await _answerOptionRepository.GetAnswerOptionById(model.SelectedAnswerId.Value);
        if (selectedAnswer == null)
            throw new Exception($"Answer option with ID {model.SelectedAnswerId.Value} not found.");

        bool isCorrect = selectedAnswer?.IsCorrect ?? false;

        var session = await _playingSessionRepository.GetPlayingSessionById(model.SessionId);
        if (session == null)
            throw new Exception($"Playing session {model.SessionId} could not be found.");

        // Calculate the new score through a private method (see below)
        int pointsEarned = isCorrect ? GetPointsForCorrectAnswer(session.CurrentLevel) : 0;
        int newScore = session.Score + pointsEarned;

        // Calculate next level
        int newLevel = isCorrect
            ? Math.Min(session.CurrentLevel + 1, 3) // not going above Level 3
            : Math.Max(session.CurrentLevel - 1, 1); // drop a level if wrong answer

        // Case: user choses wrong AnswerOption on (easiest) level 1
        if (!isCorrect && session.CurrentLevel == 1)
        {
            // Update the PlayingSession by finishing it
            var finished = await _playingSessionRepository.FinishSession(model.SessionId, newScore, newLevel);
            if (!finished)
                throw new Exception($"Failed to finish session {model.SessionId} after incorrect Level 1 answer.");

            // Retireve the Story 
            var story = await _storyRepository.GetStoryById(session.StoryId);
            if (story == null)
                throw new Exception($"Story not found for session {model.SessionId}.");

            // Increment 'Failed' and decrement 'Dnf':
            var failed = await _storyRepository.IncrementFailed(story.StoryId);
            if (!failed)
                throw new Exception($"Failed to increment 'Failed' count for StoryId {story.StoryId}.");

            // Redirect to the summary view
            return RedirectToAction("FinishStory", "StoryPlaying", new { sessionId = model.SessionId });
        }

        // Case: user proceeds to feedback view
        // Stores temporary state
        TempData["NewScore"] = newScore;
        TempData["NewLevel"] = newLevel;
        TempData["SelectedAnswerText"] = selectedAnswer?.FeedbackText;
        TempData["SessionId"] = model.SessionId;
        TempData["StoryId"] = questionScene.StoryId;

        // Store the next scene ID (we will move there after the feedback)
        QuestionScene? nextScene = null;
        if (questionScene.NextQuestionSceneId.HasValue)
        {
            nextScene = await _sceneRepository.GetQuestionSceneById(questionScene.NextQuestionSceneId.Value);
            if (nextScene == null)
                throw new Exception($"Next QuestionScene {questionScene.NextQuestionSceneId.Value} not found for StoryId {questionScene.StoryId}.");
        }

        TempData["NextSceneId"] = nextScene?.QuestionSceneId;
        TempData["NextSceneType"] = nextScene != null ? SceneType.Question : SceneType.Ending;

        // Redirect to feedback
        return RedirectToAction("AnswerFeedback", "StoryPlaying");
    }


    // Private method to decide the amount of points
    // The points are based on the level the user is on
    // Level 3 = 10, Level 2 = 5, Level 1 = 1 (can be changed)
    private static int GetPointsForCorrectAnswer(int currentLevel)
    {
        return currentLevel switch
        {
            3 => 10,
            2 => 5,
            1 => 1,
            _ => 0
        };
    }


    // The case where the user finishes the Story by advancing from Ending Scene to a summary view
    private async Task<IActionResult> HandleEndingScene(PlaySceneViewModel model, EndingScene endingScene)
    {
        // Mark the PlayingSession as finished
        var finished = await _playingSessionRepository.FinishSession(model.SessionId, model.CurrentScore, model.CurrentLevel);
        if (!finished)
            throw new Exception($"Failed to mark session {model.SessionId} as finished.");

        // Increment 'Finished' and decrement 'Dnf'
        var updated = await _storyRepository.IncrementFinished(endingScene.StoryId);
        if (!updated)
            throw new Exception($"Failed to increment 'Finished' count for StoryId {endingScene.StoryId}.");

        // Redirect to summary view
        return RedirectToAction("FinishStory", "StoryPlaying", new
        {
            sessionId = model.SessionId
        });
    }





    // ======================================================================================
    //   GET and POST for showing feedback text to the user as a Scene
    // ======================================================================================

    /*
        This section handles the intermediate "feedback scene" shown between two Question Scenes.

        When a player selects an answer in a Question Scene, we don't immediately move on to the next Question.
        Instead, we show them a short piece of text (the AnswerOption.SceneText) that describes the outcome
        or narrative consequence of their choice.

        Responsibilities:

            GET:
                - Display the feedback SceneText associated with the user's chosen AnswerOption
                - Retrieve temporary session and scoring data from TempData (set in HandleQuestionScene())
                - Keep TempData alive for the next request since it’s needed for the POST

            POST:
                - When the player clicks “Continue” from the feedback view:
                    * Update the PlayingSession with the new score and level that were calculated earlier
                - If there is a next Scene (another Question Scene), move to that Scene
                - If there is no next Scene (the story has reached its end):
                    * Determine the correct Ending Scene based on the user's performance and redirect to it
    */

    [HttpGet]
    [Authorize]
    public IActionResult AnswerFeedback()
    {
        try
        {
            var viewModel = new AnswerFeedbackViewModel
            {
                SceneText = TempData["SelectedAnswerText"] as string ?? string.Empty,
                SessionId = (int)(TempData["SessionId"] ?? 0),
                StoryId = (int)(TempData["StoryId"] ?? 0),
                NextSceneId = TempData["NextSceneId"] as int?,
                NextSceneType = (SceneType)TempData["NextSceneType"]!,
                NewScore = (int)(TempData["NewScore"] ?? 0),
                NewLevel = (int)(TempData["NewLevel"] ?? 1)
            };

            // Re-store these values in TempData for the POST request
            TempData.Keep();

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> AnswerFeedback:GET] Unexpected error while loading feedback scene.");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while loading the feedback screen. Please try again."
            });
        }
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AnswerFeedback(AnswerFeedbackViewModel model)
    {
        // No need to check ModelState

        try
        {
            var sessionId = model.SessionId;
            var newScore = model.NewScore;
            var newLevel = model.NewLevel;
            var nextSceneId = model.NextSceneId;
            var nextSceneType = model.NextSceneType;
            var storyId = model.StoryId;

            // Case 1: No next QuestionScene -> must transition to an EndingScene
            if (nextSceneId == null)
            {
                var session = await _playingSessionRepository.GetPlayingSessionById(sessionId);
                if (session == null)
                {
                    _logger.LogWarning("[StoryPlayingController -> AnswerFeedback:POST] Session not found for SessionId {sessionId}", sessionId);
                    return View("Error", new ErrorViewModel
                    {
                        ErrorTitle = "Session not found",
                        ErrorMessage = "We could not find your current playing session. Please restart the story."
                    });
                }

                var endingScene = await GetEndingSceneAsync(storyId, newScore, session!.MaxScore);
                if (endingScene == null)
                {
                    _logger.LogWarning("[StoryPlayingController -> AnswerFeedback:POST] No ending scene found for StoryId {storyId}", storyId);
                    return View("Error", new ErrorViewModel
                    {
                        ErrorTitle = "Ending not found",
                        ErrorMessage = "We could not determine an appropriate ending for your story. Please try again later."
                    });
                }

                var updated = await _playingSessionRepository.AnswerQuestion(sessionId, endingScene.EndingSceneId, nextSceneType, newScore, newLevel);
                if (!updated)
                {
                    _logger.LogWarning("[StoryPlayingController -> AnswerFeedback:POST] Failed to update playing session for ending transition. SessionId={SessionId}", sessionId);
                    return View("Error", new ErrorViewModel
                    {
                        ErrorTitle = "Update failed",
                        ErrorMessage = "We encountered a problem saving your story progress. Please try again."
                    });
                }

                return RedirectToAction("PlayScene", "StoryPlaying", new
                {
                    sceneId = endingScene.EndingSceneId,
                    sceneType = nextSceneType,
                    sessionId
                });
            }

            // Case 2: Continue to next QuestionScene
            var success = await _playingSessionRepository.AnswerQuestion(sessionId, nextSceneId.Value, nextSceneType, newScore, newLevel);
            if (!success)
            {
                _logger.LogWarning("[StoryPlayingController -> AnswerFeedback:POST] Failed to update playing session. SessionId={SessionId}, NextSceneId={NextSceneId}", sessionId, nextSceneId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Progress not saved",
                    ErrorMessage = "We couldn’t update your story progress. Please try again."
                });
            }

            return RedirectToAction("PlayScene", "StoryPlaying", new
            {
                sceneId = nextSceneId,
                sceneType = nextSceneType,
                sessionId
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> AnswerFeedback:POST] Unexpected error while handling feedback transition for SessionId {SessionId}", model.SessionId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while processing your answer. Please try again."
            });
        }
    }


    // Private method to decide which Ending Scene the user sees at the end of the story
    // If user's score is 80% or more of the max achievable score --> Good Ending
    // If user's score is 40-79% of the max achievable score --> Neutral Ending
    // If user's score is less than 40% of the max achievable score --> Bad Ending
    // These stats are just an example, they can be changed
    private async Task<EndingScene?> GetEndingSceneAsync(int storyId, int score, int maxScore)
    {
        if (maxScore <= 0)
            throw new ArgumentException($"Max score must be greater than zero, but it is {maxScore}.");

        double percentage = (double)score / maxScore * 100;

        EndingScene? endingScene = percentage switch
        {
            >= 80 => await _sceneRepository.GetGoodEndingSceneByStoryId(storyId),
            >= 40 => await _sceneRepository.GetNeutralEndingSceneByStoryId(storyId),
            _ => await _sceneRepository.GetBadEndingSceneByStoryId(storyId)
        };

        if (endingScene == null)
            throw new InvalidOperationException($"No ending scene found for story {storyId} (score {score}/{maxScore}).");

        return endingScene;
    }





    // ======================================================================================
    //   GET and POST for handling when the user finishes a Story
    // ======================================================================================

    /*
        This section handles the final stage of the story-playing experience.
        When a player reaches an Ending Scene (Good, Neutral, or Bad), the game session
        is considered complete, and the player is redirected here to see their results.

        Responsibilities:

            GET:
                - Retrieve the finished PlayingSession using the sessionId
                - Load the associated Story details for display
                - Populate a simple summary view model (FinishStoryViewModel)
                - Display a summary screen where the player can review their performance

            POST:
                - Triggered when the player clicks “Return to Home” or similar.
                - Simply redirects the user back to the main home page
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> FinishStory(int sessionId)
    {
        try
        {
            var session = await _playingSessionRepository.GetPlayingSessionById(sessionId);
            if (session == null)
            {
                _logger.LogWarning("[StoryPlayingController -> FinishStory:GET] Session not found for SessionId {sessionId}", sessionId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Session not found",
                    ErrorMessage = "We could not find your playing session. Please restart the story."
                });
            }

            var story = await _storyRepository.GetStoryById(session.StoryId);
            if (story == null)
            {
                _logger.LogWarning("[StoryPlayingController -> FinishStory:GET] Story not found for SessionId={SessionId}, StoryId={StoryId}", sessionId, session.StoryId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Story not found",
                    ErrorMessage = "We could not load the story details for this session. Please try again later."
                });
            }

            var viewModel = new FinishStoryViewModel
            {
                StoryTitle = story.Title,
                FinalScore = session.Score,
                MaxScore = session.MaxScore,
            };

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> FinishStory:GET] Unexpected error while finishing session {SessionId}", sessionId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while loading the end of your story. Please try again later."
            });
        }
    }


    [HttpPost]
    [Authorize]
    public IActionResult FinishStory()
    {
        try
        {
            // Redirect back to the home-page 
            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryPlayingController -> FinishStory:POST] Unexpected error while redirecting to homepage after playing session finished");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Unexpected error",
                ErrorMessage = "Something went wrong while returning to the main page. Please try again."
            });
        }
    }
}