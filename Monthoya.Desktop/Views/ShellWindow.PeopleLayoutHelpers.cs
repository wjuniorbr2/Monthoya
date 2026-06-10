using System.Windows;
using System.Windows.Controls;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static DatePicker NewDatePicker() => new()
    {
        Width = 140,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 6, 0, 0),
        Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR"),
        SelectedDateFormat = DatePickerFormat.Short
    };

    private static TextBlock SectionHeader(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 8)
    };

    private static DockPanel AddressHeader(string text, Control cepControl)
    {
        RemoveFromCurrentParent(cepControl);
        cepControl.Width = 120;
        cepControl.HorizontalAlignment = HorizontalAlignment.Left;
        cepControl.Margin = new Thickness(8, 0, 0, 0);

        var header = new DockPanel
        {
            Tag = text,
            LastChildFill = false,
            Margin = new Thickness(0, 8, 0, 8)
        };
        header.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        header.Children.Add(new TextBlock
        {
            Text = "CEP",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        });
        header.Children.Add(cepControl);
        return header;
    }

    private static void ReplaceSectionHeaderWithCep(StackPanel parent, string sectionText, Control cepControl)
    {
        RemoveTextBlock(parent, "CEP");
        RemoveTextBlock(parent, "CEP da empresa");
        RemoveTextBlock(parent, "CEP do responsável");
        var index = parent.Children
            .OfType<TextBlock>()
            .Select(block => new { Block = block, Index = parent.Children.IndexOf(block) })
            .FirstOrDefault(x => x.Block.Text == sectionText)?.Index;
        if (index is null)
        {
            return;
        }

        parent.Children.RemoveAt(index.Value);
        parent.Children.Insert(index.Value, AddressHeader(sectionText, cepControl));
    }

    private static WrapPanel WrapFields(params (string Label, Control Control, double Width)[] fields)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (var field in fields)
        {
            row.Children.Add(FieldStack(field.Label, field.Control, field.Width));
        }

        return row;
    }

    private static StackPanel FieldStack(string labelText, Control control, double width)
    {
        RemoveFromCurrentParent(control);
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.Margin = new Thickness(0, 6, 0, 0);
        return new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 14, 12),
            Children =
            {
                new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold },
                control
            }
        };
    }

    private sealed record EnumOption<T>(T Value, string Label);
    private static void RemoveFromCurrentParent(UIElement element)
    {
        if (element is FrameworkElement { Parent: Panel parentPanel })
        {
            parentPanel.Children.Remove(element);
            return;
        }

        if (element is FrameworkElement { Parent: ContentControl contentControl }
            && ReferenceEquals(contentControl.Content, element))
        {
            contentControl.Content = null;
            return;
        }

        if (element is FrameworkElement { Parent: Decorator decorator }
            && ReferenceEquals(decorator.Child, element))
        {
            decorator.Child = null;
        }
    }

    private static void InsertSectionHeader(StackPanel parent, string text, int index)
    {
        parent.Children.Insert(Math.Min(index, parent.Children.Count), SectionHeader(text));
    }

    private static void InsertSectionHeaderBefore(StackPanel parent, string beforeLabel, string sectionText)
    {
        var index = parent.Children
            .OfType<TextBlock>()
            .Select(block => new { Block = block, Index = parent.Children.IndexOf(block) })
            .FirstOrDefault(x => x.Block.Text == beforeLabel)?.Index ?? parent.Children.Count;
        parent.Children.Insert(index, SectionHeader(sectionText));
    }

    private static void RenameSection(StackPanel parent, string oldText, string newText)
    {
        var label = parent.Children.OfType<TextBlock>().FirstOrDefault(block => block.Text == oldText);
        if (label is not null)
        {
            label.Text = newText;
        }
    }

    private void ArrangePessoaAddressFieldsIntoRows()
    {
        ArrangeAddressFields(PessoaFisicaFieldsPanel, "PessoaResidencialAddressFieldsRow", "ENDEREÇO DE RESIDÊNCIA:",
            ("Rua", PessoaRuaBox, 300),
            ("Número", PessoaNumeroBox, 90),
            ("Complemento", PessoaComplementoBox, 220),
            ("Bairro", PessoaBairroBox, 220),
            ("Estado", PessoaEstadoBox, 80),
            ("Cidade", PessoaCidadeBox, 180));

        ArrangeAddressFields(PessoaJuridicaFieldsPanel, "PessoaEmpresaAddressFieldsRow", "ENDEREÇO DA EMPRESA:",
            ("Rua da empresa", PessoaEmpresaRuaBox, 300),
            ("Número da empresa", PessoaEmpresaNumeroBox, 90),
            ("Complemento da empresa", PessoaEmpresaComplementoBox, 220),
            ("Bairro da empresa", PessoaEmpresaBairroBox, 220),
            ("Estado da empresa", PessoaEmpresaEstadoBox, 80),
            ("Cidade da empresa", PessoaEmpresaCidadeBox, 180));

        ArrangeAddressFields(PessoaJuridicaFieldsPanel, "PessoaResponsavelAddressFieldsRow", "ENDEREÇO DE RESIDÊNCIA:",
            ("Rua do responsável", PessoaResponsavelRuaBox, 300),
            ("Número do responsável", PessoaResponsavelNumeroBox, 90),
            ("Complemento do responsável", PessoaResponsavelComplementoBox, 220),
            ("Bairro do responsável", PessoaResponsavelBairroBox, 220),
            ("Estado do responsável", PessoaResponsavelEstadoBox, 80),
            ("Cidade do responsável", PessoaResponsavelCidadeBox, 180));
    }

    private static void ArrangeAddressFields(StackPanel parent, string rowTag, string sectionText, params (string Label, Control Control, double Width)[] fields)
    {
        if (parent.Children.OfType<WrapPanel>().Any(panel => panel.Tag as string == rowTag))
        {
            return;
        }

        var row = new WrapPanel
        {
            Tag = rowTag,
            Margin = new Thickness(0, 0, 0, 12)
        };

        foreach (var field in fields)
        {
            MoveFieldToWrapPanel(parent, row, field.Label, field.Control, field.Width);
        }

        if (row.Children.Count == 0)
        {
            return;
        }

        var insertIndex = 0;
        for (var index = 0; index < parent.Children.Count; index++)
        {
            if ((parent.Children[index] is TextBlock block
                    && block.Text.Equals(sectionText, StringComparison.OrdinalIgnoreCase))
                || (parent.Children[index] is FrameworkElement element
                    && element.Tag as string == sectionText))
            {
                insertIndex = index + 1;
                break;
            }
        }

        parent.Children.Insert(insertIndex, row);
    }

    private static StackPanel MoveFieldToWrapPanel(StackPanel source, WrapPanel target, string labelText, Control control, double width)
    {
        var label = RemoveTextBlock(source, labelText) ?? new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold };
        RemoveFromCurrentParent(label);
        RemoveFromCurrentParent(control);
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.Margin = new Thickness(0, 6, 0, 0);

        var field = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 14, 12)
        };
        field.Children.Add(label);
        field.Children.Add(control);
        target.Children.Add(field);
        return field;
    }

    private static StackPanel AddTopCell(Grid grid, int column, TextBlock label, UIElement control, double leftMargin = 0)
    {
        var stack = new StackPanel { Margin = new Thickness(leftMargin, 0, 0, 0) };
        stack.Children.Add(label);
        stack.Children.Add(control);
        Grid.SetColumn(stack, column);
        grid.Children.Add(stack);
        return stack;
    }
}

