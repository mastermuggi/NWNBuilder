using Avalonia.Controls;
using Avalonia.Interactivity;
using NWNBuilder.Core;
using NWNBuilder.Core.Models;

namespace NWNBuilder;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = AppSettings.Load();
    private BuildStore? _store;

    public MainWindow()
    {
        InitializeComponent();

        // Vises indtil et build fra databasen vælges.
        BuildView.DataContext = BuildJson.LoadSample("hesper-salcedo");

        ConnectionBox.Text = _settings.ConnectionString;
        if (!string.IsNullOrWhiteSpace(_settings.ConnectionString))
            _ = ConnectAsync();
    }

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

            ShowBuilds(builds, select: builds.FirstOrDefault());
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
        if (BuildView.DataContext is not Build build)
            return;

        try
        {
            await _store.SaveAsync(build);
            var builds = await _store.ListAsync();
            ShowBuilds(builds, select: builds.FirstOrDefault(b => b.Id == build.Id));
            StatusText.Text = $"Gemt: {build.Name} · {builds.Count} builds";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Kunne ikke gemme: " + ex.Message;
        }
    }

    private void ShowBuilds(List<Build> builds, Build? select)
    {
        BuildList.ItemsSource = builds;
        if (select != null)
            BuildList.SelectedItem = select;
    }

    private void BuildList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BuildList.SelectedItem is Build build)
            BuildView.DataContext = build;
    }
}
