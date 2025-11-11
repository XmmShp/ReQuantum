using ReQuantum.Services;
using ReQuantum.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.Extensions;

public static class NavigatorExtensions
{
    public static void NavigateTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(this INavigator navigator)
        where TViewModel : class, IViewModel
    {
        navigator.NavigateTo(typeof(TViewModel));
    }
}
