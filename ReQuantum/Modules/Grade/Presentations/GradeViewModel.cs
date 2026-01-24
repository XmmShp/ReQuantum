using System;
using System.Collections.Generic;
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
    [ObservableProperty]
    private bool _isLoggedIn;

    private readonly ILocalizer _localizer;
    private readonly IZdbkGradeService _zdbkGradeService;
    private readonly IZdbkSessionService _sessionService;
    private readonly IZjuSsoService _zjuSsoService;

    [ObservableProperty]
    private ZdbkGrades _grades;

    public GradeViewModel(
        ILocalizer localizer,
        IZdbkGradeService zdbkGradeService,
        IZdbkSessionService sessionService,
        IZjuSsoService zjuSsoService)
    {

        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.Grade) },
            IconKind = PackIconMaterialKind.CommentSearch,
            OnSelected = () => Navigator.NavigateTo<GradeViewModel>()
        };
        _localizer = localizer;
        _zdbkGradeService = zdbkGradeService;
        _sessionService = sessionService;
        _zjuSsoService = zjuSsoService;

        // 订阅 ZJU SSO 登录状态变化
        _zjuSsoService.OnLogin += HandleLogin;
        _zjuSsoService.OnLogout += HandleLogout;

        // 检查登录状态
        UpdateLoginStatus();

        // 如果已登录，初始化数据
        if (IsLoggedIn)
        {
            LoadDataAsync();
        }

        LoadDataAsync();
    }

    private void HandleLogin()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateLoginStatus();
            LoadDataAsync();
        });
    }

    private void HandleLogout()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateLoginStatus();
        });
    }
    private void UpdateLoginStatus()
    {
        IsLoggedIn = _zjuSsoService.IsAuthenticated;
    }
    private async Task LoadDataAsync()
    {
        var result = await _zdbkGradeService.GetSemeserGradesAsync("2025-2026", "秋");
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
                CoursesGrade = new List<ZdbkCoursesGrade>
                {
                    new ZdbkCoursesGrade { CourseName = $"{result.Message}", CourseCode = "Wrong", Grade100 = 0, Grade5 = 0, Credit = 0, Semester = "" },
                }
            };
        }
    }


}
