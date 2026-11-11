using Avalonia.Controls;
using Avalonia.Interactivity;
using NWNBuilder.Core;
using NWNBuilder.Core.Models;

namespace NWNBuilder;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = AppSettings.Load();
    private BuildStore? _store;
    private List<Build> _builds = new();
    private BuildEditor _editor = null!;

    public MainWindow()
    {
        InitializeComponent();

        // Vises indtil et build fra databasen vælges.
        ShowBuild(BuildJson.LoadSample("hesper-salcedo"));

        ConnectionBox.Text = _settings.ConnectionString;
        if (!string.IsNullOrWhiteSpace(_settings.ConnectionString))
            _ = ConnectAsync();
    }

    private void ShowBuild(Build build)
    {
        _editor = new BuildEditor(build);
        Editor.DataContext = _editor;
    }

    // --- Database ---

    private async void Connect_Click(object? sender, RoutedEventArgs e) => await ConnectAsync();

    private async Task ConnectAsync()
    {
        var connectionString = ConnectionBox.Text?.Trim() ?? "";
        if (connectionString.Length == 0)
        {
            StatusText.Text = "Skriv en connection string.";
            return;
        }

        StatusText.Text = "Forbinder…";
        try
        {
            var store = new BuildStore(connectionString);
            var builds = await store.ListAsync();

            _store = store;
            _settings.ConnectionString = connectionString;
            _settings.Save();

            _builds = builds;
            RefreshList(select: builds.FirstOrDefault());
            StatusText.Text = $"Forbundet · {builds.Count} builds";
        }
        catch (Exception ex)
        {
            _store = null;
            StatusText.Text = "Kunne ikke forbinde: " + ex.Message;
        }
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_store is null)
        {
            StatusText.Text = "Ikke forbundet.";
            return;
        }

        var build = _editor.Apply();
        try
        {
            await _store.SaveAsync(build);
            _builds = await _store.ListAsync();
            var saved = _builds.FirstOrDefault(b => b.Id == build.Id);
            RefreshList(select: saved);
            if (saved != null)
                ShowBuild(saved);
            StatusText.Text = $"Gemt: {build.Name} · {_builds.Count} builds";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Kunne ikke gemme: " + ex.Message;
        }
    }

    // --- Liste ---

    private void RefreshList(Build? select)
    {
        var filter = SearchBox.Text?.Trim() ?? "";
        BuildList.ItemsSource = filter.Length == 0
            ? _builds
            : _builds.Where(b => b.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        BuildList.SelectedItem = select;
    }

    private void Search_TextChanged(object? sender, TextChangedEventArgs e) =>
        RefreshList(select: BuildList.SelectedItem as Build);

    private void BuildList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BuildList.SelectedItem is Build build && build != _editor.Build)
            ShowBuild(build);
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        BuildList.SelectedItem = null;
        ShowBuild(BuildEditor.NewBuild());
        StatusText.Text = "Nyt build – husk at gemme.";
    }

    // --- Skills / crafting ---

    private void AddSkill_Click(object? sender, RoutedEventArgs e) => _editor.Skills.Add(new NamedValue());

    private void AddCraft_Click(object? sender, RoutedEventArgs e) => _editor.Crafting.Add(new NamedValue());

    private void RemoveRow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: NamedValue row })
        {
            _editor.Skills.Remove(row);
            _editor.Crafting.Remove(row);
        }
    }
}
