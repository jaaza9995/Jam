using Microsoft.EntityFrameworkCore;
using Jam.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Jam.DAL.ApplicationUserDAL;

// Consider adding AsNoTracking where appropriate for read-only queries
// to improve performance by disabling change tracking 

public class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<ApplicationUserRepository> _logger;

    public ApplicationUserRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<ApplicationUserRepository> logger
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllApplicationUsersAsync()
    {
        try
        {
            return await _userManager.Users.ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ApplicationUserRepository -> GetAllApplicationUsersAsync] Error retrieving all ApplicationUsers");
            return Enumerable.Empty<ApplicationUser>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<ApplicationUser?> GetApplicationUserByIdAsync(string applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            _logger.LogWarning("[ApplicationUserRepository -> GetApplicationUserByIdAsync] Invalid applicationUserId {applicationUserId} provided", applicationUserId);
            return null;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(applicationUserId);
            if (user == null)
            {
                _logger.LogWarning("[ApplicationUserRepository -> GetApplicationUserByIdAsync] No ApplicationUser found with applicationUserId {applicationUserId}", applicationUserId);
            }

            return user;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ApplicationUserRepository -> GetApplicationUserByIdAsync] Error retrieving ApplicationUser with id {applicationUserId}", applicationUserId);
            return null;
        }
    }

    public async Task<IEnumerable<ApplicationUser>> GetApplicationUsersInRoleAsync(string roleName)
    {
        try
        {
            return await _userManager.GetUsersInRoleAsync(roleName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ApplicationUserRepository -> GetApplicationUsersInRoleAsync] Error retrieving ApplicationUser with role {roleName}", roleName);
            return Enumerable.Empty<ApplicationUser>(); // not returning null to avoid null reference exceptions
        }
    }

    public async Task<bool> DeleteApplicationUserAsync(string applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            _logger.LogWarning("[ApplicationUserRepository -> GetApplicationUsersInRoleAsync] Invalid applicationUserId {applicationUserId} provided", applicationUserId);
            return false;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(applicationUserId);
            if (user == null)
            {
                _logger.LogWarning("[ApplicationUserRepository -> DeleteApplicationUserAsync] No ApplicationUser found with id {applicationUserId} to delete", applicationUserId);
                return false;
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errorDescriptions = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("[ApplicationUserRepository -> DeleteApplicationUserAsync] Failed to delete user {applicationUserId}. Errors: {errors}", applicationUserId, errorDescriptions);
            }
            else
            {
                _logger.LogInformation("[UserReApplicationUserRepositorypository -> DeleteApplicationUserAsync] Successfully deleted ApplicationUser with id {applicationUserId}", applicationUserId);
            }

            return result.Succeeded;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ApplicationUserRepository -> DeleteApplicationUserAsync] Error deleting ApplicationUser with id {applicationUserId}", applicationUserId);
            return false;
        }
    }
}