using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
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

    private readonly ILocalizer _localizer;
    private readonly IZdbkGradeService _zdbkGradeService;

    // 1. 自动生成 Grades 属性 (对应字段 _grades)
    [ObservableProperty]
    private ZdbkGrades? _grades;

    public ObservableCollection<string> AvailableYears { get; } = [];

    // 3. 自动生成 AvailableTerms 属性
    public ObservableCollection<string> AvailableTerms { get; } =  [ "秋冬","春夏","短" ];

    // 4. 手动实现 SelectedYear 和 SelectedTerm，确保逻辑触发不依赖生成器的 partial 方法
    private string _selectedYear = string.Empty;
    public string SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    private string _selectedTerm = string.Empty;
    public string SelectedTerm
    {
        get => _selectedTerm;
        set
        {
            if (SetProperty(ref _selectedTerm, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    public GradeViewModel(ILocalizer localizer, IZdbkGradeService zdbkGradeService)
    {
        _localizer = localizer;
        _zdbkGradeService = zdbkGradeService;

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

        // 设置初始值 (直接设字段不触发 LoadData，避免重复调用)
        _selectedYear = $"{year}-{year + 1}";
        _selectedTerm = "秋冬";

        // 通知 UI 更新
        OnPropertyChanged(nameof(SelectedYear));
        OnPropertyChanged(nameof(SelectedTerm));

        // 第一次手动加载
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (string.IsNullOrEmpty(SelectedYear) || string.IsNullOrEmpty(SelectedTerm)) return;

        var result = await _zdbkGradeService.GetSemeserGradesAsync(SelectedYear, SelectedTerm);
        if (result.IsSuccess)
        {
            Grades = result.Value;
        }
        else
        {
            Grades = null;
        }
    }
}
