namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private static string GetPessoaRolesLabel(bool isProprietario, bool isLocatario, bool isFiador)
    {
        var roles = new List<string>();
        if (isProprietario) roles.Add("Proprietário");
        if (isLocatario) roles.Add("Locatário");
        if (isFiador) roles.Add("Fiador");
        return roles.Count == 0 ? "-" : string.Join(", ", roles);
    }

    private static string GetPessoaDocumentoTipoLabel(string tipo) =>
        tipo switch
        {
            "cpf" => "CPF",
            "rg" => "RG",
            "comprovante_residencia" => "Comprovante de residência",
            "comprovante_renda" => "Comprovante de renda",
            "estado_civil" => "Comprovante de estado civil",
            "contrato_social" => "Contrato social",
            "cartao_cnpj" => "Cartão CNPJ",
            "procuracao" => "Procuração/autorização",
            "dados_bancarios" => "Dados bancários",
            _ => "Outros"
        };

    private static string GetDocumentoDeLabel(string documentoDe) =>
        documentoDe switch
        {
            "conjuge" => "Cônjuge",
            "empresa_trabalho" => "Empresa onde trabalha",
            _ => "Pessoa"
        };
}
