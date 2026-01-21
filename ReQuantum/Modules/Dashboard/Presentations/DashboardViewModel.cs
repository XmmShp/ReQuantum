using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Material;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Calendar.Presentations;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
using ReQuantum.Services;
using ReQuantum.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(DashboardViewModel), typeof(IMenuItemProvider)])]
public partial class DashboardViewModel : ViewModelBase<DashboardView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public MenuItem MenuItem { get; }
    public uint Order => 0;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ViewModelType => typeof(DashboardViewModel);
    #endregion

    private readonly ILocalizer _localizer;
    private readonly EventListViewModel _eventListViewModel;
    private readonly TodoListViewModel _todoListViewModel;
    private readonly NoteListViewModel _noteListViewModel;

    private readonly DispatcherTimer _refreshTimer;
    private DateTime _lastUpdateTime;

    public string Welcome => _localizer[UIText.HelloWorld];
    public string aSentence => "This is an attempt";

    [ObservableProperty]
    private bool _isClick;

    public DashboardViewModel(ILocalizer localizer,
        EventListViewModel eventListViewModel,
        TodoListViewModel todoListViewModel,
        NoteListViewModel noteListViewModel)
    {
        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.Home) },
            IconKind = PackIconMaterialKind.HomeOutline,
            OnSelected = () => Navigator.NavigateTo<DashboardViewModel>()
        };
        _localizer = localizer;

        // 保存三个 ViewModel
        _eventListViewModel = eventListViewModel;
        _todoListViewModel = todoListViewModel;
        _noteListViewModel = noteListViewModel;

        // 加载最近任务
        LoadRecentItems();
        // 创建并启动定时器：每5秒刷新一次
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5), // 可改为 3 或 10 秒
            IsEnabled = true
        };

        // 绑定事件：每次触发时重新加载数据
        _refreshTimer.Tick += (sender, e) =>
        {
            LoadRecentItems(); // 重新加载三个模块的数据
            _lastUpdateTime = DateTime.Now;
            OnPropertyChanged(nameof(LastUpdateTimeString)); // 更新界面显示的时间
        };
    }

    [RelayCommand]
    private void UpdateWelcome()
    {
        _localizer.SetCulture("zh-CN");
    }

    #region Recent Items Display Logic

    // 供界面绑定的精选列表
    public ObservableCollection<CalendarEvent> RecentEvents { get; private set; }
    public ObservableCollection<CalendarTodo> RecentTodos { get; private set; }
    public ObservableCollection<CalendarNote> RecentNotes { get; private set; }

    /// <summary>
    /// 加载每个模块中“最近的5项”
    /// </summary>
    private void LoadRecentItems()
    {
        var now = DateTime.Now;

        // 日程：从今天开始的日程，按开始时间升序
        RecentEvents = new ObservableCollection<CalendarEvent>(
            _eventListViewModel.Events
                .Where(e => e.StartTime.Date >= now.Date)
                .OrderBy(e => e.StartTime)
                .Take(5)
        );

        // 待办：未完成 + 截止时间有效，按截止时间升序
        RecentTodos = new ObservableCollection<CalendarTodo>(
            _todoListViewModel.Todos
                .Where(t => !t.IsCompleted && t.DueTime.Year >= 2020)
                .OrderBy(t => t.DueTime)
                .Take(5)
        );

        // 便签：按创建时间倒序，最新在前
        RecentNotes = new ObservableCollection<CalendarNote>(
            _noteListViewModel.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
        );
    }

    #endregion
    private void OnTimerTick(object? sender, EventArgs e)
    {
        LoadRecentItems();
        _lastUpdateTime = DateTime.Now;
        OnPropertyChanged(nameof(LastUpdateTimeString));
    }

    /// <summary>
    /// 供界面显示“上次更新时间”
    /// </summary>
    public string LastUpdateTimeString =>
        _lastUpdateTime == default
            ? "等待刷新..."
            : $"上次更新：{_lastUpdateTime:HH:mm:ss}";
}
