
using Jam.Models;

namespace Jam.DAL.ApplicationUserDAL;

public interface IApplicationUserRepository
{
    Task<IEnumerable<ApplicationUser>> GetAllApplicationUsersAsync();
    Task<ApplicationUser?> GetApplicationUserByIdAsync(string applicationUserId);
    Task<IEnumerable<ApplicationUser>> GetApplicationUsersInRoleAsync(string roleName);
    Task<bool> DeleteApplicationUserAsync(string applicationUserId);
}