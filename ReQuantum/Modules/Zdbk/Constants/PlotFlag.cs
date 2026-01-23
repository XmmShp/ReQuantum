namespace ReQuantum.Modules.Zdbk.Constants;

/// <summary>
/// 课程时间位标志工具类
/// 用于编码和解码课程的学期、星期、单双周和节次信息
/// </summary>
public static class PlotFlag
{
    /// <summary>
    /// 学期掩码（第17-18位）
    /// </summary>
    public const int SemesterMask = 0b0000_0000_0000_0110_0000_0000_0000_0000;

    /// <summary>
    /// 学期位偏移量
    /// </summary>
    public const int SemesterOffset = 17;

    /// <summary>
    /// 星期几掩码（第14-16位）
    /// </summary>
    public const int WeekDayMask = 0b0000_0000_0000_0001_1100_0000_0000_0000;

    /// <summary>
    /// 星期几位偏移量
    /// </summary>
    public const int WeekDayOffset = 14;

    /// <summary>
    /// 单双周掩码（第13位）
    /// </summary>
    public const int WeekTypeMask = 0b0000_0000_0000_0000_0010_0000_0000_0000;

    /// <summary>
    /// 单双周位偏移量
    /// </summary>
    public const int WeekTypeOffset = 13;

    /// <summary>
    /// 节次掩码（第0-12位，共13位，对应第1-13节课）
    /// </summary>
    public const int PlotMask = 0b0000_0000_0000_0000_0001_1111_1111_1111;

    /// <summary>
    /// 节次位偏移量
    /// </summary>
    public const int PlotOffset = 0;

    /// <summary>
    /// 解析位标志
    /// </summary>
    /// <param name="flag">位标志值</param>
    /// <returns>元组：(学期, 星期几, 单双周, 节次位图)</returns>
    public static (int Semester, int WeekDay, int WeekType, int Plots) ParsePlotFlag(int flag)
    {
        var semester = (flag & SemesterMask) >> SemesterOffset;
        var weekDay = (flag & WeekDayMask) >> WeekDayOffset;
        var weekType = (flag & WeekTypeMask) >> WeekTypeOffset;
        var plots = (flag & PlotMask) >> PlotOffset;
        return (semester, weekDay, weekType, plots);
    }

    /// <summary>
    /// 创建位标志
    /// </summary>
    /// <param name="semester">学期（0-3：秋冬春夏）</param>
    /// <param name="weekDay">星期几（1-7：周一到周日）</param>
    /// <param name="weekType">单双周（0=单周, 1=双周, 2=每周）</param>
    /// <param name="plots">节次位图（第n位为1表示第n节课）</param>
    /// <returns>编码后的位标志</returns>
    public static int CreatePlotFlag(int semester, int weekDay, int weekType, int plots)
        => semester << SemesterOffset
           | weekDay << WeekDayOffset
           | weekType << WeekTypeOffset
           | plots << PlotOffset;
}