using ReQuantum.Contract.Calendar;
using System.Globalization;

namespace ReQuantum.UI.Pages.Calendar;

public partial class MonthCalendar
{
    private const int MonthTransitionDurationMs = 560;

    private string _monthTransitionClass = string.Empty;
    private int _monthAnimationKey;
    private bool _isMonthTransitioning;
    private bool _isLoadingMonthData;
    private bool _isDataFadingIn;
    private int _dataFadeKey;
    private double _monthTransitionFromPercent;
    private double _monthTransitionToPercent;

    private DateOnly _previousFirstDayOfMonth;
    private DateOnly _previousLastDayOfMonth;
    private List<DateOnly> _previousCalendarDays = [];
    private readonly List<DateOnly> _transitionCalendarDays = [];
    private readonly Dictionary<DateOnly, CalendarDayData> _previousDayDataMap = [];

    private async Task TransitionToMonthAsync(DateTime targetMonth)
    {
        var monthDelta = (targetMonth.Year - _currentMonth.Year) * 12 + (targetMonth.Month - _currentMonth.Month);
        _monthTransitionClass = monthDelta > 0 ? "slide-up" : "slide-down";

        _previousCalendarDays = [.. _calendarDays];
        _previousDayDataMap.Clear();
        foreach (var kvp in _dayDataMap)
        {
            _previousDayDataMap[kvp.Key] = kvp.Value;
        }

        _previousFirstDayOfMonth = _firstDayOfMonth;
        _previousLastDayOfMonth = _lastDayOfMonth;

        _transitionCalendarDays.Clear();
        if (monthDelta > 0)
        {
            var start = _previousCalendarDays.Max().AddDays(1);
            for (var i = 0; i < 42; i++)
            {
                _transitionCalendarDays.Add(start.AddDays(i));
            }
        }
        else
        {
            var start = _previousCalendarDays.Min().AddDays(-42);
            for (var i = 0; i < 42; i++)
            {
                _transitionCalendarDays.Add(start.AddDays(i));
            }
        }

        _currentMonth = targetMonth;
        GenerateCalendarDays();

        ConfigureMonthTransitionDistance(monthDelta);

        _isMonthTransitioning = true;
        _isLoadingMonthData = true;
        _monthAnimationKey++;
        await InvokeAsync(StateHasChanged);

        await Task.Delay(MonthTransitionDurationMs);
        _isMonthTransitioning = false;
        await InvokeAsync(StateHasChanged);

        await LoadMonthDataAsync();
        _isLoadingMonthData = false;

        _isDataFadingIn = true;
        _dataFadeKey++;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(180);
        _isDataFadingIn = false;
    }

    private void ConfigureMonthTransitionDistance(int monthDelta)
    {
        const double perRowTrackPercent = 50d / 6d;

        if (monthDelta > 0)
        {
            var rowsToScrollUp = GetRowIndex(_previousCalendarDays, _firstDayOfMonth) - 1;
            rowsToScrollUp = Math.Clamp(rowsToScrollUp, 0, 5);
            _monthTransitionFromPercent = 0;
            _monthTransitionToPercent = -(rowsToScrollUp * perRowTrackPercent);
            return;
        }

        var rowsToScrollDown = GetRowIndex(_calendarDays, _previousFirstDayOfMonth) - 1;
        rowsToScrollDown = Math.Clamp(rowsToScrollDown, 0, 5);
        _monthTransitionFromPercent = -50;
        _monthTransitionToPercent = _monthTransitionFromPercent + (rowsToScrollDown * perRowTrackPercent);
    }

    private static int GetRowIndex(List<DateOnly> days, DateOnly date)
    {
        var index = days.IndexOf(date);
        if (index < 0)
        {
            return 6;
        }

        return (index / 7) + 1;
    }

    private string GetDaysTrackStyle()
    {
        var from = _monthTransitionFromPercent.ToString("0.###", CultureInfo.InvariantCulture);
        var to = _monthTransitionToPercent.ToString("0.###", CultureInfo.InvariantCulture);
        return $"--from-y:{from}%;--to-y:{to}%;";
    }
}
