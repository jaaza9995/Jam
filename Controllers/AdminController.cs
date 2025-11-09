
using System.Threading.Tasks;
using Jam.DAL.ApplicationUserDAL;
using Jam.DAL.PlayingSessionDAL;
using Jam.DAL.StoryDAL;
using Jam.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jam.Controllers;

[Authorize(Roles = "Admin")] // Only admins can access this controller
public class AdminController : Controller
{

    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly IStoryRepository _storyRepository;
    private readonly IPlayingSessionRepository _playingSessionRepository;

    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IStoryRepository storyRepository,
        IApplicationUserRepository userRepository,
        IPlayingSessionRepository playingSessionRepository,
        ILogger<AdminController> logger
    )
    {
        _storyRepository = storyRepository;
        _applicationUserRepository = userRepository;
        _playingSessionRepository = playingSessionRepository;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }



    //

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        try
        {
            var users = await _applicationUserRepository.GetAllApplicationUsersAsync();
            return View(users);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AdminController -> Users:GET] Unexpected error while loading users list");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Error Loading Users Page",
                ErrorMessage = "An unexpected error occurred while loading the users list."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("[AdminController -> DeleteUser:POST] No id provided for deletion");
            return RedirectToAction(nameof(Users));
        }

        try
        {
            var deleted = await _applicationUserRepository.DeleteApplicationUserAsync(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Users));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AdminController -> DeleteUser:POST] Unexpected error deleting user with id {id}", id);
            TempData["ErrorMessage"] = "An unexpected error occurred while deleting the user.";
            return RedirectToAction(nameof(Users));
        }
    }




    // 

    [HttpGet]
    public async Task<IActionResult> Stories()
    {
        try
        {
            var stories = await _storyRepository.GetAllStories();
            return View(stories);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AdminController -> Stories:GET] Unexpected error while loading stories list");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Error Loading Stories Page",
                ErrorMessage = "An unexpected error occurred while loading the users list."
            });
        }
    }





    [HttpGet]
    public async Task<IActionResult> PlayingSessions()
    {
        try
        {
            var playingSessions = await _playingSessionRepository.GetAllPlayingSessions();
            return View(playingSessions);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AdminController -> PlayingSessions:GET] Unexpected error while loading playing sessions list");
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Error Loading Playing Sessions Page",
                ErrorMessage = "An unexpected error occurred while loading the playing sessions list."
            });
        }
    }
}