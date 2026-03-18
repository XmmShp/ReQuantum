namespace ReQuantum.Application.Zdbk.Constants;

public static class ClassTimeTable
{
    public static readonly Dictionary<int, (TimeOnly Start, TimeOnly End)> SectionTimeMap = new()
    {
        { 1, (new TimeOnly(8, 0), new TimeOnly(8, 45)) },
        { 2, (new TimeOnly(8, 50), new TimeOnly(9, 35)) },
        { 3, (new TimeOnly(10, 0), new TimeOnly(10, 45)) },
        { 4, (new TimeOnly(10, 50), new TimeOnly(11, 35)) },
        { 5, (new TimeOnly(11, 40), new TimeOnly(12, 25)) },
        { 6, (new TimeOnly(13, 25), new TimeOnly(14, 10)) },
        { 7, (new TimeOnly(14, 15), new TimeOnly(15, 0)) },
        { 8, (new TimeOnly(15, 5), new TimeOnly(15, 50)) },
        { 9, (new TimeOnly(16, 15), new TimeOnly(17, 00)) },
        { 10, (new TimeOnly(17, 5), new TimeOnly(17, 50)) },
        { 11, (new TimeOnly(18, 50), new TimeOnly(19, 35)) },
        { 12, (new TimeOnly(19, 40), new TimeOnly(20, 25)) },
        { 13, (new TimeOnly(20, 30), new TimeOnly(21, 15)) }
    };

    public static (TimeOnly Start, TimeOnly End) GetClassTime(int startSection, int duration)
    {
        if (!SectionTimeMap.TryGetValue(startSection, out var startTime))
        {
            throw new ArgumentException($"Invalid start section: {startSection}");
        }

        var endSection = startSection + duration - 1;
        if (!SectionTimeMap.TryGetValue(endSection, out var endTime))
        {
            throw new ArgumentException($"Invalid end section: {endSection}");
        }

        return (startTime.Start, endTime.End);
    }
}
