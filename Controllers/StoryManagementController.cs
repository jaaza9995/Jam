

using Jam.DAL.StoryDAL;
using Jam.Models;
using Jam.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jam.Controllers;

[Authorize]
public class StoryManagementController : Controller
{
    private readonly IStoryRepository _storyRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<StoryManagementController> _logger;

    public StoryManagementController(
        IStoryRepository storyRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<StoryManagementController> logger)
    {
        _storyRepository = storyRepository;
        _userManager = userManager;
        _logger = logger;
    }


    // ===============================================
    // Shared method DeleteStory (for admin + user)
    // ===============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStory(int storyId)
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryManagementController -> DeleteStory:POST] Invalid storyId: {storyId}", storyId);

            // Different redirects based on role
            if (isAdmin)
                return RedirectToAction("Stories", "Admin");

            return RedirectToAction("Index", "Home");
        }
        
        try
        {
            var story = await _storyRepository.GetStoryById(storyId);
            if (story == null)
            {
                _logger.LogWarning("[StoryManagementController -> DeleteStory:POST] Story not found {storyId}", storyId);
                return RedirectToAction("Index", "Home");
            }

            if (story.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("[StoryManagementController -> DeleteStory:POST] Unauthorized delete attempt for story {storyId}", storyId);
                return Forbid();
            }

            var deleted = await _storyRepository.DeleteStory(storyId);

            if (!deleted)
            {
                _logger.LogWarning("[StoryManagementController -> DeleteStory:POST] Failed to delete story {storyId}", storyId);
            }

            // Different redirects based on role
            if (isAdmin)
                return RedirectToAction("Stories", "Admin");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryManagementController -> DeleteStory:POST] Error deleting story {storyId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Error Deleting Story",
                ErrorMessage = "An unexpected error occurred while deleting the story."
            });
        }
    }


    // ===============================================
    // Shared method: StorDetails (for admin + user)
    // ===============================================
    [HttpGet]
    public async Task<IActionResult> StoryDetails(int storyId)
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        if (storyId <= 0)
        {
            _logger.LogWarning("[StoryManagementController -> StoryDetails:GET] Invalid storyId: {storyId} provided", storyId);
            
            // Different redirects based on role
            if (isAdmin)
                return RedirectToAction("Stories", "Admin");

            return RedirectToAction("Index", "Home");
        }

        try
        {
            var story = await _storyRepository.GetStoryById(storyId);
            if (story == null)
            {
                _logger.LogWarning("[StoryManagementController -> StoryDetails:GET] StoryId {storyId} not found.", storyId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "Story not found",
                    ErrorMessage = "We couldn't find the story you're trying to play. Please try again later."
                });
            }

            // Optional, to ensure user owns the story
            if (story.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("[StoryManagementController -> StoryDetails:GET] User with UserId {userId} is not the owner of Story with StoryId {storyId} not found.", userId, storyId);
                return View("Error", new ErrorViewModel
                {
                    ErrorTitle = "You are not the owner of this story",
                    ErrorMessage = "We cannot show you details about this story when you are not the owner."
                });
            }

            return View(story);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StoryManagementController -> Details:GET] Error loading story details for {storyId}", storyId);
            return View("Error", new ErrorViewModel
            {
                ErrorTitle = "Error Loading Story Details",
                ErrorMessage = "An unexpected error occurred while loading the story details."
            });
        }
    }


}