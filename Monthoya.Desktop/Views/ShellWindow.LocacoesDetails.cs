using System.Globalization;
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

        var dados = locacaoDetails?.Dados;
        var partes = locacaoDetails?.Partes ?? Array.Empty<LocacaoParteSummary>();
        var culture = CultureInfo.GetCultureInfo("pt-BR");

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
        root.Children.Add(new TextBlock
        {
            Text = "Visualização da locação. Os campos ficam bloqueados até clicar em Editar.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            root.Children.Add(new TextBlock
            {
                Text = statusMessage,
                Foreground = System.Windows.Media.Brushes.SeaGreen,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        var form = new WrapPanel();
        root.Children.Add(form);

        AddLabeledControl(form, "Código", CreateReadOnlyTextBox(locacao.Codigo ?? "-"), 120);
        AddLabeledControl(form, "Status", CreateReadOnlyTextBox(locacao.Status), 150);
        AddLabeledControl(form, "Imóvel", CreateReadOnlyTextBox(locacao.ImovelResumo), 320);
        AddLabeledControl(form, "Proprietário(s) do imóvel", CreateReadOnlyTextBox(FormatLocacaoPartes(partes, TipoParteLocacao.Proprietario)), 300);
        AddLabeledControl(form, "Tipo de locação", CreateReadOnlyTextBox(locacao.TipoLocacao), 180);
        AddLabeledControl(form, "Aluguel atual", CreateReadOnlyTextBox((locacao.ValorAluguelAtual ?? dados?.ValorAluguelAtual ?? dados?.ValorAluguelInicial)?.ToString("N2", culture) ?? "-"), 180);
        AddLabeledControl(form, "Data início locação", CreateReadOnlyTextBox(FormatDate(dados?.DataInicioLocacao ?? locacao.DataInicioLocacao)), 180);
        AddLabeledControl(form, "Data início cobrança", CreateReadOnlyTextBox(FormatDate(dados?.DataInicioCobranca)), 180);
        AddLabeledControl(form, "Dia base", CreateReadOnlyTextBox((dados?.DiaBase ?? locacao.DiaVencimentoLocatario)?.ToString() ?? "-"), 120);
        AddLabeledControl(form, "Dia vencimento locatário", CreateReadOnlyTextBox((dados?.DiaVencimentoLocatario ?? locacao.DiaVencimentoLocatario)?.ToString() ?? "-"), 170);
        AddLabeledControl(form, "Dia repasse proprietário", CreateReadOnlyTextBox(dados?.DiaRepasseProprietario?.ToString() ?? "-"), 170);
        AddLabeledControl(form, string.Empty, CreateReadOnlyCheckBox("Aluguel antecipado", dados?.AluguelAntecipado == true), 190);
        AddLabeledControl(form, "Observações internas", CreateReadOnlyTextBox(dados?.ObservacoesInternas, height: 70, acceptsReturn: true), 520);

        AddReadOnlyLocacaoPartesSection(root, partes);

        ModuleDetailsHost.Content = root;
    }

    private void AddReadOnlyLocacaoPartesSection(Panel root, IReadOnlyList<LocacaoParteSummary> partes)
    {
        root.Children.Add(new TextBlock
        {
            Text = "Locatários e fiadores",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 4)
        });
        root.Children.Add(new TextBlock
        {
            Text = "O proprietário vem do imóvel selecionado. Locatários e fiadores ficam bloqueados na visualização.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var rowsPanel = new StackPanel();
        root.Children.Add(rowsPanel);

        AddReadOnlyParteRows(rowsPanel, partes, TipoParteLocacao.Locatario);
        AddReadOnlyParteRows(rowsPanel, partes, TipoParteLocacao.Fiador);
    }

    private void AddReadOnlyParteRows(Panel rowsPanel, IReadOnlyList<LocacaoParteSummary> partes, TipoParteLocacao tipoParte)
    {
        var selected = partes
            .Where(x => x.TipoParte == tipoParte)
            .OrderByDescending(x => x.IsPrincipal)
            .ThenBy(x => x.PessoaNome)
            .ToList();

        if (selected.Count == 0)
        {
            var emptyRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            AddInlineField(emptyRow, GetTipoParteLabel(tipoParte), CreateReadOnlyTextBox("-"), 300);
            emptyRow.Children.Add(CreateReadOnlyCheckBox("Principal", false));
            rowsPanel.Children.Add(emptyRow);
            return;
        }

        foreach (var parte in selected)
        {
            var rowPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            AddInlineField(rowPanel, GetTipoParteLabel(tipoParte), CreateReadOnlyTextBox(parte.PessoaNome), 300);
            rowPanel.Children.Add(CreateReadOnlyCheckBox("Principal", parte.IsPrincipal));
            rowsPanel.Children.Add(rowPanel);
        }
    }

    private static TextBox CreateReadOnlyTextBox(string? value, double? height = null, bool acceptsReturn = false)
    {
        var box = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
            IsReadOnly = true,
            IsTabStop = false,
            AcceptsReturn = acceptsReturn,
            TextWrapping = acceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Margin = new Thickness(0, 6, 0, 12)
        };

        if (height.HasValue)
        {
            box.Height = height.Value;
        }

        return box;
    }

    private static CheckBox CreateReadOnlyCheckBox(string text, bool isChecked) =>
        new()
        {
            Content = text,
            IsChecked = isChecked,
            IsEnabled = false,
            Margin = new Thickness(0, 28, 12, 0)
        };

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) ?? "-";
}
