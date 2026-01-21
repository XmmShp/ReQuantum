using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Calendar.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Services;
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
    private readonly IPtaProblemSetService _ptaService;
    private readonly IPtaCalendarConvertService _ptaConverter;
    private readonly IPtaAuthService _ptaAuthService;

//ddd
    private bool _isRepeating;
	public bool IsRepeating
	{
		get => _isRepeating;
		set => SetProperty(ref _isRepeating, value);
	}

	private int _repeatWeeks = 1; // 默认重复1周
	public int RepeatWeeks
	{
		get => _repeatWeeks;
		set => SetProperty(ref _repeatWeeks, value);
	}
	public List<int> RepeatOptions => new() { 1, 2, 4, 8, 12, 16 };

	//ddd
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
    private bool _isSyncingAll;

    [ObservableProperty]
    private string _syncErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _showSyncError;

    #endregion

    public EventListViewModel(
        ICalendarService calendarService,
        IZdbkSectionScheduleService zdbkService,
        IZdbkExamService examService,
        IZdbkCalendarConverter zdbkConverter,
        IZjuSsoService zjuSsoService,
        IPtaProblemSetService ptaService,
        IPtaCalendarConvertService ptaConverter,
        IPtaAuthService ptaAuthService)
    {
        _calendarService = calendarService;
        _zdbkService = zdbkService;
        _examService = examService;
        _zdbkConverter = zdbkConverter;
        _zjuSsoService = zjuSsoService;
        _ptaService = ptaService;
        _ptaConverter = ptaConverter;
        _ptaAuthService = ptaAuthService;
        EventsTitle = new LocalizedText();
        UpdateEventsTitle();
        LoadEvents();

        _zjuSsoService.OnLogin += OnZjuSsoLoginHandler;
        _zjuSsoService.OnLogout += () => OnPropertyChanged(nameof(ShowZdbkSyncButton));
        _ptaAuthService.OnLogin += OnPtaLoginHandler;
        _ptaAuthService.OnLogout += () => OnPropertyChanged(nameof(ShowPtaSyncButton));
    }

    private async void OnZjuSsoLoginHandler()
    {
        OnPropertyChanged(nameof(ShowZdbkSyncButton));
        // 登录成功后自动同步课程表和考试
        await SyncZdbkScheduleAsync();
    }

    private async void OnPtaLoginHandler()
    {
        OnPropertyChanged(nameof(ShowPtaSyncButton));
        // 登录成功后自动同步 PTA 习题集
        await SyncPtaAsync();
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
        //dd
        IsRepeating = false;
        //dd
        IsAddDialogOpen = true;
    }
	//ddd
	[RelayCommand]
	private void AddEvent()
	{
		if (string.IsNullOrWhiteSpace(NewEventContent))
		{
			return;
		}

		if (NewEventEndTime <= NewEventStartTime)
		{
			return;
		}

		// 如果不是重复事件，只添加一次
		if (!IsRepeating)
		{
			var calendarEvent = new CalendarEvent
			{
				Content = NewEventContent.Trim(),
				StartTime = NewEventStartTime,
				EndTime = NewEventEndTime
			};

			_calendarService.AddOrUpdateEvent(calendarEvent);

			if (DateOnly.FromDateTime(calendarEvent.StartTime) == SelectedDate)
			{
				Events.Add(calendarEvent);
			}
		}
		else
		{
			// 如果是重复事件，添加接下来几周的相同日程
			var startDate = NewEventStartTime;
			var endDate = NewEventEndTime;
			var content = NewEventContent.Trim();

			for (int i = 0; i < RepeatWeeks; i++) // 创建未来几周的重复事件
			{
				var occurrenceDate = startDate.AddDays(i * 7); // 每次加 7 天
				var calendarEvent = new CalendarEvent
				{
					Content = content,
					StartTime = occurrenceDate,
					EndTime = endDate.AddDays(i * 7)
				};

				_calendarService.AddOrUpdateEvent(calendarEvent);

				// 如果这次的日程正好是当前查看的日期，就显示在列表中
				if (DateOnly.FromDateTime(occurrenceDate) == SelectedDate)
				{
					Events.Add(calendarEvent);
				}
			}
		}

		// 清空并关闭弹窗
		NewEventContent = string.Empty;
		NewEventStartTime = DateTime.Now;
		NewEventEndTime = DateTime.Now.AddHours(1);
		IsRepeating = false; // 重置重复选项
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

        try
        {
            // 1. 同步课程表
            var scheduleResult = await _zdbkService.GetCurrentSemesterScheduleAsync();

            if (scheduleResult.IsSuccess)
            {
                var schedule = scheduleResult.Value;
                var allNewEvents = new List<CalendarEvent>();

                // 如果有 RelatedSemesters 信息，按学期分别转换
                if (schedule.RelatedSemesters != null && schedule.RelatedSemesters.Length == 2)
                {
                    var semester1 = schedule.RelatedSemesters[0]; // 秋 或 春
                    var semester2 = schedule.RelatedSemesters[1]; // 冬 或 夏

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
                }
                else
                {
                    allNewEvents = await _zdbkConverter.ConvertToCalendarEventsAsync(
                        schedule.SectionList,
                        schedule.AcademicYear ?? "",
                        schedule.Semester ?? "");
                }

                // 标记来源
                foreach (var evt in allNewEvents)
                    evt.IsFromZdbk = true;

                // 删除旧课程，添加新课程
                var existingZdbkEvents = _calendarService.GetAllEvents().Where(e => e.IsFromZdbk).ToList();
                var newEventIds = allNewEvents.Select(e => e.Id).ToHashSet();

                foreach (var existingEvent in existingZdbkEvents.Where(e => !newEventIds.Contains(e.Id)))
                    _calendarService.DeleteEvent(existingEvent.Id);

                foreach (var evt in allNewEvents)
                    _calendarService.AddOrUpdateEvent(evt);
            }

            // 2. 同步考试信息
            var examsResult = await _examService.GetExamsAsync();

            if (examsResult.IsSuccess)
            {
                var parsedExams = examsResult.Value;
                var calendarEvents = _zdbkConverter.ConvertExamsToCalendarEvents(parsedExams);

                // 标记来源为考试
                foreach (var evt in calendarEvents)
                    evt.IsFromZdbkExam = true;

                // 删除旧考试，添加新考试
                var existingExamEvents = _calendarService.GetAllEvents().Where(e => e.IsFromZdbkExam).ToList();
                var newEventIds = calendarEvents.Select(e => e.Id).ToHashSet();

                foreach (var existingEvent in existingExamEvents.Where(e => !newEventIds.Contains(e.Id)))
                    _calendarService.DeleteEvent(existingEvent.Id);

                foreach (var evt in calendarEvents)
                    _calendarService.AddOrUpdateEvent(evt);
            }

            Publisher.Publish(new CalendarSelectedDateChanged(SelectedDate));
        }
        finally
        {
            IsSyncingZdbk = false;
        }
    }

    #endregion

    #region PTA 习题集同步

    public bool ShowPtaSyncButton => _ptaAuthService.IsAuthenticated;

    [ObservableProperty]
    private bool _isSyncingPta;

    [RelayCommand]
    private async Task SyncPtaAsync()
    {
        if (IsSyncingPta)
            return;
        IsSyncingPta = true;

        try
        {
            var problemSetsResult = await _ptaService.GetProblemSetsAsync();

            if (problemSetsResult.IsSuccess)
            {
                var problemSets = problemSetsResult.Value;
                var calendarEvents = _ptaConverter.ConvertToCalendarEvents(problemSets);

                // 标记来源为 PTA
                foreach (var evt in calendarEvents)
                    evt.IsFromPta = true;

                // 删除旧的 PTA 事件，添加新事件
                var existingPtaEvents = _calendarService.GetAllEvents().Where(e => e.IsFromPta).ToList();
                var newEventIds = calendarEvents.Select(e => e.Id).ToHashSet();

                foreach (var existingEvent in existingPtaEvents.Where(e => !newEventIds.Contains(e.Id)))
                    _calendarService.DeleteEvent(existingEvent.Id);

                foreach (var evt in calendarEvents)
                    _calendarService.AddOrUpdateEvent(evt);
            }

            Publisher.Publish(new CalendarSelectedDateChanged(SelectedDate));
        }
        finally
        {
            IsSyncingPta = false;
        }
    }

    #endregion

    #region 一键同步

    public bool ShowSyncAllButton => _zjuSsoService.IsAuthenticated || _ptaAuthService.IsAuthenticated;

    [RelayCommand]
    private async Task SyncAllAsync()
    {
        if (IsSyncingAll)
            return;

        IsSyncingAll = true;
        ShowSyncError = false;
        SyncErrorMessage = string.Empty;
        var errors = new List<string>();

        try
        {
            // 1. 同步课程表和考试（如果已登录教务网）
            if (_zjuSsoService.IsAuthenticated)
            {
                try
                {
                    IsSyncingZdbk = true;

                    // 同步课程表
                    var scheduleResult = await _zdbkService.GetCurrentSemesterScheduleAsync();
                    if (!scheduleResult.IsSuccess)
                    {
                        errors.Add($"📅 {Localizer[nameof(UIText.CourseSyncFailed)]}: {scheduleResult.Message}");
                    }
                    else
                    {
                        var schedule = scheduleResult.Value;
                        var allNewEvents = new List<CalendarEvent>();

                        if (schedule.RelatedSemesters != null && schedule.RelatedSemesters.Length == 2)
                        {
                            var semester1 = schedule.RelatedSemesters[0];
                            var semester2 = schedule.RelatedSemesters[1];

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
                        }
                        else
                        {
                            allNewEvents = await _zdbkConverter.ConvertToCalendarEventsAsync(
                                schedule.SectionList,
                                schedule.AcademicYear ?? "",
                                schedule.Semester ?? "");
                        }

                        foreach (var evt in allNewEvents)
                            evt.IsFromZdbk = true;

                        var existingZdbkEvents = _calendarService.GetAllEvents().Where(e => e.IsFromZdbk).ToList();
                        var newEventIds = allNewEvents.Select(e => e.Id).ToHashSet();

                        foreach (var existingEvent in existingZdbkEvents.Where(e => !newEventIds.Contains(e.Id)))
                            _calendarService.DeleteEvent(existingEvent.Id);

                        foreach (var evt in allNewEvents)
                            _calendarService.AddOrUpdateEvent(evt);
                    }

                    // 同步考试
                    var examsResult = await _examService.GetExamsAsync();
                    if (!examsResult.IsSuccess)
                    {
                        errors.Add($"📝 {Localizer[nameof(UIText.ExamSyncFailed)]}: {examsResult.Message}");
                    }
                    else
                    {
                        var parsedExams = examsResult.Value;
                        var calendarEvents = _zdbkConverter.ConvertExamsToCalendarEvents(parsedExams);

                        foreach (var evt in calendarEvents)
                            evt.IsFromZdbkExam = true;

                        var existingExamEvents = _calendarService.GetAllEvents().Where(e => e.IsFromZdbkExam).ToList();
                        var newEventIds = calendarEvents.Select(e => e.Id).ToHashSet();

                        foreach (var existingEvent in existingExamEvents.Where(e => !newEventIds.Contains(e.Id)))
                            _calendarService.DeleteEvent(existingEvent.Id);

                        foreach (var evt in calendarEvents)
                            _calendarService.AddOrUpdateEvent(evt);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"📅 {Localizer[nameof(UIText.ZdbkSyncException)]}: {ex.Message}");
                }
                finally
                {
                    IsSyncingZdbk = false;
                }
            }

            // 2. 同步 PTA 作业（如果已登录 PTA）
            if (_ptaAuthService.IsAuthenticated)
            {
                try
                {
                    IsSyncingPta = true;

                    var problemSetsResult = await _ptaService.GetProblemSetsAsync();
                    if (!problemSetsResult.IsSuccess)
                    {
                        errors.Add($"📚 {Localizer[nameof(UIText.PtaSyncFailed)]}: {problemSetsResult.Message}");
                    }
                    else
                    {
                        var problemSets = problemSetsResult.Value;
                        var calendarEvents = _ptaConverter.ConvertToCalendarEvents(problemSets);

                        foreach (var evt in calendarEvents)
                            evt.IsFromPta = true;

                        var existingPtaEvents = _calendarService.GetAllEvents().Where(e => e.IsFromPta).ToList();
                        var newEventIds = calendarEvents.Select(e => e.Id).ToHashSet();

                        foreach (var existingEvent in existingPtaEvents.Where(e => !newEventIds.Contains(e.Id)))
                            _calendarService.DeleteEvent(existingEvent.Id);

                        foreach (var evt in calendarEvents)
                            _calendarService.AddOrUpdateEvent(evt);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"📚 {Localizer[nameof(UIText.PtaSyncException)]}: {ex.Message}");
                }
                finally
                {
                    IsSyncingPta = false;
                }
            }

            // 3. 刷新日历显示
            Publisher.Publish(new CalendarSelectedDateChanged(SelectedDate));

            // 4. 显示错误信息（如果有）
            if (errors.Count > 0)
            {
                SyncErrorMessage = string.Join("\n\n", errors);
                ShowSyncError = true;
            }
        }
        finally
        {
            IsSyncingAll = false;
        }
    }

    [RelayCommand]
    private void CloseSyncError()
    {
        ShowSyncError = false;
        SyncErrorMessage = string.Empty;
    }

    #endregion

    public void Handle(CalendarSelectedDateChanged @event)
    {
        SelectedDate = @event.Date;
    }
}
