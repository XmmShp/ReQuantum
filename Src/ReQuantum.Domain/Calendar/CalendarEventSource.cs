namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 日程事件来源
/// </summary>
public enum CalendarEventSource
{
    /// <summary>
    /// 用户手动创建
    /// </summary>
    Manual = 0,

    /// <summary>
    /// 来自教务网课程表
    /// </summary>
    Zdbk = 1,

    /// <summary>
    /// 来自教务网考试
    /// </summary>
    ZdbkExam = 2,

    /// <summary>
    /// 来自 PTA
    /// </summary>
    Pta = 3
}
