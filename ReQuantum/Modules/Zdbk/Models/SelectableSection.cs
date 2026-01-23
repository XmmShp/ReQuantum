using System.Collections.Generic;
using System.Linq;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 表示某个选课页面可选的教学班
/// </summary>
public class SelectableSection : Section
{
    public int AvailableSeats { get; set; }
    public int MajorWaitingCount { get; set; }
    public int TotalWaitingCount { get; set; }
    public int Capacity { get; set; }  // 格式：余量/总容量

    public decimal SelectionProbability =>
        AvailableSeats <= 0 ? 0.00m :
            TotalWaitingCount > 0 && TotalWaitingCount > AvailableSeats ?
                decimal.Round((decimal)AvailableSeats / TotalWaitingCount, 2) :
                1.00m;

    /// <summary>
    /// 用于XAML绑定的课表时间和地点列表
    /// </summary>
    public IEnumerable<ScheduleLocationItem> ScheduleLocationItems =>
        ScheduleAndLocations.Select(x => new ScheduleLocationItem(x.Schedule, x.Location));

    public override SectionSnapshot CreateSnapshot()
    {
        return base.CreateSnapshot() with
        {
            ExtraProperties = new Dictionary<string, string>()
            {
                ["AvailableSeats"] = AvailableSeats.ToString(),
                ["MajorWaitingCount"] = MajorWaitingCount.ToString(),
                ["TotalWaitingCount"] = TotalWaitingCount.ToString(),
                ["Capacity"] = Capacity.ToString(),
                ["SelectionProbability"] = SelectionProbability.ToString("F2")
            }
        };
    }
}

/// <summary>
/// 包装课表时间和地点信息，用于XAML绑定
/// </summary>
public class ScheduleLocationItem
{
    public string Schedule { get; }
    public string Location { get; }

    public ScheduleLocationItem(string schedule, string location)
    {
        Schedule = schedule;
        Location = location;
    }
}