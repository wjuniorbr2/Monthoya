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
        AddReadOnlyGarantiaSection(root, locacaoDetails?.Garantia, culture);
        AddReadOnlyEncargosSection(root, locacaoDetails?.Encargos ?? Array.Empty<LocacaoEncargoSummary>(), culture);
        AddReadOnlyLancamentosSection(root, locacaoDetails?.Lancamentos ?? Array.Empty<LocacaoLancamentoSummary>(), culture);
        AddReadOnlyCobrancasSection(root, locacaoDetails?.Cobrancas ?? Array.Empty<LocacaoCobrancaSummary>(), culture);
        AddReadOnlyHistoricoSection(root, locacaoDetails?.Historicos ?? Array.Empty<LocacaoHistoricoSummary>(), culture);

        ModuleDetailsHost.Content = root;
    }

    private void AddReadOnlyLocacaoPartesSection(Panel root, IReadOnlyList<LocacaoParteSummary> partes)
    {
        AddReadOnlySectionHeader(
            root,
            "Locatários e fiadores",
            "O proprietário vem do imóvel selecionado. Locatários e fiadores ficam bloqueados na visualização.");

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

    private void AddReadOnlyGarantiaSection(Panel root, LocacaoGarantiaSummary? garantia, CultureInfo culture)
    {
        AddReadOnlySectionHeader(root, "Garantia", "Dados da garantia vinculada à locação.");

        if (garantia is null)
        {
            root.Children.Add(CreateMutedText("Sem garantia cadastrada."));
            return;
        }

        var form = new WrapPanel();
        root.Children.Add(form);
        AddLabeledControl(form, "Tipo", CreateReadOnlyTextBox(garantia.TipoGarantia.ToString()), 180);
        AddLabeledControl(form, "Valor", CreateReadOnlyTextBox(FormatCurrency(garantia.Valor, culture)), 160);
        AddLabeledControl(form, "Validade", CreateReadOnlyTextBox(FormatDate(garantia.DataValidade)), 160);
        AddLabeledControl(form, string.Empty, CreateReadOnlyCheckBox("Ativa", garantia.Ativa), 120);
        AddLabeledControl(form, "Observações", CreateReadOnlyTextBox(garantia.Observacoes, height: 60, acceptsReturn: true), 360);
    }

    private void AddReadOnlyEncargosSection(Panel root, IReadOnlyList<LocacaoEncargoSummary> encargos, CultureInfo culture)
    {
        AddReadOnlyTextListSection(
            root,
            "Encargos",
            "Condomínio, IPTU, seguros e outras despesas recorrentes da locação.",
            encargos
                .OrderByDescending(x => x.Ativo)
                .ThenBy(x => x.TipoEncargo.ToString())
                .Select(x => FormatEncargo(x, culture)),
            "Sem encargos cadastrados.");
    }

    private void AddReadOnlyLancamentosSection(Panel root, IReadOnlyList<LocacaoLancamentoSummary> lancamentos, CultureInfo culture)
    {
        AddReadOnlyTextListSection(
            root,
            "Lançamentos",
            "Ajustes, descontos, reembolsos e lançamentos avulsos vinculados à locação.",
            lancamentos
                .OrderByDescending(x => x.Competencia)
                .ThenByDescending(x => x.DataVencimento)
                .Select(x => FormatLancamento(x, culture)),
            "Sem lançamentos cadastrados.");
    }

    private void AddReadOnlyCobrancasSection(Panel root, IReadOnlyList<LocacaoCobrancaSummary> cobrancas, CultureInfo culture)
    {
        AddReadOnlyTextListSection(
            root,
            "Cobranças",
            "Prévia das cobranças geradas para a locação.",
            cobrancas
                .OrderByDescending(x => x.Competencia)
                .ThenByDescending(x => x.DataVencimento)
                .Select(x => FormatCobranca(x, culture)),
            "Sem cobranças geradas.");
    }

    private void AddReadOnlyHistoricoSection(Panel root, IReadOnlyList<LocacaoHistoricoSummary> historicos, CultureInfo culture)
    {
        AddReadOnlyTextListSection(
            root,
            "Histórico",
            "Registro de alterações relevantes da locação.",
            historicos
                .OrderByDescending(x => x.DataHoraUtc)
                .Take(8)
                .Select(x => FormatHistorico(x, culture)),
            "Sem histórico registrado.");
    }

    private void AddReadOnlyTextListSection(
        Panel root,
        string title,
        string description,
        IEnumerable<string> lines,
        string emptyMessage)
    {
        AddReadOnlySectionHeader(root, title, description);
        var text = string.Join(Environment.NewLine, lines.Where(x => !string.IsNullOrWhiteSpace(x)));
        root.Children.Add(string.IsNullOrWhiteSpace(text)
            ? CreateMutedText(emptyMessage)
            : CreateReadOnlyTextBox(text, height: 88, acceptsReturn: true));
    }

    private static void AddReadOnlySectionHeader(Panel root, string title, string description)
    {
        root.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 4)
        });
        root.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
    }

    private static TextBlock CreateMutedText(string text) =>
        new()
        {
            Text = text,
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

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

    private static string FormatEncargo(LocacaoEncargoSummary encargo, CultureInfo culture)
    {
        var parts = new List<string>
        {
            encargo.TipoEncargo.ToString(),
            FormatCurrency(encargo.Valor, culture),
            encargo.Ativo ? "ativo" : "inativo"
        };

        if (encargo.DiaVencimento.HasValue)
        {
            parts.Add($"venc. dia {encargo.DiaVencimento.Value}");
        }

        if (encargo.CobradoComAluguel) parts.Add("cobrado com aluguel");
        if (encargo.PagoDiretoPeloLocatario) parts.Add("pago direto pelo locatário");
        if (encargo.PagoPeloProprietario) parts.Add("pago pelo proprietário");
        if (encargo.RequerAtualizacao) parts.Add("requer atualização");

        return string.Join(" • ", parts);
    }

    private static string FormatLancamento(LocacaoLancamentoSummary lancamento, CultureInfo culture)
    {
        var competencia = FormatDate(lancamento.Competencia);
        var vencimento = FormatDate(lancamento.DataVencimento);
        return $"{competencia} • {lancamento.TipoLancamento}: {lancamento.Descricao} • {FormatCurrency(lancamento.Valor, culture)} • venc. {vencimento} • {lancamento.Status}";
    }

    private static string FormatCobranca(LocacaoCobrancaSummary cobranca, CultureInfo culture) =>
        $"{FormatDate(cobranca.Competencia)} • {cobranca.TipoCobranca} • venc. {FormatDate(cobranca.DataVencimento)} • {FormatCurrency(cobranca.ValorTotal, culture)} • {cobranca.Status}";

    private static string FormatHistorico(LocacaoHistoricoSummary historico, CultureInfo culture)
    {
        var when = historico.DataHoraUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", culture);
        var field = string.IsNullOrWhiteSpace(historico.Campo) ? string.Empty : $" • {historico.Campo}";
        var user = string.IsNullOrWhiteSpace(historico.Usuario) ? string.Empty : $" • {historico.Usuario}";
        return $"{when}{user} • {historico.Acao}{field}";
    }

    private static string FormatCurrency(decimal? value, CultureInfo culture) =>
        value.HasValue ? value.Value.ToString("C2", culture) : "-";

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) ?? "-";
}
