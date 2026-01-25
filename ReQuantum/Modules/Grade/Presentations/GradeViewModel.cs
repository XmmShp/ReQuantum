using System;
using System.Diagnostics.CodeAnalysis;
using IconPacks.Avalonia.Material;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
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

    public GradeViewModel(ILocalizer localizer)
    {

        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.Grade) },
            IconKind = PackIconMaterialKind.CommentSearch,
            OnSelected = () => Navigator.NavigateTo<GradeViewModel>()
        };
        _localizer = localizer;

    }
}
