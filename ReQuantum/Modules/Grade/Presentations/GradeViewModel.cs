using System;
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

    [ObservableProperty]
    private ZdbkGrades _grades;

    public GradeViewModel(ILocalizer localizer, IZdbkGradeService zdbkGradeService)
    {

        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.Grade) },
            IconKind = PackIconMaterialKind.CommentSearch,
            OnSelected = () => Navigator.NavigateTo<GradeViewModel>()
        };
        _localizer = localizer;
        _zdbkGradeService = zdbkGradeService;
        LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var result = await _zdbkGradeService.GetSemeserGradesAsync("2024-2025", "春");
        if (result.IsSuccess)
        {
            Grades = result.Value;
        }
    }



}
