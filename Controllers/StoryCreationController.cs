using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Jam.DAL.AnswerOptionDAL;
using Jam.DAL.SceneDAL;
using Jam.DAL.StoryDAL;
using Jam.DTOs;
using Jam.Extensions;
using Jam.Models;
using Jam.Models.Enums;
using Jam.ViewModels;
using Jam.ViewModels.StoryCreation;

/*
    StoryCreationController
        Handles all logic for teachers / creators who are building/creating stories.
        This controller deals with a number of scenarios related to story-creation, like:
            * Creating new Story
            * Creating Introduction Scene
            * Creating Question Scenes
            * Creating the three Ending Scene types
            * Adding Question and AnswerOptions to Question Scenes
        
        Basically, everything that belongs to the “authoring tool” side of the app.
        This controller will communicate mainly with these repositories:
            * StoryRepository
            * SceneRepository
            * QuestionRepository
            * AnswerOptionRepositor

        What this controller does not yet do (but should)
            Even though the user is in creation mode, they should still be able to update
            and delete scenes as well. For instance, they should be able to navigate from
            the view for creating the 3 Ending Scenes back to the view for creating the
            Question Scenes, and here update or delete the Scenes they have created.
            I have not yet added this functionality, as it is a little bit difficult 

*/

namespace Jam.Controllers;

[Authorize]
public class StoryCreationController : Controller
{
    private readonly IStoryRepository _storyRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IAnswerOptionRepository _answerOptionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly ILogger<StoryCreationController> _logger;


    public StoryCreationController(
        IStoryRepository storyRepository,
        ISceneRepository sceneRepository,
        IAnswerOptionRepository answerOptionRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<StoryCreationController> logger
    )
    {
        _storyRepository = storyRepository;
        _sceneRepository = sceneRepository;
        _answerOptionRepository = answerOptionRepository;
        _userManager = userManager;
        _logger = logger;
    }


    // ======================================================================================
    //   GET and POST for creating a Story and Introduction Scene
    // ======================================================================================

    /*
        This is the first of 3 views the user meets when creating a story and scenes. 
        It is the view where the user creates: 
            1) The story
            2) The introduction scene
        
        Here I am converting enums into dropdown lists that Razor can automatically bind
    */

    [HttpGet]
    public IActionResult CreateStoryAndIntro()
    {
        try
        {
            // Step 1: Try to load any existing data from the session
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession");

            // Step 2: Initialize a fresh view model
            var viewModel = new CreateStoryAndIntroViewModel();

            if (sessionDto != null)
            {
                // Step 3: If the user has lready started creating a story,
                // pre-fill their previous inputs from the session
                viewModel.Title = sessionDto.Title;
                viewModel.Description = sessionDto.Description;
                viewModel.DifficultyLevel = sessionDto.DifficultyLevel;
                viewModel.Accessibility = sessionDto.Accessibility;
                viewModel.IntroText = sessionDto.IntroText;
            }

            // Step 4: Populate dropdowns for chosing Difficulty and Accessibility
            PopulateDropdowns(viewModel);

            // Step 5: Render the view
            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryCreationController -> CreateStoryAndIntro:GET] Failed to initialize view model.");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not load the story and intro creation form. Please try again later."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateStoryAndIntro(CreateStoryAndIntroViewModel model, string? action)
    {
        if (action == "cancel")
        {
            // Fjern alt som er lagret i sesjonen
             HttpContext.Session.Remove("StoryCreationSession");
            ModelState.Clear();
            return RedirectToAction("Index", "Home"); // 👈 sender til forsiden
        }
    
        if (!ModelState.IsValid)
        {
            // model.DifficultyLevelOptions = GetDifficultySelectList();
            // model.AccessibilityOptions = GetAccessibilitySelectList();

            _logger.LogWarning("[StoryCreationController -> CreateQuestionScenes:POST] Model state invalid");
            PopulateDropdowns(model); // repopulate dropdown lists since model validation failed
            return View(model);
        }

        // Step 1: Retrieve current session data or start a new one
        var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession")
                            ?? new StoryCreationSessionDto();

        // Step 2: Fill step 1 data into the DTO
        sessionDto.Title = model.Title;
        sessionDto.Description = model.Description;
        sessionDto.DifficultyLevel = model.DifficultyLevel!.Value;
        sessionDto.Accessibility = model.Accessibility!.Value;
        sessionDto.IntroText = model.IntroText;

        // Step 3: Save the updated DTO back into session
        HttpContext.Session.SetObject("StoryCreationSession", sessionDto);

        // Step 4: Redirect to next step (creating QuestionScenes)
        return RedirectToAction("CreateQuestionScenes");
    }

    // Private helper method to populate the dropdown lists for DifficultyLevel and Accessibility
    private static void PopulateDropdowns(CreateStoryAndIntroViewModel model)
    {
        // Creating the dropdown list for choosing difficulty level
        model.DifficultyLevelOptions = Enum.GetValues(typeof(DifficultyLevel))
            .Cast<DifficultyLevel>()
            .Select(dl => new SelectListItem
            {
                Value = dl.ToString(),
                Text = dl.ToString()
            })
            .ToList();

        // Creating the dropdown list for choosing the accessibility 
        model.AccessibilityOptions = Enum.GetValues(typeof(Accessibility))
            .Cast<Accessibility>()
            .Select(a => new SelectListItem
            {
                Value = a.ToString(),
                Text = a.ToString()
            })
            .ToList();
    }





    // ======================================================================================
    //   GET and POST for creating Question Scenes for a Story
    // ======================================================================================

    /*
        This is the second of 3 views the user meets when creating a story and scenes. 
        It is the view where the user creates Question Scenes. Each Question Scene contains:
            1) Scene text (e.g. "you go into the cave")
            2) Question (e.g. "what is 2 + 2?")
            3) Four possible answers (where only one is correct)
            4) For scene texts (each respectively connected to one answer) 

            Example of answers and scene texts:
                1) Answer = "4", IsCorrect = True, SceneText = "Great, you find food, but there is something else there..."
                2) Answer = "3", IsCorrect = False, SceneText = "Oh no! A monster is in the cave..."
                3) Answer = "2", IsCorrect = False, SceneText = "Oh no! A monster with a sword is in the cave..."
                4) Answer = "5", IsCorrect = False, SceneText = "Oh no! A monster is in the cave and about to attack you!"
    */

    [HttpGet]
    public IActionResult CreateQuestionScenes()
    {
        try
        {
            // Step 1: Retrieve the in-progress story from session
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession");
            if (sessionDto == null)
            {
                TempData["ErrorMessage"] = "Your session has expired. Please start creating your story again.";
                return RedirectToAction("CreateStoryAndIntro");
            }

            // Step 2: Prepare the view model
            var viewModel = new CreateMultipleQuestionScenesViewModel
            {
                QuestionScenes = sessionDto.QuestionScenes.Count != 0
                    ? sessionDto.QuestionScenes.Select(q => new QuestionSceneBaseViewModel
                    {
                        StoryText = q.StoryText,
                        QuestionText = q.QuestionText,
                        Answers = q.Answers.Select(a => new AnswerOptionInput
                        {
                            AnswerText = a.AnswerText,
                            ContextText = a.ContextText
                        }).ToList(),
                        CorrectAnswerIndex = q.CorrectAnswerIndex
                    }).ToList()
                    : new List<QuestionSceneBaseViewModel>
                    {
                        // If none exist, start with one empty scene
                        new QuestionSceneBaseViewModel()
                    }
            };

            // Step 3: Render the view
            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryCreationController -> CreateQuestionScene:GET] Failed to initialize view model");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not load the question scene creation form. Please try again later."
            });
        }
    }

    // Helper endpoint used by AJAX in CreateQuestionScenes.cshtml
    // Returns a fresh "_QuestionScenePartial" HTML fragment when the user clicks "+ Add New Question Scene".
    // The 'index' parameter represents the numeric position of the QuestionScenes in the QuestionScenes list
    // (e.g. QuestionScenes[0], QuestionScenes[1], etc.), ensuring correct model binding when the form is posted.
    [HttpGet]
    public IActionResult GetQuestionScenePartial(int index, int questionNumber)
    {
        // Optional: Prevent creating partials if session expired
        var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession");
        if (sessionDto == null)
        {
            return BadRequest("Session expired. Please restart story creation.");
        }

        // Ensure index is non-negative (fallback safeguard)
        if (index < 0) index = 0;

        // Create a blank view model for one QuestionScene
        var viewModel = new QuestionSceneBaseViewModel();
        ViewData["QuestionNumber"] = questionNumber;

        // Set the HtmlFieldPrefix on the current ViewData.TemplateInfo
        // This makes Razor generate input names like "QuestionScenes[0].StoryText" instead of just "StoryText"
        // ensuring ASP.NET Core model binding reconstructs the list of scenes correctly on POST.
        ViewData.TemplateInfo.HtmlFieldPrefix = $"QuestionScenes[{index}]";

        // Return the partial view for one blank Question Scene form section
        return PartialView("_QuestionScenePartial", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateQuestionScenes(CreateMultipleQuestionScenesViewModel model, string action)
    {
        // 👇 Sjekk først om brukeren trykker "Back"
        if (action == "back")
        {
            // 🔹 Fjern valideringsfeil slik at ModelState ikke blokkerer
            ModelState.Clear();

            // 🔹 Lagre midlertidig alt brukeren har skrevet inn
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession")
                             ?? new StoryCreationSessionDto();

            sessionDto.QuestionScenes = model.QuestionScenes.Select(q => new QuestionSceneDto
            {
                StoryText = q.StoryText,
                QuestionText = q.QuestionText,
                Answers = q.Answers.Select(a => new AnswerOptionDto
                {
                    AnswerText = a.AnswerText,
                    ContextText = a.ContextText
                }).ToList(),
                CorrectAnswerIndex = q.CorrectAnswerIndex
            }).ToList();

            HttpContext.Session.SetObject("StoryCreationSession", sessionDto);

            // 🔹 Gå tilbake uten validering
            return RedirectToAction("CreateStoryAndIntro");
        }

        // --- Normal validering og fremover-navigasjon ---
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession")
                              ?? new StoryCreationSessionDto();

            sessionDto.QuestionScenes = model.QuestionScenes.Select(q => new QuestionSceneDto
            {
                StoryText = q.StoryText,
                QuestionText = q.QuestionText,
                Answers = q.Answers.Select(a => new AnswerOptionDto
                {
                    AnswerText = a.AnswerText,
                    ContextText = a.ContextText
                }).ToList(),
                CorrectAnswerIndex = q.CorrectAnswerIndex
            }).ToList();

            HttpContext.Session.SetObject("StoryCreationSession", sessionDto);

            return RedirectToAction("CreateEndingScenes");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryCreationController -> CreateQuestionScenes:POST] Failed to save the data to session");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not save your data. Please try again later."
            });
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteQuestionInline(CreateMultipleQuestionScenesViewModel model, int deleteIndex)
    {
        // fjern validering slik at sletting alltid virker
        ModelState.Clear();

        // sikkerhetssjekk
        if (model.QuestionScenes == null || model.QuestionScenes.Count == 0)
            return View("CreateQuestionScenes", model);

        // fjern valgt spørsmål
        if (deleteIndex >= 0 && deleteIndex < model.QuestionScenes.Count)
            model.QuestionScenes.RemoveAt(deleteIndex);

        // behold minst én tom boks så brukeren ikke mister alt
        if (model.QuestionScenes.Count == 0)
            model.QuestionScenes.Add(new QuestionSceneBaseViewModel());

        // vis siden på nytt
        return View("CreateQuestionScenes", model);
    }







    // ======================================================================================
    //   GET and POST for creating EndingScenes for a Story
    // ======================================================================================

    /*
        This is for the third and last view the user meets when creating a story and scenes. 
        This methods handle the user creating the 3 possible Ending Scenes:
            1) The good Ending
            2) The neutral Ending
            3) The bad Ending
    */

    [HttpGet]
    public IActionResult CreateEndingScenes(int? storyId = null)
    {
        try
        {
            // Step 1: Try to load existing story progress from session
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession");
            if (sessionDto == null)
            {
                TempData["ErrorMessage"] = "Your session has expired. Please start creating your story again.";
                return RedirectToAction("CreateStoryAndIntro");
            }

            // Step 2: Initialize the view model, optionally pre-filling if user navigates back
            var viewModel = new EndingScenesViewModel
            {
                StoryId = storyId ?? 0, // keep property for shared compatibility (unused here)
                GoodEnding = sessionDto.GoodEnding,
                NeutralEnding = sessionDto.NeutralEnding,
                BadEnding = sessionDto.BadEnding,
                IsEditMode = false
            };

            // Step 3: Return the view
            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryCreationController -> CreateEndingScenes:GET] Failed to initialize view model for storyId {storyId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Something went wrong",
                ErrorMessage = "We could not load the ending creation form. Please try again later."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEndingScenes(EndingScenesViewModel model, string action)
    {
        try
        {
            // 🔹 Først: Håndter "Back" uten validering
            if (action == "back")
            {
                ModelState.Clear();

                var storyCreationSessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession")
                                  ?? new StoryCreationSessionDto();

                // Lagre endings midlertidig i session
                storyCreationSessionDto.GoodEnding = model.GoodEnding;
                storyCreationSessionDto.NeutralEnding = model.NeutralEnding;
                storyCreationSessionDto.BadEnding = model.BadEnding;

                HttpContext.Session.SetObject("StoryCreationSession", storyCreationSessionDto);

                // Tilbake til QuestionScenes
                return RedirectToAction("CreateQuestionScenes");
            }

            // --- Normal validering for "Next"/"Save" ---
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[StoryCreationController -> CreateEndingScenes:POST] Model state invalid for storyId={StoryId}", model.StoryId);
                return View(model);
            }

            // Hent session-data
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession");
            if (sessionDto == null)
            {
                TempData["ErrorMessage"] = "Your session has expired. Please start creating your story again.";
                return RedirectToAction("CreateStoryAndIntro");
            }

            // Lagre endings i session her også (for sikkerhet)
            sessionDto.GoodEnding = model.GoodEnding;
            sessionDto.NeutralEnding = model.NeutralEnding;
            sessionDto.BadEnding = model.BadEnding;
            HttpContext.Session.SetObject("StoryCreationSession", sessionDto);

            // Step 3: Hent innlogget bruker
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to save a story.";
                return RedirectToAction("Login", "Account");
            }

            // Step 4: Map DTO → Entities
            var questionScenes = sessionDto.QuestionScenes.Select(q => new QuestionScene
            {
                SceneText = q.StoryText,
                Question = q.QuestionText,
                AnswerOptions = q.Answers.Select((a, i) => new AnswerOption
                {
                    Answer = a.AnswerText,
                    FeedbackText = a.ContextText,
                    IsCorrect = i == q.CorrectAnswerIndex
                }).ToList()
            }).ToList();

            // Koble QuestionScenes i rekkefølge
            for (int i = 0; i < questionScenes.Count; i++)
            {
                questionScenes[i].NextQuestionScene = i < questionScenes.Count - 1
                    ? questionScenes[i + 1]
                    : null;
            }

            // Opprett Story med relasjoner
            var story = new Story
            {
                Title = sessionDto.Title,
                Description = sessionDto.Description,
                DifficultyLevel = sessionDto.DifficultyLevel!.Value,
                Accessible = sessionDto.Accessibility!.Value,
                UserId = user.Id,
                Played = 0,
                Finished = 0,
                Failed = 0,
                Dnf = 0,
                IntroScene = new IntroScene
                {
                    IntroText = sessionDto.IntroText
                },
                QuestionScenes = questionScenes,
                EndingScenes = new List<EndingScene>
            {
                new EndingScene { EndingType = EndingType.Good, EndingText = sessionDto.GoodEnding },
                new EndingScene { EndingType = EndingType.Neutral, EndingText = sessionDto.NeutralEnding },
                new EndingScene { EndingType = EndingType.Bad, EndingText = sessionDto.BadEnding }
            }
            };

            // Generer kode hvis privat
            story.Code = sessionDto.Accessibility == Accessibility.Private
                ? await GenerateUniqueStoryCodeAsync()
                : null;

            // Lagre i databasen
            var success = await _storyRepository.AddFullStory(story);

            if (success)
            {
                HttpContext.Session.Remove("StoryCreationSession");
                TempData["SuccessMessage"] = "Your story has been successfully created!";
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Something went wrong while saving your story.";
            return RedirectToAction("CreateEndingScenes");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryCreationController -> CreateEndingScenes:POST] Failed to create endings for storyId {storyId}", model.StoryId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Could not create endings",
                ErrorMessage = "An unexpected error occurred while saving your endings. Please try again."
            });
        }
    }



    // This private method creates a code for stories that are private
    // (it is the exact same method used StoryEditingController, shoud move this to one place)
    private async Task<string> GenerateUniqueStoryCodeAsync()
    {
        string code;
        bool exists;

        // This generates an 8-char code which most likely is unique
        // However, there is no guarentee that it is actually unique, 
        // which is why I still have to check it against the database
        // Returns True if the code does exist in the db -> make new code
        // Returns False if the code doesnt exist in the db -> good to go
        do
        {
            code = Guid.NewGuid().ToString("N")[..8].ToUpper();
            exists = await _storyRepository.DoesCodeExist(code);
        }
        while (exists);

        return code;
    }
}

/*
    First version of CreateQuestioScenes after adding session:

            // Step 1: Retrieve the current session DTO
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession");
            if (sessionDto == null)
            {
                // Session expired or user jumped directly to this step
                TempData["ErrorMessage"] = "Your session has expired. Please start creating your story again.";
                return RedirectToAction("CreateStoryAndIntro");
            }

            // Step 2: Convert QuestionScenes from ViewModel to DTOs
            var questionSceneDtos = model.QuestionScenes.Select(qs => new QuestionSceneDto
            {
                StoryText = qs.StoryText,
                QuestionText = qs.QuestionText,
                CorrectAnswerIndex = qs.CorrectAnswerIndex,
                Answers = qs.Answers.Select(a => new AnswerOptionDto
                {
                    AnswerText = a.AnswerText,
                    ContextText = a.ContextText
                }).ToList()
            }).ToList();

            // Step 3: Update the session object
            sessionDto.QuestionScenes = questionSceneDtos;

            // Step 4: Save back to session
            HttpContext.Session.SetObject("StoryCreationSession", sessionDto);

            // Step 5: Redirect to the next step (creating EndingScenes)
            return RedirectToAction("CreateEndingScenes");
            
*/

/*
    [HttpPost]
    public IActionResult SavePartialStep([FromBody] StoryCreationSessionDto partialDto)
    {
        try
        {
            // Load the current session data
            var sessionDto = HttpContext.Session.GetObject<StoryCreationSessionDto>("StoryCreationSession") ?? new StoryCreationSessionDto();

            // Merge the partial updates 
            if (partialDto.QuestionScenes?.Count > 0)
                sessionDto.QuestionScenes = partialDto.QuestionScenes;

            // Save back to session
            HttpContext.Session.SetObject("StoryCreationSession", sessionDto);

            return Ok(new { success = true });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryCreationController -> SavePartialStep] Failed to save partial session data.");
            return StatusCode(500, new { success = false });
        }
    }
    */


/*
    About CreateStoryAndIntro (GET and POST)
        Currently, I am populating the dropdown lists for DifficultyLevel (easy || medium || hard) 
        and Accessibility (public || private) through a private helper method: PopulateDropdowns. 
        This is good, but later I can consider enhancing this by moving this logic out of the 
        controller and into a dedicated folder. Instead of just converting the enum types DifficulyLevel
        and Accessibility, I can modify the helper method to help controller methods convert ANY enum 
        type to a List<SelectListItem>. This is one of those “professional touches” that makes the 
        controllers maintainable, and it is easier for the future to add DropDown lists for other enums

        Step-by-step guide:
            1. Add a Helpers-folder in the root-folder with a file named EnumHelper.cs:
                namespace Jam.Helpers;

                public static class EnumHelper
                {
                    // Uses generics so it can handle any enum type.
                    // It is clean, reusable, and one-liner friendly

                    // Converts an enum type to a List of SelectListItem for dropdowns.
                    public static List<SelectListItem> ToSelectList<TEnum>() where TEnum : Enum
                    {
                        return Enum.GetValues(typeof(TEnum))
                            .Cast<TEnum>()
                            .Select(e => new SelectListItem
                            {
                                Value = e.ToString(),
                                Text = e.ToString()
                            })
                            .ToList();
                    }        
                }   

            2. Refactor StoryCreationController.CreateStoryAndIntro (GET)    
                [HttpGet]
                public IActionResult CreateStoryAndIntro()
                {
                    var model = new CreateIntroViewModel
                    {
                        DifficultyLevelOptions = EnumHelper.ToSelectList<DifficultyLevel>(),
                        AccessibilityOptions = EnumHelper.ToSelectList<Accessibility>()
                    };

                    return View(model);
                }   

            3. Also use it in the POST-version of CreateStoryAndIntro (for repopulating dropdowns)
                if (!ModelState.IsValid)
                {
                    model.DifficultyLevelOptions = EnumHelper.ToSelectList<DifficultyLevel>();
                    model.AccessibilityOptions = EnumHelper.ToSelectList<Accessibility>();
                    return View(model);
                }
*/

