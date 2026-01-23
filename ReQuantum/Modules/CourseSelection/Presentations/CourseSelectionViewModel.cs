using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Material;
using Microsoft.Extensions.Logging;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
using ReQuantum.Modules.Zdbk.Constants;
using ReQuantum.Modules.Zdbk.Enums;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.Zdbk.Services;
using ReQuantum.Modules.ZjuSso.Services;
using ReQuantum.Services;
using ReQuantum.Views;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(CourseSelectionViewModel), typeof(IMenuItemProvider)])]
public partial class CourseSelectionViewModel : ViewModelBase<CourseSelectionView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public MenuItem MenuItem { get; }
    public uint Order => 0;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ViewModelType => typeof(CourseSelectionViewModel);
    #endregion

    private readonly IZdbkCourseService _courseService;
    private readonly IZdbkGraduationService _graduationService;
    private readonly IZdbkSessionService _sessionService;
    private readonly ILogger<CourseSelectionViewModel> _logger;

    // 登录状态
    [ObservableProperty]
    private bool _isLoggedIn;

    // 课程分类列表
    public ObservableCollection<CourseCategoryItem> CourseCategories { get; } = new();

    // 选中的课程分类
    [ObservableProperty]
    private CourseCategoryItem? _selectedCategory;

    // 可选课程列表
    [ObservableProperty]
    private ObservableCollection<SelectableCourse> _availableCourses = new();

    // 选中的课程
    [ObservableProperty]
    private SelectableCourse? _selectedCourse;

    // 教学班列表
    [ObservableProperty]
    private ObservableCollection<SelectableSection> _sections = new();

    // 加载状态
    [ObservableProperty]
    private bool _isLoadingCourses;

    [ObservableProperty]
    private bool _isLoadingSections;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // 分页
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    // 已选课程统计
    [ObservableProperty]
    private int _selectedCoursesCount;

    [ObservableProperty]
    private decimal _selectedCredits;

    // 毕业要求统计
    [ObservableProperty]
    private int _totalRequiredCourses;

    [ObservableProperty]
    private int _completedCourses;

    [ObservableProperty]
    private decimal _completedCredits;

    // 调试信息
    [ObservableProperty]
    private string _debugInfo = string.Empty;

    private readonly IZjuSsoService _zjuSsoService;

    public CourseSelectionViewModel(
        IZdbkCourseService courseService,
        IZdbkGraduationService graduationService,
        IZdbkSessionService sessionService,
        IZjuSsoService zjuSsoService,
        ILogger<CourseSelectionViewModel> logger)
    {
        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.CourseSelection) },
            IconKind = PackIconMaterialKind.BookOpenPageVariant,
            OnSelected = () => Navigator.NavigateTo<CourseSelectionViewModel>()
        };

        _courseService = courseService;
        _graduationService = graduationService;
        _sessionService = sessionService;
        _zjuSsoService = zjuSsoService;
        _logger = logger;

        // 订阅 ZJU SSO 登录状态变化
        _zjuSsoService.OnLogin += HandleLogin;
        _zjuSsoService.OnLogout += HandleLogout;

        // 检查登录状态
        UpdateLoginStatus();

        // 如果已登录，初始化数据
        if (IsLoggedIn)
        {
            InitializeCourseCategories();
            LoadStatistics();
        }
    }

    private void HandleLogin()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateLoginStatus();
            InitializeCourseCategories();
            LoadStatistics();
            StatusMessage = "登录成功";
            UpdateDebugInfo();
        });
    }

    private void HandleLogout()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateLoginStatus();
            CourseCategories.Clear();
            AvailableCourses.Clear();
            Sections.Clear();
            StatusMessage = "已登出";
            UpdateDebugInfo();
        });
    }

    private void UpdateLoginStatus()
    {
        IsLoggedIn = _zjuSsoService.IsAuthenticated;
        UpdateDebugInfo();
    }

    private void UpdateDebugInfo()
    {
        var zjuSsoStatus = _zjuSsoService.IsAuthenticated ? "已登录" : "未登录";
        var zjuSsoId = _zjuSsoService.Id ?? "无";
        var zdbkState = _sessionService.State != null ? "存在" : "不存在";
        var zdbkStudentId = _sessionService.State?.StudentId ?? "无";
        var zdbkStudentName = _sessionService.State?.StudentName ?? "无";
        var categoriesCount = CourseCategories.Count;
        var selectedCategory = SelectedCategory?.DisplayName ?? "未选择";

        DebugInfo = $"ZJU SSO: {zjuSsoStatus} (ID: {zjuSsoId})\n" +
                    $"ZDBK State: {zdbkState} (学号: {zdbkStudentId}, 姓名: {zdbkStudentName})\n" +
                    $"课程分类数: {categoriesCount}, 当前选择: {selectedCategory}\n" +
                    $"可选课程数: {AvailableCourses.Count}, 教学班数: {Sections.Count}";
    }

    private void InitializeCourseCategories()
    {
        CourseCategories.Clear();
        foreach (var category in Modules.Zdbk.Constants.CourseCategories.All)
        {
            CourseCategories.Add(new CourseCategoryItem
            {
                Category = category.Id,
                DisplayName = category.Name
            });
        }

        if (CourseCategories.Count > 0)
        {
            SelectedCategory = CourseCategories[0];
        }
    }

    partial void OnSelectedCategoryChanged(CourseCategoryItem? value)
    {
        if (value != null)
        {
            _ = LoadCoursesAsync();
        }
        UpdateDebugInfo();
    }

    partial void OnSelectedCourseChanged(SelectableCourse? value)
    {
        if (value != null)
        {
            _ = LoadSectionsAsync();
        }
        UpdateDebugInfo();
    }

    [RelayCommand]
    private async Task LoadCoursesAsync()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "请选择课程分类";
            return;
        }

        IsLoadingCourses = true;
        StatusMessage = "正在加载课程列表...";

        try
        {
            var startPage = (CurrentPage - 1) * PageSize + 1;
            var endPage = CurrentPage * PageSize;

            var result = await _courseService.GetAvailableCoursesAsync(
                SelectedCategory.Category,
                startPage,
                endPage);

            if (result.IsSuccess)
            {
                AvailableCourses.Clear();
                foreach (var course in result.Value.OrderBy(c => c.Code))
                {
                    AvailableCourses.Add(course);
                }

                StatusMessage = $"成功加载 {AvailableCourses.Count} 门课程";
            }
            else
            {
                StatusMessage = $"加载失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载出错: {ex.Message}";
            _logger.LogError(ex, "加载课程异常");
        }
        finally
        {
            IsLoadingCourses = false;
            UpdateDebugInfo();
        }
    }

    [RelayCommand]
    private async Task LoadSectionsAsync()
    {
        if (SelectedCourse == null)
        {
            return;
        }

        IsLoadingSections = true;
        StatusMessage = "正在加载教学班信息...";

        try
        {
            var result = await _courseService.UpdateSectionsAsync(SelectedCourse);

            if (result.IsSuccess)
            {
                Sections.Clear();
                foreach (var section in SelectedCourse.Sections.OrderByDescending(s => s.AvailableSeats))
                {
                    Sections.Add(section);
                }

                StatusMessage = $"找到 {Sections.Count} 个教学班";
            }
            else
            {
                StatusMessage = $"加载教学班失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载教学班出错: {ex.Message}";
            _logger.LogError(ex, "加载教学班异常");
        }
        finally
        {
            IsLoadingSections = false;
        }
    }

    [RelayCommand]
    private async Task RefreshSelectedCoursesAsync()
    {
        StatusMessage = "正在刷新已选课程...";

        try
        {
            var result = await _courseService.RefreshSelectedSectionsAsync();

            if (result.IsSuccess)
            {
                LoadStatistics();
                StatusMessage = "已选课程刷新成功";
            }
            else
            {
                StatusMessage = $"刷新失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新出错: {ex.Message}";
            _logger.LogError(ex, "刷新已选课程异常");
        }
    }

    [RelayCommand]
    private async Task RefreshGraduationRequirementsAsync()
    {
        StatusMessage = "正在刷新毕业要求...";

        try
        {
            var result = await _graduationService.RefreshGraduationRequirementsAsync();

            if (result.IsSuccess)
            {
                LoadStatistics();
                StatusMessage = "毕业要求刷新成功";
            }
            else
            {
                StatusMessage = $"刷新失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新出错: {ex.Message}";
            _logger.LogError(ex, "刷新毕业要求异常");
        }
    }

    private void LoadStatistics()
    {
        var selectedSections = _courseService.SelectedSections;
        if (selectedSections != null)
        {
            SelectedCoursesCount = selectedSections.Count;
            SelectedCredits = selectedSections.Sum(s => s.CourseCredits);
        }

        var requirements = _graduationService.GraduationRequirements;
        if (requirements != null)
        {
            TotalRequiredCourses = requirements.Count;
            CompletedCourses = requirements.Count(c => c.Status == CourseStatus.Passed);
            CompletedCredits = requirements
                .Where(c => c.Status == CourseStatus.Passed)
                .Sum(c => c.Credits);
        }

        UpdateDebugInfo();
    }

    [RelayCommand]
    private void NextPage()
    {
        CurrentPage++;
        _ = LoadCoursesAsync();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            _ = LoadCoursesAsync();
        }
    }

    [RelayCommand]
    private void RefreshLoginStatus()
    {
        UpdateLoginStatus();

        if (IsLoggedIn)
        {
            if (CourseCategories.Count == 0)
            {
                InitializeCourseCategories();
            }
            LoadStatistics();
            StatusMessage = "登录状态已刷新";
        }
        else
        {
            StatusMessage = "未登录";
        }
    }
}

public class CourseCategoryItem
{
    public CourseCategory Category { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
