using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static void SetFieldLabel(Control control, string labelText)
    {
        if (control.Parent is StackPanel parent
            && parent.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock label)
        {
            label.Text = labelText;
        }
    }

    private static TextBox NewTextBox(double width) => new()
    {
        Width = width,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 6, 0, 0)
    };

    private static TextBox NewMultilineBox(double height) => new()
    {
        Width = 360,
        Height = height,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 6, 0, 0)
    };

    private static TextBox NewMultilineBox(double width, double height)
    {
        var textBox = NewMultilineBox(height);
        textBox.Width = width;
        return textBox;
    }

    private void CreatePessoaEstadoCivilComboBox()
    {
        if (_pessoaEstadoCivilComboBox is not null)
        {
            return;
        }

        _pessoaEstadoCivilComboBox = new ComboBox
        {
            Width = 170,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = false,
            SelectedIndex = -1,
            ItemsSource = new[]
            {
                string.Empty,
                "Solteiro(a)",
                "Casado(a)",
                "União estável",
                "Divorciado(a)",
                "Separado(a)",
                "Viúvo(a)"
            }
        };

        _pessoaEstadoCivilComboBox.SelectionChanged += (_, _) =>
        {
            PessoaEstadoCivilBox.Text = _pessoaEstadoCivilComboBox.SelectedItem as string ?? string.Empty;
        };
        PessoaEstadoCivilBox.TextChanged += (_, _) => SyncPessoaEstadoCivilComboFromTextBox();
        SyncPessoaEstadoCivilComboFromTextBox();
    }

    private void SyncPessoaEstadoCivilComboFromTextBox()
    {
        if (_pessoaEstadoCivilComboBox is null)
        {
            return;
        }

        var value = PessoaEstadoCivilBox.Text;
        _pessoaEstadoCivilComboBox.SelectedItem = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void CreatePessoaWorkComboBox()
    {
        _pessoaWorkComboBox ??= new ComboBox
        {
            Width = 180,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não possui trabalho", "Possui trabalho" },
            SelectedIndex = -1
        };
    }

    private void CreatePessoaPetControls()
    {
        if (_pessoaPetComboBox is not null)
        {
            return;
        }

        _pessoaPetQualBox = new TextBox
        {
            Width = 150,
            Margin = new Thickness(0, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };
        _pessoaPetComboBox = new ComboBox
        {
            Width = 100,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não", "Sim" },
            SelectedIndex = -1
        };
        _pessoaPetComboBox.SelectionChanged += (_, _) =>
        {
            UpdatePessoaPetQualVisibility();
        };
    }

    private void UpdatePessoaPetQualVisibility()
    {
        var isJuridica = PessoaTipoBox.SelectedValue is Monthoya.Core.Entities.TipoPessoa.Juridica;
        var hasPet = string.Equals(_pessoaPetComboBox?.SelectedItem as string, "Sim", StringComparison.OrdinalIgnoreCase);
        var visibility = !isJuridica && hasPet ? Visibility.Visible : Visibility.Collapsed;

        if (_pessoaPetQualTopCell is not null)
        {
            _pessoaPetQualTopCell.Visibility = visibility;
        }

        if (_pessoaPetQualBox is not null)
        {
            _pessoaPetQualBox.Visibility = visibility;
        }
    }

    private void AttachPessoaRgEightDigitFormatter()
    {
        PessoaRgBox.TextChanged += PessoaRgEightDigitFormatter_TextChanged;
        PessoaConjugeRgBox.TextChanged += PessoaRgEightDigitFormatter_TextChanged;
        PessoaResponsavelRgBox.TextChanged += PessoaRgEightDigitFormatter_TextChanged;
    }

    private void PessoaRgEightDigitFormatter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingPessoaRgPatch || sender is not TextBox textBox)
        {
            return;
        }

        var digits = new string((textBox.Text ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            return;
        }

        var formatted = $"{digits[..1]}.{digits.Substring(1, 3)}.{digits.Substring(4, 3)}-{digits[7..]}";
        if (textBox.Text == formatted)
        {
            return;
        }

        _isFormattingPessoaRgPatch = true;
        textBox.Text = formatted;
        textBox.CaretIndex = formatted.Length;
        _isFormattingPessoaRgPatch = false;
    }

    private void HideOriginalEstadoCivilField()
    {
        PessoaEstadoCivilBox.Visibility = Visibility.Collapsed;
        PessoaEstadoCivilBox.Width = 0;

        if (PessoaEstadoCivilBox.Parent is Panel parent)
        {
            var index = parent.Children.IndexOf(PessoaEstadoCivilBox);
            if (index > 0 && parent.Children[index - 1] is TextBlock label && label.Text == "Estado civil")
            {
                label.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static bool RemoveChild(Panel parent, UIElement child)
    {
        if (parent.Children.Contains(child))
        {
            parent.Children.Remove(child);
            return true;
        }

        return false;
    }

    private static TextBlock? RemoveTextBlock(Panel parent, string text)
    {
        var match = parent.Children.OfType<TextBlock>().FirstOrDefault(block => block.Text == text);
        if (match is not null)
        {
            parent.Children.Remove(match);
        }

        return match;
    }

    private static int FindDocumentEditorStart(StackPanel formStack)
    {
        for (var index = 0; index < formStack.Children.Count; index++)
        {
            if (formStack.Children[index] is TextBlock label
                && (label.Text == "Documento digitalizado" || label.Text == "Documentos anexos:"))
            {
                return index;
            }
        }

        return -1;
    }

    private void ApplyCompactPessoaFieldSizing()
    {
        SetCompact(PessoaTipoBox, 140);
        SetCompact(PessoaNomeBox, 260);
        SetCompact(PessoaDocumentoBox, 190);
        SetCompact(PessoaRgBox, 150);
        SetCompact(PessoaTelefoneBox, 160);
        SetCompact(PessoaEmailBox, 260);
        SetCompact(PessoaRuaBox, 300);
        SetCompact(PessoaComplementoBox, 220);
        SetCompact(PessoaBairroBox, 220);
        SetCompact(PessoaCidadeBox, 180);
        SetCompact(PessoaCepBox, 120);
        SetCompact(PessoaEstadoBox, 80);
        SetCompact(PessoaNumeroBox, 90);
        SetCompact(PessoaDataNascimentoBox, 140);
        SetCompact(PessoaConjugeCpfBox, 190);
        SetCompact(PessoaConjugeRgBox, 150);
        SetCompact(PessoaConjugeTelefoneBox, 160);
        SetCompact(PessoaConjugeDataNascimentoBox, 140);
        SetCompact(PessoaEmpresaRuaBox, 300);
        SetCompact(PessoaEmpresaComplementoBox, 220);
        SetCompact(PessoaEmpresaBairroBox, 220);
        SetCompact(PessoaEmpresaCidadeBox, 180);
        SetCompact(PessoaEmpresaCepBox, 120);
        SetCompact(PessoaEmpresaEstadoBox, 80);
        SetCompact(PessoaEmpresaNumeroBox, 90);
        SetCompact(PessoaResponsavelCpfBox, 190);
        SetCompact(PessoaResponsavelRgBox, 150);
        SetCompact(PessoaResponsavelTelefoneBox, 160);
        SetCompact(PessoaResponsavelCepBox, 120);
        SetCompact(PessoaResponsavelEstadoBox, 80);
        SetCompact(PessoaResponsavelNumeroBox, 90);
        SetCompact(PessoaResponsavelDataNascimentoBox, 140);
        SetCompact(PessoaTelefoneEmpresaTrabalhoBox, 160);
        SetCompact(PessoaResponsavelTelefoneEmpresaTrabalhoBox, 160);
        EnsureFieldLabel(PessoaConjugeCpfBox, "CPF");
    }

    private static void SetCompact(Control control, double width)
    {
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
    }

    private static void EnsureFieldLabel(Control control, string labelText)
    {
        if (control.Parent is not StackPanel parent)
        {
            return;
        }

        var label = parent.Children.OfType<TextBlock>().FirstOrDefault();
        if (label is null)
        {
            parent.Children.Insert(0, new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold });
        }
        else
        {
            label.Text = labelText;
            label.Visibility = Visibility.Visible;
        }

        control.Margin = new Thickness(0, 6, 0, 0);
    }
}
