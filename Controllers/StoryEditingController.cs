using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jam.DAL.StoryDAL;
using Jam.DAL.SceneDAL;
using Jam.DAL.AnswerOptionDAL;
using Jam.Models.Enums;
using Jam.ViewModels.StoryEditing;
using Jam.ViewModels;
using Jam.Models;
using Microsoft.AspNetCore.Authorization;
using Jam.DAL;

namespace Jam.Controllers;

/*
    StoryEditingController
        It is a little bit difficult to handle the editing-part in this application.
        That is because the user can essentialy edit a story in two different scenarios.
        
        There are two distinct "editing" scenarios:
        
        1. "In-Creation" Editing:
                The user is actively creating a new story (handled by StoryCreationController).
                During creation, the user can immediately edit scenes, questions, and answers
                before finalizing the story for the first time. For example, the user can be in  
                the final view in story-creation, where they are creating the 3 possible Ending 
                Scenes, and suddenly they decide they want to navigate back to the previous 
                view (the view where they created the QuestionScenes), to make some changes. 

        2. "Post-Creation" Editing:
                The user (a teacher) wants to edit a story they have already created previously. 
                For example, user (teacher) created a story and finished it completely yeasterday. 
                As users (pupils) start to play the story the next day, they complain about typos 
                in scenes or the fact that the Story is 'Hard', even though the creator (teacher) 
                marked it as 'Medium'. Therefore, the creator (teacher) wants to go to their home-
                page to access the story the players (pupils) complained about, to make adjustments.  

        Question: where should logic for editing a story be? 
            Should any editing no matter the scenario be handled from this controller? If that is
            the case, then I think the StoryCreationController and StoryEditingController has to
            communicate together, which might be difficult to handle and also hard to understand?
            
            Or should editing be splitted between StoryEditingcontroller and StoryCreationController? 
            If thats the case, the StoryCreationController handes the first scenario explained above 
            where the user decides they want to edit something while they are in Creation-mode, while 
            the StoryEditingController handles the second scenario explained above where the user 
            wants to edit a story after it has been created (when user is no longer in creation-mode).

    Future update
        Currently, StoryCreationControlelr and StoryEditingController are sharing a very similar logic
        for creating and updating QuestionScenes. They are using a base viewmodel, with the same partial
        view for creating and edititng QuestionScenes. I think this will also make it easier to handle 
        the editing part in creation mode. I dont have a similar shared logic for StoryCreationController
        and StoryEditingController when it comes to IntroScene and EndingScenes, that is completely 
        separate. It should probably be shared as well. 
*/

public class StoryEditingController : Controller
{
    private readonly IStoryRepository _storyRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IAnswerOptionRepository _answerOptionRepository;
    private readonly StoryDbContext _db;

    private readonly ILogger<StoryEditingController> _logger;

    public StoryEditingController(
        IStoryRepository storyRepository,
        ISceneRepository sceneRepository,
        IAnswerOptionRepository answerOptionRepository,
        StoryDbContext db,
        ILogger<StoryEditingController> logger
    )
    {
        _storyRepository = storyRepository;
        _sceneRepository = sceneRepository;
        _answerOptionRepository = answerOptionRepository;
        _db = db;
        _logger = logger;
    }


    // ======================================================================================
    //   GET and POST for handling the user updating a Story's metadata
    // ======================================================================================

    /*
        This is for the first view the user meets when wanting to edit a story
        Here, the user can edit the metadata for the story they clicked

        Responsibilities:

            GET:
                Display the form for editing the story metadata

            POST:
                Update the story by calling the UpdateStory-method in StoryRepository
                If the story's accessibility changes from private to public -> remove code
                If the story's accessibility changes from public to private -> generate code
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditStory(int storyId)
    {
        try
        {
            var story = await _storyRepository.GetStoryById(storyId);
            if (story == null)
            {
                _logger.LogWarning("[StoryEditingController -> EditStory:GET] Story not found for storyId={storyId}", storyId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Story not found",
                    ErrorMessage = "We could not load the story details for this story. Please try again later."
                });
            }

            var viewModel = new EditStoryViewModel
            {
                StoryId = story.StoryId,
                Title = story.Title,
                Description = story.Description,
                DifficultyLevel = story.DifficultyLevel,
                Accessibility = story.Accessible,
            };

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryEditingController -> EditStory:GET] Unexpected error while loading edit form for storyId={storyId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not load the story edit form. Please try again later."
            });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStory(EditStoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StoryEditingController -> EditStory:POST] Invalid model state for storyId={storyId}", model.StoryId);
            return View(model);
        }

        try
        {
            var story = await _storyRepository.GetStoryById(model.StoryId);
            if (story == null)
            {
                _logger.LogWarning("[StoryEditingController -> EditStory:POST] Story not found for storyId={storyId}", model.StoryId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Story not found",
                    ErrorMessage = "We could not load the story details for this story. Please try again later."
                });
            }

            // If the user has decided to change the accessibility for the Story
            if (story.Accessible != model.Accessibility)
            {
                if (model.Accessibility == Accessibility.Private)
                {
                    // Generate a unique code when switching from Public -> Private
                    story.Code = await GenerateUniqueStoryCodeAsync();
                }
                else if (model.Accessibility == Accessibility.Public)
                {
                    // Remove the code when switching from Private -> Public
                    story.Code = null;
                }
            }

            // Change the editable fields
            story.Title = model.Title;
            story.Description = model.Description;
            story.DifficultyLevel = model.DifficultyLevel;
            story.Accessible = model.Accessibility;

            // Save changes
            var updated = await _storyRepository.UpdateStory(story);
            if (!updated)
            {
                _logger.LogWarning("[StoryEditingController -> EditStory:POST] Failed to update story metadata for storyId={storyId}", model.StoryId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Update failed",
                    ErrorMessage = "We encountered a problem updating your story. Please try again."
                });
            }

            // Redirect back to home-page 
            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryEditingController -> EditStory:POST] Unexpected error when trying to update storyId={storyId}", model.StoryId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not update your story. Please try again later."
            });
        }
    }

    // This method creates a code for stories which has changed from Public to Private 
    // (it is the exact same method used StoryCreationController, shoud move this to one place)
    private async Task<string> GenerateUniqueStoryCodeAsync()
    {
        string code;
        bool exists;

        // This generates an 8-char code which most likely is unique
        // However, there is no guarentee that it is unique, 
        // which is why we still have to check it against the database
        // DoesCodeExist() returns True if the code does exist in the db -> make new code
        // DoesCodeExist() returns False if the code doesnt exist in the db -> good to go
        do
        {
            code = Guid.NewGuid().ToString("N")[..8].ToUpper();
            exists = await _storyRepository.DoesCodeExist(code);
        }
        while (exists);

        return code;
    }




    // ======================================================================================
    //   GET and POST for handling the user updating the IntroScene for a Story
    // ======================================================================================

    /*
        This is for the view the user meets when wanting to edit the IntroScene for a Story
        Here the user can only edit the IntroText for the IntroScene, that is it for editing 

        Responsibilities:

            GET:
                Display the form for editing an IntroScene

            POST:
                Update the IntroScene by calling the UpdateIntroScene-method in SceneRepository
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditIntroScene(int storyId)
    {
        try
        {
            var introScene = await _sceneRepository.GetIntroSceneByStoryId(storyId);
            if (introScene == null)
            {
                _logger.LogWarning("[StoryEditingController -> EditIntroScene:GET] IntroScene not found for storyId={storyId}", storyId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Intro scene not found",
                    ErrorMessage = "We could not find the intro scene for this story. Please try again later."
                });
            }

            var viewModel = new EditIntroSceneViewModel
            {
                StoryId = introScene.StoryId,
                IntroText = introScene.IntroText
            };

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryEditingController -> EditIntroScene:GET] Unexpected error while initializing view model for storyId={storyId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not load the intro scene editor. Please try again later."
            });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditIntroScene(EditIntroSceneViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StoryEditingController -> EditIntroScene:POST] Invalid model state for storyId={storyId}", model.StoryId);
            return View(model);
        }

        try
        {
            var introScene = await _sceneRepository.GetIntroSceneByStoryId(model.StoryId);
            if (introScene == null)
            {
                _logger.LogWarning("[StoryEditingController -> EditIntroScene:POST] IntroScene not found for storyId={storyId}", model.StoryId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Intro scene not found",
                    ErrorMessage = "We could not load the intro scene for editing. Please try again later."
                });
            }

            introScene.IntroText = model.IntroText;

            var updated = await _sceneRepository.UpdateIntroScene(introScene);
            if (!updated)
            {
                _logger.LogWarning("[StoryEditingController -> EditIntroScene:POST] Failed to update intro scene with id {introSceneId}", introScene.IntroSceneId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Update failed",
                    ErrorMessage = "We encountered a problem updating your intro scene. Please try again."
                });
            }

            // Success -> redirect back to the EditStory page for continuity
            return RedirectToAction("EditStory", "StoryEditing", new
            {
                storyId = model.StoryId
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryEditingController -> EditIntroScene:POST] Unexpected error while updating intro scene for storyId={storyId}", model.StoryId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not update the intro scene. Please try again later."
            });
        }
    }




    // ======================================================================================
    //   GET and POST for handling the user updating one or more QuestionScenes for a Story
    // ======================================================================================

    /*
         

        Responsibilities:

            GET:
                

            POST:
                
    */

    // ===================== QUESTION SCENES (EDIT) =====================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditQuestionScenes(int storyId)
    {
        var questionScenes = await _db.QuestionScenes
                .Include(q => q.AnswerOptions)
                .Where(q => q.StoryId == storyId)
                .OrderBy(q => q.QuestionSceneId)
                .ToListAsync();

        if (!questionScenes.Any())
        {
            TempData["ErrorMessage"] = "No question scenes found for this story.";
            return RedirectToAction("EditStory", new { storyId });
        }

        var viewModels = questionScenes.Select((scene, i) => new QuestionSceneBaseViewModel
        {
            QuestionSceneId = scene.QuestionSceneId,
            StoryId = scene.StoryId,
            StoryText = scene.SceneText,
            QuestionText = scene.Question,
            Answers = scene.AnswerOptions.Select(a => new AnswerOptionInput
            {
                AnswerText = a.Answer,
                ContextText = a.FeedbackText
            }).ToList(),
            CorrectAnswerIndex = scene.AnswerOptions.FindIndex(a => a.IsCorrect),
            IsEditing = true
        }).ToList();

        // Send nummer til partial for visning "Question 1", "Question 2" osv.
        ViewBag.QuestionNumbers = Enumerable.Range(1, viewModels.Count).ToList();
        return View(viewModels);
    }


    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestionScenes(int storyId, List<QuestionSceneBaseViewModel> model, string action)
    {
        // ---------- BACK ----------
        if (action == "back")
        {
            ModelState.Clear();
            return RedirectToAction("EditStory", new { storyId });
        }

        // ---------- DELETE INLINE ----------
        if (action.StartsWith("deleteInline"))
        {
            ModelState.Clear();

            var parts = action.Split(':');
            if (parts.Length == 2)
            {
                var rawIndex = parts[1].Trim('[', ']'); // fikser formatet [4] -> 4

                if (int.TryParse(rawIndex, out int index) && index >= 0 && index < model.Count)
                {
                    var question = model[index];

                    if (question.QuestionSceneId == 0)
                    {
                        // Fjern nye spørsmål direkte
                        model.RemoveAt(index);
                    }
                    else
                    {
                        // Marker eksisterende spørsmål for sletting i databasen
                        question.MarkedForDeletion = true;
                    }
                }
            }

            // Pass på at det alltid finnes minst ett spørsmål
            if (model.Count == 0)
            {
                model.Add(new QuestionSceneBaseViewModel
                {
                    StoryId = storyId,
                    StoryText = "",
                    QuestionText = "",
                    Answers = new List<AnswerOptionInput>
                {
                    new(), new(), new(), new()
                },
                    CorrectAnswerIndex = -1
                });
            }

            ViewBag.QuestionNumbers = Enumerable.Range(1, model.Count).ToList();
            return View("EditQuestionScenes", model);
        }

        // ---------- ADD ----------
        if (action == "add")
        {
            ModelState.Clear();

            var newScene = new QuestionSceneBaseViewModel
            {
                StoryId = storyId,
                StoryText = "",
                QuestionText = "",
                Answers = new List<AnswerOptionInput>
            {
                new(), new(), new(), new()
            },
                CorrectAnswerIndex = -1,
                IsEditing = true
            };

            model.Add(newScene);
            ViewBag.QuestionNumbers = Enumerable.Range(1, model.Count).ToList();
            return View("EditQuestionScenes", model);
        }

        // ---------- SAVE ----------
        if (action == "save")
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct validation errors before saving.";
                ViewBag.QuestionNumbers = Enumerable.Range(1, model.Count).ToList();
                return View("EditQuestionScenes", model);
            }

            // Slett de markerte spørsmålene
            var deleted = model.Where(vm => vm.MarkedForDeletion && vm.QuestionSceneId != 0);
            foreach (var del in deleted)
            {
                await _sceneRepository.DeleteQuestionScene(del.QuestionSceneId);
            }

            // Oppdater resten
            var questionScenes = model
                .Where(vm => !vm.MarkedForDeletion)
                .Select(vm => new QuestionScene
                {
                    QuestionSceneId = vm.QuestionSceneId,
                    StoryId = storyId,
                    SceneText = vm.StoryText,
                    Question = vm.QuestionText,
                    AnswerOptions = vm.Answers.Select((a, i) => new AnswerOption
                    {
                        Answer = a.AnswerText,
                        FeedbackText = a.ContextText,
                        IsCorrect = i == vm.CorrectAnswerIndex
                    }).ToList()
                })
                .ToList();

            foreach (var scene in questionScenes)
            {
                foreach (var answer in scene.AnswerOptions)
                    answer.QuestionScene = scene;
            }

            var success = await _sceneRepository.UpdateQuestionScenes(questionScenes);

            if (!success)
            {
                TempData["ErrorMessage"] = "Something went wrong while saving your changes.";
                ViewBag.QuestionNumbers = Enumerable.Range(1, model.Count).ToList();
                return View("EditQuestionScenes", model);
            }

            TempData["SuccessMessage"] = "All changes saved successfully.";
            return RedirectToAction("EditStory", "StoryEditing", new { storyId });
        }

        // Default fallback
        return View("EditQuestionScenes", model);
    }



    [HttpGet]
    public IActionResult GetEditQuestionScenePartial(int index, int questionNumber)
    {
        var viewModel = new QuestionSceneBaseViewModel
        {
            Answers = new List<AnswerOptionInput>
        {
            new(), new(), new(), new()
        }
        };

        ViewData["QuestionNumber"] = questionNumber;
        ViewData.TemplateInfo.HtmlFieldPrefix = $"[{index}]";

        return PartialView("~/Views/Shared/_EditQuestionScenePartial.cshtml", viewModel);
    }


    // ---------- DELETE ----------
    // ---------- GET: StoryEditing/DeleteQuestion/5 ----------
    [HttpGet]
    public async Task<IActionResult> DeleteQuestion(int? questionSceneId)
    {
        if (questionSceneId == null)
            return NotFound();

        var scene = await _sceneRepository.GetQuestionSceneWithAnswerOptionsById(questionSceneId.Value);
        if (scene == null)
            return NotFound();

        return View(scene);
    }


    [HttpPost, ActionName("DeleteQuestion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestionConfirmed(int questionSceneId, int storyId)
    {
        var success = await _sceneRepository.DeleteQuestionScene(questionSceneId);

        if (!success)
        {
            TempData["ErrorMessage"] = "Could not delete question.";
            return RedirectToAction("EditQuestionScenes", new { storyId });
        }
        

        TempData["SuccessMessage"] = "Question deleted successfully.";
        return RedirectToAction("EditQuestionScenes", new { storyId });
        
    }


    // ======================================================================================
    //   GET and POST for handling the user updating one or more EndingScenes for a Story
    // ======================================================================================

    /*
         

        Responsibilities:

            GET:
                

            POST:
                
    */

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditEndingScenes(int storyId)
    {
        try
        {
            var endingScenes = await _sceneRepository.GetEndingScenesByStoryId(storyId);
            if (!endingScenes.Any())
            {
                _logger.LogWarning("[StoryEditingController -> EditEndingScenes:GET] No EndingScenes found for storyId={storyId}", storyId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Ending scenes not found",
                    ErrorMessage = "We could not find any ending scenes to edit for this story. Please try again later."
                });
            }

            var endingsByType = endingScenes.ToDictionary(e => e.EndingType, e => e.EndingText);

            var viewModel = new EndingScenesViewModel
            {
                StoryId = storyId,
                GoodEnding = endingsByType.GetValueOrDefault(EndingType.Good, string.Empty),
                NeutralEnding = endingsByType.GetValueOrDefault(EndingType.Neutral, string.Empty),
                BadEnding = endingsByType.GetValueOrDefault(EndingType.Bad, string.Empty)
            };

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryEditingController -> EditEndingScenes:GET] Error while fetching EndingScenes for story {storyId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not load the edit ending scenes form. Please try again later."
            });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEndingScenes(EndingScenesViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StoryEditingController -> EditEndingScenes:POST] Invalid model state for storyId={storyId}", model.StoryId);
            return View(model);
        }

        try
        {
            var endingScenes = await _sceneRepository.GetEndingScenesByStoryId(model.StoryId!.Value);
            var goodEnding = endingScenes.FirstOrDefault(e => e.EndingType == EndingType.Good);
            var neutralEnding = endingScenes.FirstOrDefault(e => e.EndingType == EndingType.Neutral);
            var badEnding = endingScenes.FirstOrDefault(e => e.EndingType == EndingType.Bad);

            if (goodEnding == null || neutralEnding == null || badEnding == null)
            {
                _logger.LogWarning("[StoryEditingController -> EditEndingScenes:POST] Missing one or more ending scenes for storyId={storyId}", model.StoryId);
                ModelState.AddModelError("", "One or more ending scenes could not be found. Please contact an administrator.");
                return View(model);
            }

            goodEnding.EndingText = model.GoodEnding;
            neutralEnding.EndingText = model.NeutralEnding;
            badEnding.EndingText = model.BadEnding;

            var updated = await _sceneRepository.UpdateEndingScenes(endingScenes);
            if (!updated)
            {
                _logger.LogWarning("[StoryEditingController -> EditEndingScenes:POST] Failed to update ending scenes for storyId={storyId}", model.StoryId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Update failed",
                    ErrorMessage = "We encountered a problem updating your intro scene. Please try again."
                });
            }

            // Success -> redirect back to the EditStory page for continuity
            return RedirectToAction("EditStory", "StoryEditing", new
            {
                storyId = model.StoryId
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryEditingController -> EditEndingScenes:POST] Error while updating EndingScenes for story {storyId}", model.StoryId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not update the ending scenes. Please try again later."
            });
        }
    }
}