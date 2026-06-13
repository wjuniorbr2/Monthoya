using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private sealed record LocacaoGarantiaTipoOption(string Label, TipoGarantiaLocacao Tipo)
    {
        public override string ToString() => Label;
    }

    private sealed record LocacaoGarantiaEditor(
        StackPanel Root,
        ComboBox TipoGarantiaBox,
        TextBox ValorBox,
        DatePicker DataValidadeBox,
        CheckBox AtivaBox,
        TextBox ObservacoesBox,
        TextBox ObservacoesDocumentoBox);

    private static readonly IReadOnlyList<LocacaoGarantiaTipoOption> LocacaoGarantiaTipoOptions =
    [
        new("Nenhuma", TipoGarantiaLocacao.Nenhuma),
        new("Fiador", TipoGarantiaLocacao.Fiador),
        new("Caução em dinheiro", TipoGarantiaLocacao.CaucaoDinheiro),
        new("Caução em bem", TipoGarantiaLocacao.CaucaoBem),
        new("Seguro fiança", TipoGarantiaLocacao.SeguroFianca),
        new("Título de capitalização", TipoGarantiaLocacao.TituloCapitalizacao),
        new("Cessão fiduciária", TipoGarantiaLocacao.CessaoFiduciaria),
        new("Outro", TipoGarantiaLocacao.Outro)
    ];

    private LocacaoGarantiaEditor CreateLocacaoGarantiaEditor(LocacaoGarantiaRequest? garantia, TextBlock errorText)
    {
        var root = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        root.Children.Add(new TextBlock
        {
            Text = "Garantia",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        root.Children.Add(new TextBlock
        {
            Text = "Informe a garantia contratual, quando houver. Deixe como Nenhuma para locação sem garantia.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var form = new WrapPanel();
        root.Children.Add(form);

        var tipoBox = new ComboBox
        {
            ItemsSource = LocacaoGarantiaTipoOptions,
            SelectedValuePath = "Tipo",
            DisplayMemberPath = "Label",
            SelectedValue = garantia?.TipoGarantia ?? TipoGarantiaLocacao.Nenhuma,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var valorBox = new TextBox
        {
            Text = garantia?.Valor?.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var validadeBox = new DatePicker
        {
            SelectedDate = ToLocacaoGarantiaDateTime(garantia?.DataValidade),
            Margin = new Thickness(0, 6, 0, 12)
        };
        var ativaBox = new CheckBox
        {
            Content = "Garantia ativa",
            IsChecked = garantia?.Ativa ?? true,
            Margin = new Thickness(0, 28, 0, 12)
        };
        var observacoesBox = new TextBox
        {
            Text = garantia?.Observacoes ?? string.Empty,
            AcceptsReturn = true,
            Height = 60,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var observacoesDocumentoBox = new TextBox
        {
            Text = garantia?.ObservacoesDocumento ?? string.Empty,
            AcceptsReturn = true,
            Height = 60,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 12)
        };

        ConfigureLocacaoDecimalTextBox(valorBox, errorText);
        ConfigureLocacaoDatePicker(validadeBox, errorText);

        AddLabeledControl(form, "Tipo de garantia", tipoBox, 210);
        AddLabeledControl(form, "Valor", valorBox, 150);
        AddLabeledControl(form, "Validade", validadeBox, 170);
        AddLabeledControl(form, string.Empty, ativaBox, 150);
        AddLabeledControl(form, "Observações", observacoesBox, 360);
        AddLabeledControl(form, "Observações do documento", observacoesDocumentoBox, 360);

        return new LocacaoGarantiaEditor(
            root,
            tipoBox,
            valorBox,
            validadeBox,
            ativaBox,
            observacoesBox,
            observacoesDocumentoBox);
    }

    private LocacaoGarantiaRequest? BuildLocacaoGarantiaRequest(LocacaoGarantiaEditor editor)
    {
        var tipoGarantia = editor.TipoGarantiaBox.SelectedValue is TipoGarantiaLocacao selectedTipo
            ? selectedTipo
            : TipoGarantiaLocacao.Nenhuma;
        var hasGarantiaData =
            !string.IsNullOrWhiteSpace(editor.ValorBox.Text) ||
            editor.DataValidadeBox.SelectedDate.HasValue ||
            !string.IsNullOrWhiteSpace(editor.ObservacoesBox.Text) ||
            !string.IsNullOrWhiteSpace(editor.ObservacoesDocumentoBox.Text);

        if (tipoGarantia == TipoGarantiaLocacao.Nenhuma)
        {
            if (hasGarantiaData)
            {
                throw new InvalidOperationException("Selecione o tipo de garantia ou limpe os campos da garantia.");
            }

            return null;
        }

        var valor = ParseNullableDecimal(editor.ValorBox.Text);
        if (valor is < 0)
        {
            throw new InvalidOperationException("O valor da garantia não pode ser negativo.");
        }

        return new LocacaoGarantiaRequest(
            tipoGarantia,
            Valor: valor,
            DataValidade: ToLocacaoDateOnly(editor.DataValidadeBox.SelectedDate),
            Ativa: editor.AtivaBox.IsChecked == true,
            Observacoes: NullIfWhiteSpace(editor.ObservacoesBox.Text),
            ObservacoesDocumento: NullIfWhiteSpace(editor.ObservacoesDocumentoBox.Text));
    }

    private static DateTime? ToLocacaoGarantiaDateTime(DateOnly? value) =>
        value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
