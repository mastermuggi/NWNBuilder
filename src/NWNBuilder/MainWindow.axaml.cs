using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NWNBuilder.Core;
using NWNBuilder.Core.Models;
using NWNBuilder.Core.Rules;

namespace NWNBuilder;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = AppSettings.Load();
    private BuildStore? _store;
    private List<Build> _builds = new();
    private Dictionary<string, Ruleset> _rulesets = new(StringComparer.OrdinalIgnoreCase);
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
        _editor = new BuildEditor(build, _rulesets.GetValueOrDefault(build.Ruleset));
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
            _rulesets = (await store.ListRulesetsAsync()).ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

            _store = store;
            _settings.ConnectionString = connectionString;
            _settings.Save();

            _builds = builds;
            RefreshList(select: builds.FirstOrDefault());
            ShowBuild(builds.FirstOrDefault() ?? _editor.Build);
            StatusText.Text = $"Forbundet · {builds.Count} builds · " + (_rulesets.Count == 0
                ? "intet regelsæt – tryk Importér regelsæt"
                : "regelsæt: " + string.Join(", ", _rulesets.Keys));
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

    private async void ImportRuleset_Click(object? sender, RoutedEventArgs e)
    {
        if (_store is null)
        {
            StatusText.Text = "Forbind til databasen først.";
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Vælg regelsæt (JSON)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Regelsæt (JSON)") { Patterns = new[] { "*.json" } } },
        });
        if (files.Count == 0)
            return;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            var ruleset = BuildJson.LoadRuleset(stream);
            await _store.SaveRulesetAsync(ruleset);
            _rulesets[ruleset.Id] = ruleset;
            ShowBuild(_editor.Apply());
            StatusText.Text = $"Regelsæt importeret: {ruleset.Name} ({ruleset.Version}) · {ruleset.Classes.Count} klasser";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Kunne ikke importere: " + ex.Message;
        }
    }

    // --- Beregning ---

    private void Tabs_SelectionChanged(object? sender, SelectionChangedEventArgs e) => _editor?.Recalculate();

    private void Gift_Click(object? sender, RoutedEventArgs e) => _editor.Recalculate();

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

    private void AddSkill_Click(object? sender, RoutedEventArgs e) => _editor.Skills.Add(new SkillRow());

    private void AddCraft_Click(object? sender, RoutedEventArgs e) => _editor.Crafting.Add(new NamedValue());

    private void AddFriend_Click(object? sender, RoutedEventArgs e) => _editor.Friends.Add(new Friend());

    private void RemoveRow_Click(object? sender, RoutedEventArgs e)
    {
        switch ((sender as Button)?.DataContext)
        {
            case SkillRow skill:
                _editor.Skills.Remove(skill);
                break;
            case NamedValue row:
                _editor.Crafting.Remove(row);
                break;
            case Friend friend:
                _editor.Friends.Remove(friend);
                break;
        }
    }
}
