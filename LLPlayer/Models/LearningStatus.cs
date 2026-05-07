namespace LLPlayer.Models;

/// <summary>
/// Статус изучения элемента.
/// </summary>
public enum LearningStatus
{
    New,        // только добавлено
    Learning,   // в процессе изучения
    Learned,    // выучено
    Ignored,    // пропустить (не изучать)
    Archived    // убрано из основного списка
}
