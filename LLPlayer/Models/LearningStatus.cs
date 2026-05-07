namespace LLPlayer.Models;

/// <summary>
/// Learning status of an item
/// </summary>
public enum LearningStatus
{
    New,       // Just added, not reviewed yet
    Learning,  // Currently being learned
    Learned,   // Mastered
    Ignored,   // Marked as not useful
    Archived   // Hidden from main view
}
