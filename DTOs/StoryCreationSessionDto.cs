
using Jam.Models.Enums;

namespace Jam.DTOs;

public class StoryCreationSessionDto
{
    // Step 1: Story + IntroScene
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DifficultyLevel? DifficultyLevel { get; set; }
    public Accessibility? Accessibility { get; set; }
    public string IntroText { get; set; } = string.Empty;

    // Step 2: QuestionScenes
    public List<QuestionSceneDto> QuestionScenes { get; set; } = new();

    // Step 3: EndingScenes
    public string GoodEnding { get; set; } = string.Empty;
    public string NeutralEnding { get; set; } = string.Empty;
    public string BadEnding { get; set; } = string.Empty;
}

// Each QuestionScene has its own mini-DTO
public class QuestionSceneDto
{
    public string StoryText { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<AnswerOptionDto> Answers { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
}

public class AnswerOptionDto
{
    public string AnswerText { get; set; } = string.Empty;
    public string ContextText { get; set; } = string.Empty;
}