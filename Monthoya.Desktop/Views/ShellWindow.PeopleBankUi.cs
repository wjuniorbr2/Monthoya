using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static ComboBox NewBankComboBox()
    {
        var comboBox = new ComboBox
        {
            Width = 290,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = true,
            IsTextSearchEnabled = false,
            StaysOpenOnEdit = true,
            ItemsSource = BrazilianBankCatalog
        };
        TextSearch.SetTextPath(comboBox, nameof(BankOption.SearchText));
        return comboBox;
    }

    private static Button NewSmallBankActionButton(string content, RoutedEventHandler clickHandler)
    {
        var button = new Button
        {
            Content = content,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 6, 8, 0),
            Padding = new Thickness(10, 4, 10, 4),
            MinHeight = 28
        };
        button.Click += clickHandler;
        return button;
    }

    private static void AttachBankSelection(ComboBox bankBox, TextBox codigoBox, TextBox nomeBox)
    {
        bankBox.DropDownOpened += (_, _) =>
        {
            if (GetEditableComboTextBox(bankBox) is TextBox { Text.Length: 0 })
            {
                bankBox.ItemsSource = BrazilianBankCatalog;
            }
        };

        bankBox.Loaded += (_, _) =>
        {
            bankBox.ApplyTemplate();
            if (GetEditableComboTextBox(bankBox) is not TextBox textBox)
            {
                return;
            }

            textBox.TextChanged += (_, _) =>
            {
                if (bankBox.SelectedItem is BankOption selectedBank
                    && textBox.Text.Equals(selectedBank.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (bankBox.IsKeyboardFocusWithin)
                {
                    var query = textBox.Text;
                    ApplyBankSearch(bankBox, query);
                    bankBox.Dispatcher.BeginInvoke(() =>
                    {
                        if (GetEditableComboTextBox(bankBox) is not { } editableTextBox
                            || editableTextBox.Text == query)
                        {
                            return;
                        }

                        editableTextBox.Text = query;
                        editableTextBox.CaretIndex = editableTextBox.Text.Length;
                    }, DispatcherPriority.Background);
                }
            };
        };

        bankBox.SelectionChanged += (_, _) =>
        {
            if (bankBox.SelectedItem is BankOption bank)
            {
                codigoBox.Text = bank.Code;
                nomeBox.Text = bank.Name;
            }
        };
    }

    private static TextBox? GetEditableComboTextBox(ComboBox comboBox) =>
        comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;

    private static void ApplyBankSearch(ComboBox bankBox, string searchText)
    {
        var query = searchText.Trim();
        if (query.Length == 0)
        {
            bankBox.ItemsSource = BrazilianBankCatalog;
            return;
        }

        var normalizedQuery = NormalizeBankSearchText(query);
        var matches = BrazilianBankCatalog
            .Where(bank => bank.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                           || NormalizeBankSearchText(bank.Name).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                           || NormalizeBankSearchText(bank.ToString()).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                           || NormalizeBankSearchText(bank.SearchText).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        bankBox.ItemsSource = matches;

        bankBox.IsDropDownOpen = true;
    }

    private static string NormalizeBankSearchText(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static ComboBox NewContaTipoComboBox() => new()
    {
        Width = 130,
        Margin = new Thickness(0, 6, 0, 0),
        ItemsSource = new EnumOption<Monthoya.Core.Entities.ContaBancariaTipo>[]
        {
            new(Monthoya.Core.Entities.ContaBancariaTipo.Corrente, "Corrente"),
            new(Monthoya.Core.Entities.ContaBancariaTipo.Poupanca, "Poupança"),
            new(Monthoya.Core.Entities.ContaBancariaTipo.Pagamento, "Pagamento"),
            new(Monthoya.Core.Entities.ContaBancariaTipo.Outro, "Outro")
        },
        SelectedValuePath = "Value",
        DisplayMemberPath = "Label",
        SelectedIndex = -1
    };

    private static ComboBox NewPixTipoComboBox() => new()
    {
        Width = 130,
        Margin = new Thickness(0, 6, 0, 0),
        ItemsSource = new EnumOption<Monthoya.Core.Entities.PixChaveTipo>[]
        {
            new(Monthoya.Core.Entities.PixChaveTipo.Cpf, "CPF"),
            new(Monthoya.Core.Entities.PixChaveTipo.Cnpj, "CNPJ"),
            new(Monthoya.Core.Entities.PixChaveTipo.Email, "E-mail"),
            new(Monthoya.Core.Entities.PixChaveTipo.Telefone, "Telefone"),
            new(Monthoya.Core.Entities.PixChaveTipo.Aleatoria, "Aleatória"),
            new(Monthoya.Core.Entities.PixChaveTipo.Outro, "Outro")
        },
        SelectedValuePath = "Value",
        DisplayMemberPath = "Label",
        SelectedIndex = -1
    };

    private static ComboBox NewRepassePreferencialComboBox() => new()
    {
        Width = 160,
        Margin = new Thickness(0, 6, 0, 0),
        ItemsSource = new EnumOption<Monthoya.Core.Entities.MetodoRepassePreferencial>[]
        {
            new(Monthoya.Core.Entities.MetodoRepassePreferencial.Pix, "PIX"),
            new(Monthoya.Core.Entities.MetodoRepassePreferencial.TransferenciaBancaria, "Transferência"),
            new(Monthoya.Core.Entities.MetodoRepassePreferencial.Manual, "Manual")
        },
        SelectedValuePath = "Value",
        DisplayMemberPath = "Label",
        SelectedIndex = -1
    };

    private static StackPanel CreateBankSection(
        TextBox observacoes,
        ComboBox banco,
        TextBox agenciaNumero,
        TextBox agenciaDigito,
        TextBox contaNumero,
        TextBox contaDigito,
        ComboBox contaTipo,
        TextBox titularNome,
        TextBox titularDocumento,
        ComboBox pixTipo,
        TextBox pixChave,
        ComboBox repassePreferencial,
        Button usarDadosButton,
        Button usarPixButton)
    {
        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 6),
            Children = { usarDadosButton, usarPixButton }
        };

        return new StackPanel
        {
            Tag = "PessoaBankFields",
            Margin = new Thickness(0, 10, 0, 12),
            Children =
            {
                SectionHeader("DADOS BANCÁRIOS / PIX:"),
                actionRow,
                WrapFields(
                    ("Banco", banco, 290),
                    ("Agência", agenciaNumero, 100),
                    ("Dígito ag.", agenciaDigito, 70),
                    ("Conta", contaNumero, 130),
                    ("Dígito conta", contaDigito, 80),
                    ("Tipo de conta", contaTipo, 130),
                    ("Titular", titularNome, 220),
                    ("CPF/CNPJ titular", titularDocumento, 170),
                    ("Tipo PIX", pixTipo, 130),
                    ("Chave PIX", pixChave, 220),
                    ("Repasse preferencial", repassePreferencial, 160),
                    ("Observações bancárias", observacoes, 360))
            }
        };
    }

    private sealed record BankOption(string Code, string Name)
    {
        public string SearchText => $"{Code} {Name}";
        public override string ToString() => $"{Code} - {Name}";
    }

    private static readonly BankOption[] BrazilianBankCatalog =
    [
        new("001", "Banco do Brasil"),
        new("003", "Banco da Amazônia"),
        new("004", "Banco do Nordeste"),
        new("021", "Banestes"),
        new("033", "Santander"),
        new("041", "Banrisul"),
        new("070", "BRB"),
        new("077", "Banco Inter"),
        new("104", "Caixa Econômica Federal"),
        new("121", "Banco Agibank"),
        new("136", "Unicred"),
        new("197", "Stone"),
        new("208", "BTG Pactual"),
        new("212", "Banco Original"),
        new("237", "Bradesco"),
        new("260", "Nubank"),
        new("290", "PagBank"),
        new("318", "Banco BMG"),
        new("320", "China Construction Bank Brasil"),
        new("323", "Mercado Pago"),
        new("336", "Banco C6"),
        new("341", "Itaú Unibanco"),
        new("422", "Banco Safra"),
        new("623", "Banco PAN"),
        new("633", "Banco Rendimento"),
        new("655", "Banco Votorantim"),
        new("707", "Banco Daycoval"),
        new("735", "Banco Neon"),
        new("739", "Banco Cetelem"),
        new("748", "Sicredi"),
        new("756", "Sicoob")
    ];
}


