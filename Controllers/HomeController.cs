using Jam.DAL.StoryDAL;
using Jam.Models;
using Jam.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jam.Controllers;

public class HomeController : Controller
{
    private readonly IStoryRepository _storyRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IStoryRepository storyRepository,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<HomeController> logger
    )
    {
        _storyRepository = storyRepository;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // Shows the main page for a logged-in user (GET: /Views/Home/Index.cshtml)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("[HomeController -> Index] Auth cookie invalid or user no longer exists. Redirecting to login.");
                await _signInManager.SignOutAsync();
                return LocalRedirect("/Identity/Account/Login");
            }

            // Fetch user's created stories and user's recently played stories
            var userStories = await _storyRepository.GetStoriesByUserId(user!.Id);
            var recentlyPlayed = await _storyRepository.GetMostRecentPlayedStories(user.Id, 5); // getting the last 5 played stories

            var viewModel = new HomeViewModel
            {
                FirstName = user.Firstname,
                YourGames = userStories,
                RecentlyPlayed = recentlyPlayed

            };
            var questionCounts = new Dictionary<int, int>();
            foreach (var s in userStories.Concat(recentlyPlayed))
            {
                questionCounts[s.StoryId] = await _storyRepository.GetAmountOfQuestionsForStory(s.StoryId) ?? 0;
            }
            ViewData["QuestionCounts"] = questionCounts;

            return View(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HomeController -> Index] Unexpected error while loading home page.");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Error Loading Home Page",
                ErrorMessage = "An unexpected error occurred while loading your dashboard."
            });
        }
    }
}

/*
    Consider using this in Program.cs:

        using Microsoft.AspNetCore.DataProtection;

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/Users/michaelnedgaardisaksen/.aspnet/DataProtection-Keys"))
            .SetApplicationName("JamApp");
    
    Instead of this in Index:
        if (user == null)
            {
                _logger.LogWarning("[HomeController -> Index] Auth cookie invalid or user no longer exists. Redirecting to login.");
                await _signInManager.SignOutAsync();
                return LocalRedirect("/Identity/Account/Login");
            }
    
    To ask ChatGPT later:
        How to store Data Protection keys in my own DataProtectionKeys table inside the existing database
        That is usually the cleanest solution once the app moves beyond local dev.
*/