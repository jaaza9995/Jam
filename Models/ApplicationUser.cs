using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Jam.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    public string Firstname { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Lastname { get; set; } = string.Empty;

    public List<Story> Stories { get; set; } = new(); // Navigation property
    public List<PlayingSession> PlayingSessions { get; set; } = new(); // Navigation property
}