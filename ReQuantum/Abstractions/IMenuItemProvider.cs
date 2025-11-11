using IconPacks.Avalonia.Material;
using ReQuantum.Models;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.Abstractions;

public interface IMenuItemProvider
{
    string Title { get; }
    PackIconMaterialKind IconKind { get; }
    uint Order { get; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type ViewModelType { get; }

    Action<MenuItem> OnCultureChanged { get; }
}
