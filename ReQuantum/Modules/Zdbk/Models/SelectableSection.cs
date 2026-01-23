using System.Collections.Generic;

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