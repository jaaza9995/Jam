
namespace Jam.ViewModels;

public class ErrorViewModel
{
    public string ErrorTitle { get; set; } = "An unexpected error occurred.";
    public string? ErrorMessage { get; set; }

    // Optional: where the user should be sent after clicking the button
    public string? ReturnAction { get; set; } = "Index";
    public string? ReturnController { get; set; } = "Home";

    // Optional: custom label for the button
    public string ReturnButtonText { get; set; } = "Return Home";
}