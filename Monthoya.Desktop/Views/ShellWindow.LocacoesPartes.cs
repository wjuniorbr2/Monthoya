using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private StackPanel CreateLocacaoPartesEditor(
        IReadOnlyList<PessoaSummary> pessoas,
        TextBlock errorText,
        List<LocacaoParteEditorRow> rows)
    {
        var root = new StackPanel { Margin = new Thickness(0, 2, 0, 12) };
        root.Children.Add(new TextBlock
        {
            Text = "Participantes",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 8)
        });

        var rowsPanel = new StackPanel();
        root.Children.Add(rowsPanel);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };

        AddParteButton(buttons, "Adicionar proprietário", () => AddLocacaoParteRow(rowsPanel, pessoas, rows, TipoParteLocacao.Proprietario, errorText));
        AddParteButton(buttons, "Adicionar locatário", () => AddLocacaoParteRow(rowsPanel, pessoas, rows, TipoParteLocacao.Locatario, errorText));
        AddParteButton(buttons, "Adicionar fiador", () => AddLocacaoParteRow(rowsPanel, pessoas, rows, TipoParteLocacao.Fiador, errorText));
        root.Children.Add(buttons);

        AddLocacaoParteRow(rowsPanel, pessoas, rows, TipoParteLocacao.Proprietario, errorText);
        AddLocacaoParteRow(rowsPanel, pessoas, rows, TipoParteLocacao.Locatario, errorText);
        return root;
    }

    private void AddParteButton(Panel panel, string text, Action onClick)
    {
        var button = new Button
        {
            Content = text,
            Style = (Style)FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        button.Click += (_, _) => onClick();
        panel.Children.Add(button);
    }

    private void AddLocacaoParteRow(
        Panel rowsPanel,
        IReadOnlyList<PessoaSummary> pessoas,
        List<LocacaoParteEditorRow> rows,
        TipoParteLocacao tipoParte,
        TextBlock errorText)
    {
        var rowPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
        var pessoaBox = CreateComboBox(pessoas, nameof(PessoaSummary.Nome));
        pessoaBox.Width = 300;
        var principalBox = new CheckBox
        {
            Content = "Principal",
            IsChecked = rows.All(x => x.TipoParte != tipoParte),
            Margin = new Thickness(0, 28, 12, 0)
        };
        var percentualBox = new TextBox
        {
            Text = tipoParte == TipoParteLocacao.Proprietario && rows.All(x => x.TipoParte != TipoParteLocacao.Proprietario) ? "100,00" : string.Empty,
            Width = 80,
            Margin = new Thickness(0, 6, 12, 0),
            Visibility = tipoParte == TipoParteLocacao.Proprietario ? Visibility.Visible : Visibility.Collapsed
        };
        ConfigureLocacaoDecimalTextBox(percentualBox, errorText);

        var removeButton = new Button
        {
            Content = "Remover",
            Style = (Style)FindResource("SecondaryButton"),
            Margin = new Thickness(0, 23, 0, 0)
        };

        var row = new LocacaoParteEditorRow(tipoParte, rowPanel, pessoaBox, principalBox, percentualBox);
        removeButton.Click += (_, _) =>
        {
            rows.Remove(row);
            rowsPanel.Children.Remove(rowPanel);
        };

        AddInlineField(rowPanel, GetTipoParteLabel(tipoParte), pessoaBox, 300);
        if (tipoParte == TipoParteLocacao.Proprietario)
        {
            AddInlineField(rowPanel, "Participação", percentualBox, 100);
        }

        rowPanel.Children.Add(principalBox);
        rowPanel.Children.Add(removeButton);
        rows.Add(row);
        rowsPanel.Children.Add(rowPanel);
    }

    private static void AddInlineField(Panel panel, string label, Control control, double width)
    {
        var field = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 12, 0)
        };
        field.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
        field.Children.Add(control);
        panel.Children.Add(field);
    }

    private static IReadOnlyList<LocacaoParteRequest> BuildLocacaoParteRequests(IReadOnlyList<LocacaoParteEditorRow> rows)
    {
        var requests = new List<LocacaoParteRequest>();
        foreach (var row in rows)
        {
            if (row.PessoaBox.SelectedItem is not PessoaSummary pessoa)
            {
                throw new InvalidOperationException("Selecione a pessoa em todas as linhas de participantes ou remova a linha vazia.");
            }

            var percentual = row.TipoParte == TipoParteLocacao.Proprietario
                ? ParseNullableDecimal(row.PercentualBox.Text)
                : null;

            requests.Add(new LocacaoParteRequest(
                pessoa.Id,
                row.TipoParte,
                IsPrincipal: row.PrincipalBox.IsChecked == true,
                PercentualParticipacao: percentual,
                RecebeCobranca: row.TipoParte == TipoParteLocacao.Locatario,
                RecebeRepasse: row.TipoParte == TipoParteLocacao.Proprietario,
                RecebeNotificacao: true));
        }

        if (requests.Count(x => x.TipoParte == TipoParteLocacao.Proprietario) == 1)
        {
            var index = requests.FindIndex(x => x.TipoParte == TipoParteLocacao.Proprietario);
            requests[index] = requests[index] with
            {
                IsPrincipal = true,
                PercentualParticipacao = requests[index].PercentualParticipacao ?? 100m
            };
        }

        if (requests.Count(x => x.TipoParte == TipoParteLocacao.Locatario) == 1)
        {
            var index = requests.FindIndex(x => x.TipoParte == TipoParteLocacao.Locatario);
            requests[index] = requests[index] with { IsPrincipal = true };
        }

        if (requests.Count(x => x.TipoParte == TipoParteLocacao.Fiador) == 1)
        {
            var index = requests.FindIndex(x => x.TipoParte == TipoParteLocacao.Fiador);
            requests[index] = requests[index] with { IsPrincipal = true };
        }

        return requests;
    }

    private static string FormatLocacaoPartes(IEnumerable<LocacaoParteSummary> partes, TipoParteLocacao tipoParte)
    {
        var formatted = partes
            .Where(x => x.TipoParte == tipoParte)
            .OrderByDescending(x => x.IsPrincipal)
            .ThenBy(x => x.PessoaNome)
            .Select(x =>
            {
                var principal = x.IsPrincipal ? " (principal)" : string.Empty;
                var percentual = tipoParte == TipoParteLocacao.Proprietario && x.PercentualParticipacao.HasValue
                    ? $" - {x.PercentualParticipacao.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}%"
                    : string.Empty;
                return $"{x.PessoaNome}{principal}{percentual}";
            })
            .ToList();

        return formatted.Count == 0 ? "-" : string.Join(Environment.NewLine, formatted);
    }

    private static string GetTipoParteLabel(TipoParteLocacao tipoParte) =>
        tipoParte switch
        {
            TipoParteLocacao.Proprietario => "Proprietário",
            TipoParteLocacao.Locatario => "Locatário",
            TipoParteLocacao.Fiador => "Fiador",
            _ => "Participante"
        };

    private sealed record LocacaoParteEditorRow(
        TipoParteLocacao TipoParte,
        Panel Panel,
        ComboBox PessoaBox,
        CheckBox PrincipalBox,
        TextBox PercentualBox);
}
