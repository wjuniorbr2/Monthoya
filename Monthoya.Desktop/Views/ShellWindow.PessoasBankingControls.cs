using System.Collections.Generic;
using System.Windows.Controls;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private IEnumerable<ComboBox> GetPessoaBankComboBoxes()
    {
        foreach (var comboBox in new[]
        {
            _pessoaBancoBox,
            _pessoaContaTipoBox,
            _pessoaPixTipoBox,
            _pessoaRepassePreferencialBox,
            _pessoaResponsavelBancoBox,
            _pessoaResponsavelContaTipoBox,
            _pessoaResponsavelPixTipoBox,
            _pessoaResponsavelRepassePreferencialBox
        })
        {
            if (comboBox is not null)
            {
                yield return comboBox;
            }
        }
    }

    private IEnumerable<Button> GetPessoaBankActionButtons()
    {
        foreach (var button in new[]
        {
            _pessoaUsarDadosPessoaBancoButton,
            _pessoaUsarPixButton,
            _pessoaUsarDadosResponsavelBancoButton,
            _pessoaResponsavelUsarPixButton
        })
        {
            if (button is not null)
            {
                yield return button;
            }
        }
    }
}
