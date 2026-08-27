using System.Collections;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class TeamSectionView : ContentView
{
    public static readonly BindableProperty HasTeamProperty = BindableProperty.Create(
        nameof(HasTeam), typeof(bool), typeof(TeamSectionView), false);

    public static readonly BindableProperty TeamSectionTitleProperty = BindableProperty.Create(
        nameof(TeamSectionTitle), typeof(string), typeof(TeamSectionView), string.Empty);

    public static readonly BindableProperty TeamCountTextProperty = BindableProperty.Create(
        nameof(TeamCountText), typeof(string), typeof(TeamSectionView), string.Empty);

    public static readonly BindableProperty TeamMembersProperty = BindableProperty.Create(
        nameof(TeamMembers), typeof(IEnumerable), typeof(TeamSectionView));

    public bool HasTeam
    {
        get => (bool)GetValue(HasTeamProperty);
        set => SetValue(HasTeamProperty, value);
    }

    public string TeamSectionTitle
    {
        get => (string)GetValue(TeamSectionTitleProperty);
        set => SetValue(TeamSectionTitleProperty, value);
    }

    public string TeamCountText
    {
        get => (string)GetValue(TeamCountTextProperty);
        set => SetValue(TeamCountTextProperty, value);
    }

    public IEnumerable? TeamMembers
    {
        get => (IEnumerable?)GetValue(TeamMembersProperty);
        set => SetValue(TeamMembersProperty, value);
    }

    public TeamSectionView()
    {
        InitializeComponent();
    }
}
