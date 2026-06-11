using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
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
}
