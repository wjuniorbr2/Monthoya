using Monthoya.Core.Entities;

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

    private static string GetImovelPublicacaoLabel(Imovel imovel) =>
        GetImovelPublicacaoLabel(imovel.PublicarNoSite, imovel.PublicarNoApp, imovel.Destaque);

    private static string GetImovelPublicacaoLabel(bool publicarNoSite, bool publicarNoApp, bool destaque)
    {
        if (publicarNoSite && publicarNoApp)
        {
            return destaque ? "Site/App - destaque" : "Site/App";
        }

        if (publicarNoSite)
        {
            return destaque ? "Site - destaque" : "Site";
        }

        if (publicarNoApp)
        {
            return destaque ? "App - destaque" : "App";
        }

        return "Privado";
    }

    private static string GetImovelFinalidadeLabel(ImovelFinalidade finalidade) =>
        finalidade switch
        {
            ImovelFinalidade.Locacao => "Locação",
            ImovelFinalidade.Venda => "Venda",
            ImovelFinalidade.Ambos => "Ambos",
            _ => finalidade.ToString()
        };

    private static string GetImovelStatusLabel(ImovelStatus status) =>
        status switch
        {
            ImovelStatus.Disponivel => "Disponível",
            ImovelStatus.Reservado => "Reservado",
            ImovelStatus.Locado => "Locado",
            ImovelStatus.Vendido => "Vendido",
            ImovelStatus.Inativo => "Inativo",
            _ => status.ToString()
        };

    private static string GetImovelChavePosseLabel(ImovelChavePosse posse) =>
        posse switch
        {
            ImovelChavePosse.NaoCadastrada => "Sem chave",
            ImovelChavePosse.Imobiliaria => "Na imobiliária",
            ImovelChavePosse.Proprietario => "Com proprietário",
            ImovelChavePosse.Locatario => "Com locatário",
            ImovelChavePosse.Terceiro => "Com terceiro",
            ImovelChavePosse.Outro => "Outro",
            _ => posse.ToString()
        };

    private static string GetImovelChaveMovimentoTipoLabel(ImovelChaveMovimentoTipo tipo) =>
        tipo switch
        {
            ImovelChaveMovimentoTipo.Retirada => "Retirada",
            ImovelChaveMovimentoTipo.Devolucao => "Devolução",
            ImovelChaveMovimentoTipo.Transferencia => "Transferência",
            ImovelChaveMovimentoTipo.MarcadaPerdida => "Perdida",
            ImovelChaveMovimentoTipo.Outro => "Outro",
            _ => tipo.ToString()
        };

    private static string? MergeNotes(string? current, string? added)
    {
        if (string.IsNullOrWhiteSpace(added))
        {
            return TrimOrNull(current);
        }

        if (string.IsNullOrWhiteSpace(current))
        {
            return added.Trim();
        }

        return $"{current.Trim()}{Environment.NewLine}{added.Trim()}";
    }

    private static string GetImovelMediaCategoryLabel(ImovelMediaCategory category) =>
        category switch
        {
            ImovelMediaCategory.PropertyPhoto => "Foto pública do imóvel",
            ImovelMediaCategory.Document => "Documento",
            ImovelMediaCategory.InspectionPhoto => "Foto de vistoria",
            ImovelMediaCategory.MaintenancePhoto => "Foto de manutenção",
            ImovelMediaCategory.Other => "Outro",
            _ => category.ToString()
        };

    private static string GetImovelMediaSourceLabel(ImovelMediaSource source) =>
        source switch
        {
            ImovelMediaSource.Windows => "Windows",
            ImovelMediaSource.AndroidStaff => "Android equipe",
            ImovelMediaSource.Website => "Site",
            ImovelMediaSource.Import => "Importação",
            _ => source.ToString()
        };

    private static string GetVistoriaTipoLabel(VistoriaTipo tipo) =>
        tipo switch
        {
            VistoriaTipo.InicialProprietario => "Inicial do proprietário",
            VistoriaTipo.Entrada => "Entrada da locação",
            VistoriaTipo.Saida => "Saída da locação",
            VistoriaTipo.Periodica => "Periódica",
            VistoriaTipo.Manutencao => "Manutenção",
            VistoriaTipo.Outros => "Outra",
            _ => tipo.ToString()
        };

    private static string GetVistoriaStatusLabel(VistoriaStatus status) =>
        status switch
        {
            VistoriaStatus.Draft => "Rascunho",
            VistoriaStatus.InProgress => "Em andamento",
            VistoriaStatus.ReadyToReview => "Pronta para revisão",
            VistoriaStatus.Finished => "Finalizada",
            VistoriaStatus.SignedPaper => "Assinada em papel",
            VistoriaStatus.SignedDigitally => "Assinada digitalmente",
            VistoriaStatus.Canceled => "Cancelada",
            _ => status.ToString()
        };
}

