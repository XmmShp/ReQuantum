using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Calendar.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Services;
using ReQuantum.Modules.ZjuSso.Services;
using ReQuantum.ViewModels;
using ReQuantum.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LocalizedText = ReQuantum.Infrastructure.Entities.LocalizedText;

namespace ReQuantum.Modules.Calendar.Presentations;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(EventListViewModel), typeof(IEventHandler<CalendarSelectedDateChanged>)])]
public partial class EventListViewModel : ViewModelBase<EventListView>, IEventHandler<CalendarSelectedDateChanged>
{
    private readonly ICalendarService _calendarService;
    private readonly IZdbkSectionScheduleService _zdbkService;
    private readonly IZdbkExamService _examService;
    private readonly IZdbkCalendarConverter _zdbkConverter;
    private readonly IZjuSsoService _zjuSsoService;

    /// <summary>
    /// 动态标题：日程 - 日期
    /// </summary>
    public LocalizedText EventsTitle { get; }

    #region 数据集合

    [ObservableProperty]
    private ObservableCollection<CalendarEvent> _events = [];

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    #endregion

    #region 编辑状态

    [ObservableProperty]
    private string _newEventContent = string.Empty;

    public DateTime NewEventStartTime
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                CheckEventTimeWarning();
            }
        }
    } = DateTime.Now;

    // 辅助属性：用于DatePicker绑定
    public DateTimeOffset NewEventStartDate
    {
        get => new(NewEventStartTime);
        set
        {
            var time = NewEventStartTime.TimeOfDay;
            NewEventStartTime = value.DateTime.Date + time;
        }
    }

    // 辅助属性：用于TimePicker绑定
    public TimeSpan NewEventStartTimeOfDay
    {
        get => NewEventStartTime.TimeOfDay;
        set
        {
            var date = NewEventStartTime.Date;
            NewEventStartTime = date + value;
        }
    }

    public DateTime NewEventEndTime
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                CheckEventTimeWarning();
            }
        }
    } = DateTime.Now.AddHours(1);

    // 辅助属性：用于DatePicker绑定
    public DateTimeOffset NewEventEndDate
    {
        get => new(NewEventEndTime);
        set
        {
            var time = NewEventEndTime.TimeOfDay;
            NewEventEndTime = value.DateTime.Date + time;
        }
    }

    // 辅助属性：用于TimePicker绑定
    public TimeSpan NewEventEndTimeOfDay
    {
        get => NewEventEndTime.TimeOfDay;
        set
        {
            var date = NewEventEndTime.Date;
            NewEventEndTime = date + value;
        }
    }

    private void CheckEventTimeWarning()
    {
        if (!IsAddDialogOpen)
        { return; }

        // 检查结束时间是否早于开始时间
        if (NewEventEndTime <= NewEventStartTime)
        {
            WarningMessage = Localizer[nameof(UIText.EventEndTimeBeforeStart)];
        }
        // 检查是否创建过去的日程
        else if (NewEventStartTime < DateTime.Now)
        {
            WarningMessage = Localizer[nameof(UIText.EventInPast)];
        }
        else
        {
            WarningMessage = string.Empty;
        }
    }

    #endregion

    #region 对话框状态

    [ObservableProperty]
    private bool _isAddDialogOpen;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private bool _isSyncingZdbk;

    [ObservableProperty]
    private string _debugInfo = string.Empty;

    [ObservableProperty]
    private bool _showDebugInfo = false;

    #endregion

    public EventListViewModel(
        ICalendarService calendarService,
        IZdbkSectionScheduleService zdbkService,
        IZdbkExamService examService,
        IZdbkCalendarConverter zdbkConverter,
        IZjuSsoService zjuSsoService)
    {
        _calendarService = calendarService;
        _zdbkService = zdbkService;
        _examService = examService;
        _zdbkConverter = zdbkConverter;
        _zjuSsoService = zjuSsoService;
        EventsTitle = new LocalizedText();
        UpdateEventsTitle();
        LoadEvents();

        _zjuSsoService.OnLogin += OnLoginHandler;
        _zjuSsoService.OnLogout += () => OnPropertyChanged(nameof(ShowZdbkSyncButton));
    }

    private async void OnLoginHandler()
    {
        OnPropertyChanged(nameof(ShowZdbkSyncButton));
        // 登录成功后自动同步课程表和考试
        await SyncZdbkScheduleAsync();
    }

    #region 数据加载

    public void LoadEvents()
    {
        // 加载选中日期的日程（跨越该日期的所有日程）
        var events = _calendarService.GetEventsByDate(SelectedDate);
        Events = new ObservableCollection<CalendarEvent>(events);
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        UpdateEventsTitle();
        LoadEvents();
    }

    private void UpdateEventsTitle()
    {
        EventsTitle.Set(nameof(UIText.EventsOnDate), SelectedDate.ToDateTime(TimeOnly.MinValue));
    }

    #endregion

    #region 日程管理

    [RelayCommand]
    private void ShowAddDialog()
    {
        NewEventContent = string.Empty;
        WarningMessage = string.Empty;
        // 使用选中日期，时间设置为当前时间
        var now = DateTime.Now;
        NewEventStartTime = SelectedDate.ToDateTime(new TimeOnly(now.Hour, now.Minute));
        NewEventEndTime = NewEventStartTime.AddHours(1);
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    private void AddEvent()
    {
        if (string.IsNullOrWhiteSpace(NewEventContent))
        {
            return;
        }

        // 验证结束时间必须晚于开始时间
        if (NewEventEndTime <= NewEventStartTime)
        {
            return; // 警告已经实时显示，直接返回
        }

        var calendarEvent = new CalendarEvent
        {
            Content = NewEventContent.Trim(),
            StartTime = NewEventStartTime,
            EndTime = NewEventEndTime
        };

        _calendarService.AddOrUpdateEvent(calendarEvent);

        // 如果新日程的日期是选中日期，则添加到列表
        if (DateOnly.FromDateTime(calendarEvent.StartTime) == SelectedDate)
        {
            Events.Add(calendarEvent);
        }

        NewEventContent = string.Empty;
        NewEventStartTime = DateTime.Now;
        NewEventEndTime = DateTime.Now.AddHours(1);
        WarningMessage = string.Empty;
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        NewEventContent = string.Empty;
        NewEventStartTime = DateTime.Now;
        NewEventEndTime = DateTime.Now.AddHours(1);
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void DeleteEvent(CalendarEvent calendarEvent)
    {
        _calendarService.DeleteEvent(calendarEvent.Id);
        Events.Remove(calendarEvent);
    }

    [RelayCommand]
    private void UpdateEvent(CalendarEvent calendarEvent)
    {
        _calendarService.AddOrUpdateEvent(calendarEvent);
        LoadEvents();
    }

    #endregion

    #region 教务网课程表同步

    public bool ShowZdbkSyncButton => _zjuSsoService.IsAuthenticated;

    [RelayCommand]
    private async Task SyncZdbkScheduleAsync()
    {
        if (IsSyncingZdbk)
            return;
        IsSyncingZdbk = true;

        var debugLines = new List<string>();
        debugLines.Add($"===== 课表与考试同步调试信息 =====");
        debugLines.Add($"开始时间: {DateTime.Now:HH:mm:ss}");

        try
        {
            // 1. 同步课程表
            debugLines.Add("正在获取课表...");
            var scheduleResult = await _zdbkService.GetCurrentSemesterScheduleAsync();

            if (!scheduleResult.IsSuccess)
            {
                debugLines.Add($"❌ 获取课表失败: {scheduleResult.Message}");
            }
            else
            {
                var schedule = scheduleResult.Value;
                debugLines.Add($"✅ 获取课表成功");
                debugLines.Add($"学年: {schedule.AcademicYear ?? "无"}");
                debugLines.Add($"学期: {schedule.Semester ?? "无"}");
                debugLines.Add($"课程数量: {schedule.SectionList?.Count ?? 0}");

                var allNewEvents = new List<CalendarEvent>();

                // 如果有 RelatedSemesters 信息，按学期分别转换
                if (schedule.RelatedSemesters != null && schedule.RelatedSemesters.Length == 2)
                {
                    var semester1 = schedule.RelatedSemesters[0]; // 秋 或 春
                    var semester2 = schedule.RelatedSemesters[1]; // 冬 或 夏

                    debugLines.Add($"\n转换学期: {semester1}, {semester2}");

                    var events1 = await _zdbkConverter.ConvertToCalendarEventsAsync(
                        schedule.SectionList,
                        schedule.AcademicYear ?? "",
                        semester1);

                    var events2 = await _zdbkConverter.ConvertToCalendarEventsAsync(
                        schedule.SectionList,
                        schedule.AcademicYear ?? "",
                        semester2);

                    allNewEvents.AddRange(events1);
                    allNewEvents.AddRange(events2);

                    debugLines.Add($"转换后事件数: {semester1}={events1.Count}, {semester2}={events2.Count}");
                }
                else
                {
                    debugLines.Add($"\n转换学期: {schedule.Semester}");
                    allNewEvents = await _zdbkConverter.ConvertToCalendarEventsAsync(
                        schedule.SectionList,
                        schedule.AcademicYear ?? "",
                        schedule.Semester ?? "");
                    debugLines.Add($"转换后事件数: {allNewEvents.Count}");
                }

                // 标记来源
                foreach (var evt in allNewEvents)
                    evt.IsFromZdbk = true;

                // 删除旧课程，添加新课程
                var existingZdbkEvents = _calendarService.GetAllEvents().Where(e => e.IsFromZdbk).ToList();
                var newEventIds = allNewEvents.Select(e => e.Id).ToHashSet();

                debugLines.Add($"\n删除旧课程: {existingZdbkEvents.Count}");
                debugLines.Add($"添加新课程: {allNewEvents.Count}");

                foreach (var existingEvent in existingZdbkEvents.Where(e => !newEventIds.Contains(e.Id)))
                    _calendarService.DeleteEvent(existingEvent.Id);

                foreach (var evt in allNewEvents)
                    _calendarService.AddOrUpdateEvent(evt);
            }

            // 2. 同步考试信息
            debugLines.Add("\n正在获取考试信息...");
            var examsResult = await _examService.GetExamsAsync();

            if (!examsResult.IsSuccess)
            {
                debugLines.Add($"❌ 获取考试失败: {examsResult.Message}");
            }
            else
            {
                var parsedExams = examsResult.Value;
                debugLines.Add($"✅ 获取考试成功");
                debugLines.Add($"考试数量: {parsedExams.Count}");

                debugLines.Add("\n正在转换为日程事件...");
                var calendarEvents = _zdbkConverter.ConvertExamsToCalendarEvents(parsedExams);
                debugLines.Add($"转换后事件数: {calendarEvents.Count}");

                // 标记来源为考试
                foreach (var evt in calendarEvents)
                    evt.IsFromZdbkExam = true;

                // 删除旧考试，添加新考试
                var existingExamEvents = _calendarService.GetAllEvents().Where(e => e.IsFromZdbkExam).ToList();
                var newEventIds = calendarEvents.Select(e => e.Id).ToHashSet();

                debugLines.Add($"\n删除旧考试: {existingExamEvents.Count}");
                debugLines.Add($"添加新考试: {calendarEvents.Count}");

                foreach (var existingEvent in existingExamEvents.Where(e => !newEventIds.Contains(e.Id)))
                    _calendarService.DeleteEvent(existingEvent.Id);

                foreach (var evt in calendarEvents)
                    _calendarService.AddOrUpdateEvent(evt);
            }

            Publisher.Publish(new CalendarSelectedDateChanged(SelectedDate));

            debugLines.Add($"\n✅ 同步完成");
            debugLines.Add($"结束时间: {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            debugLines.Add($"\n❌ 异常: {ex.Message}");
            debugLines.Add($"堆栈: {ex.StackTrace}");
        }
        finally
        {
            IsSyncingZdbk = false;
            DebugInfo = string.Join("\n", debugLines);
            ShowDebugInfo = true;
        }
    }

    #endregion

    public void Handle(CalendarSelectedDateChanged @event)
    {
        SelectedDate = @event.Date;
    }
}
