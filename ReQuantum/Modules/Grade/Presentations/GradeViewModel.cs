using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Material;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.Zdbk.Services;
using ReQuantum.Modules.ZjuSso.Services;
using ReQuantum.Services;
using ReQuantum.Views;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(GradeViewModel), typeof(IMenuItemProvider)])]
public partial class GradeViewModel : ViewModelBase<GradeView>, IMenuItemProvider
{
    #region MenuItemProvider APIs

    public MenuItem MenuItem { get; }
    public uint Order => 0;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ViewModelType => typeof(GradeViewModel);

    #endregion

    // 登录状态
    [ObservableProperty] private bool _isLoggedIn;

    private readonly ILocalizer _localizer;
    private readonly IZdbkGradeService _zdbkGradeService;
    private readonly IZdbkSessionService _sessionService;
    private readonly IZjuSsoService _zjuSsoService;

    // 1. 自动生成 Grades 属性 (对应字段 _grades)
    [ObservableProperty] private ZdbkGrades? _grades;

    public ObservableCollection<string> AvailableYears { get; } = [];

    // 3. 自动生成 AvailableTerms 属性
    public ObservableCollection<string> AvailableTerms { get; } = ["秋冬", "春夏", "短"];

    [ObservableProperty] private string _selectedYear = string.Empty;

    [ObservableProperty] private string _selectedTerm = string.Empty;

    [ObservableProperty] private float _totalCredits = -1;
    [ObservableProperty] private float _gpa5 = -1;
    [ObservableProperty] private float _gpa100 = -1;
    [ObservableProperty] private float _majorGpa5 = -1;


    public GradeViewModel(
        ILocalizer localizer,
        IZdbkGradeService zdbkGradeService,
        IZdbkSessionService sessionService,
        IZjuSsoService zjuSsoService)
    {
        _localizer = localizer;
        _zdbkGradeService = zdbkGradeService;
        _zjuSsoService = zjuSsoService;
        _sessionService = sessionService;

        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.Grade) },
            IconKind = PackIconMaterialKind.CommentSearch,
            OnSelected = () => Navigator.NavigateTo<GradeViewModel>()
        };

        InitializeData();
    }

    private void InitializeData()
    {
        // 初始化年份列表
        var year = DateTime.Now.Year;
        AvailableYears.Clear();
        for (int i = -2; i <= 1; i++)
        {
            AvailableYears.Add($"{year + i}-{year + i + 1}");
        }

        SelectedYear = $"{year - 1}-{year}";
        SelectedTerm = "秋冬";

        // 第一次手动加载
        _ = LoadDataAsync();

        // 订阅 ZJU SSO 登录状态变化
        _zjuSsoService.OnLogin += HandleLogin;
        _zjuSsoService.OnLogout += HandleLogout;

        // 检查登录状态
        UpdateLoginStatus();

        // 如果已登录，初始化数据
        if (IsLoggedIn)
        {
            _ = LoadDataAsync();
        }

    }

    private void HandleLogin()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateLoginStatus();
            _ = LoadDataAsync();
        });
    }

    private void HandleLogout()
    {
        Dispatcher.UIThread.Post(UpdateLoginStatus);
    }

    private void UpdateLoginStatus()
    {
        IsLoggedIn = _zjuSsoService.IsAuthenticated;
    }

    partial void OnSelectedYearChanged(string value)
    {
        _ = LoadDataAsync();
    }

    partial void OnSelectedTermChanged(string value)
    {
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {

        var result = await _zdbkGradeService.GetSemesterGradesAsync(SelectedYear, SelectedTerm);
        if (result.IsSuccess)
        {
            Grades = result.Value;
        }
        else
        {
            Grades = new ZdbkGrades
            {
                Credit = 0,
                MajorCredit = 0,
                GradePoint5 = 0,
                GradePoint4 = 0,
                GradePoint100 = 0,
                MajorGradePoint = 0,
                CoursesGrade =
                [
                    new ZdbkCoursesGrade
                    {
                        CourseName = $"{result.Message}",
                        CourseCode = "Wrong",
                        Grade100 = 0,
                        Grade5 = 0,
                        Credit = 0,
                        Semester = ""
                    }
                ]
            };
        }

        result = await _zdbkGradeService.GetGradesAsync();
        if (result.IsSuccess)
        {
            if (result.Value != null)
            {
                TotalCredits = (float)result.Value.Credit;
                Gpa5 = (float)result.Value.GradePoint5;
                Gpa100 = (float)result.Value.GradePoint100;
                MajorGpa5 = (float)result.Value.MajorGradePoint;
            }
        }
    }
}
