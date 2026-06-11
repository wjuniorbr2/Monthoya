using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private static CreateImovelRequest ToImovelRequest(Imovel imovel) =>
        new(
            imovel.ProprietarioId,
            imovel.Rua,
            imovel.Numero,
            imovel.Bairro,
            imovel.Cidade,
            imovel.Estado,
            imovel.ValorAluguel,
            imovel.Finalidade,
            imovel.Observacoes,
            imovel.Complemento,
            imovel.Cep,
            imovel.SaneparMatricula,
            imovel.CopelMatricula,
            imovel.IptuInscricaoImobiliaria,
            imovel.IptuCadastroImovel,
imovel.ColetaLixo,
            imovel.TipoImovel,
            imovel.Descricao,
            imovel.ValorVenda,
            imovel.Latitude,
            imovel.Longitude,
            imovel.Status,
            imovel.ValorCondominio,
            imovel.ValorIptu,
            imovel.Quartos,
            imovel.Suites,
            imovel.Banheiros,
            imovel.VagasGaragem,
            imovel.AreaConstruida,
            imovel.AreaTerreno,
            imovel.Mobiliado,
            imovel.AceitaPets,
            imovel.DescricaoInterna,
            imovel.DescricaoPublica,
            imovel.PublicarNoSite,
            imovel.PublicarNoApp,
            imovel.Destaque,
            imovel.MostrarEnderecoCompletoPublicamente,
            imovel.ModoExibicaoEnderecoPublico,
            imovel.ChavePosse,
            imovel.ChaveCodigo,
            imovel.ChaveQuemTem,
            FormatPhoneForDisplay(imovel.ChaveTelefone),
            imovel.ChaveContatoNome,
            imovel.ChaveContatoDocumento,
            imovel.ChaveLocalRetirada,
            imovel.ChaveMelhorHorario,
            imovel.ChaveAutorizacaoNecessaria,
            imovel.ChaveObservacoes);
}
