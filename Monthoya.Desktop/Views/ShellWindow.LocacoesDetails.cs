using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async void ShowLocacaoDetails(LocacaoSummary locacao, string? statusMessage = null)
    {
        ModuleDetailsBorder.Visibility = Visibility.Visible;
        LocacaoDetails? locacaoDetails = null;
        try
        {
            locacaoDetails = await _rentalManagementService.GetLocacaoAsync(locacao.Id);
        }
        catch
        {
            // Keep the summary visible if details cannot be loaded right now.
        }

        var root = new StackPanel();

        var header = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = $"Locação {locacao.Codigo ?? "-"}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 0);
        header.Children.Add(title);

        var actionButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var editButton = new Button
        {
            Content = "Editar",
            Style = (Style)FindResource("PrimaryButtonSmall"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        editButton.Click += async (_, _) => await ShowEditLocacaoInlineAsync(locacao);
        actionButtons.Children.Add(editButton);

        var removeButton = new Button
        {
            Content = "Remover",
            Style = (Style)FindResource("SecondaryButton"),
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = !string.Equals(locacao.Status, "Cancelada", StringComparison.OrdinalIgnoreCase)
        };
        removeButton.Click += async (_, _) => await CancelLocacaoWithPasswordAsync(locacao);
        actionButtons.Children.Add(removeButton);

        Grid.SetColumn(actionButtons, 1);
        header.Children.Add(actionButtons);

        root.Children.Add(header);

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            root.Children.Add(new TextBlock
            {
                Text = statusMessage,
                Foreground = System.Windows.Media.Brushes.SeaGreen,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        var details = new WrapPanel();
        root.Children.Add(details);
        AddDetailText(details, "Status", locacao.Status);
        AddDetailText(details, "Tipo", locacao.TipoLocacao);
        AddDetailText(details, "Imóvel", locacao.ImovelResumo, 360);
        AddDetailText(details, "Locatário principal", locacao.LocatarioPrincipalNome);
        AddDetailText(details, "Proprietário principal", locacao.ProprietarioPrincipalNome);
        AddDetailText(details, "Aluguel atual", locacao.ValorAluguelAtual?.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) ?? "-");
        AddDetailText(details, "Vencimento locatário", locacao.DiaVencimentoLocatario?.ToString() ?? "-");
        AddDetailText(details, "Início", locacao.DataInicioLocacao?.ToString("dd/MM/yyyy") ?? "-");
        AddDetailText(details, "Fim previsto", locacao.DataFimPrevista?.ToString("dd/MM/yyyy") ?? "-");
        AddDetailText(details, "Alertas", string.IsNullOrWhiteSpace(locacao.AlertasTexto) ? "-" : locacao.AlertasTexto, 360);
        if (locacaoDetails is not null)
        {
            AddDetailText(details, "Proprietários", FormatLocacaoPartes(locacaoDetails.Partes, TipoParteLocacao.Proprietario), 360);
            AddDetailText(details, "Locatários", FormatLocacaoPartes(locacaoDetails.Partes, TipoParteLocacao.Locatario), 360);
            AddDetailText(details, "Fiadores", FormatLocacaoPartes(locacaoDetails.Partes, TipoParteLocacao.Fiador), 360);
        }

        root.Children.Add(new TextBlock
        {
            Text = "Use Editar para alterar os dados básicos desta locação.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        });

        ModuleDetailsHost.Content = root;
    }

    private static void AddDetailText(Panel panel, string label, string value, double width = 220)
    {
        var field = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 18, 12)
        };
        field.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
        field.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "-" : value, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(field);
    }
}
