using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private static void ValidateImovelRequest(CreateImovelRequest request)
    {
        if (request.ProprietarioId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um proprietário.");
        }

        if (string.IsNullOrWhiteSpace(request.Rua))
        {
            throw new InvalidOperationException("Informe a rua do imóvel.");
        }
    }

    private static void ApplyImovelRequest(Imovel imovel, CreateImovelRequest request, Guid proprietarioId)
    {
        imovel.ProprietarioId = proprietarioId;
        imovel.Rua = request.Rua.Trim();
        imovel.Numero = TrimOrNull(request.Numero);
        imovel.Complemento = TrimOrNull(request.Complemento);
        imovel.Bairro = TrimOrNull(request.Bairro);
        imovel.Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? "Paranavaí" : request.Cidade.Trim();
        imovel.Estado = string.IsNullOrWhiteSpace(request.Estado) ? "PR" : request.Estado.Trim().ToUpperInvariant();
        imovel.Cep = TrimOrNull(request.Cep);
        imovel.SaneparMatricula = TrimOrNull(request.SaneparMatricula);
        imovel.CopelMatricula = TrimOrNull(request.CopelMatricula);
        imovel.IptuInscricaoImobiliaria = TrimOrNull(request.IptuInscricaoImobiliaria);
        imovel.IptuCadastroImovel = TrimOrNull(request.IptuCadastroImovel);
        imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);
        imovel.TipoImovel = TrimOrNull(request.TipoImovel);
        imovel.Descricao = TrimOrNull(request.Descricao);
        imovel.DescricaoInterna = TrimOrNull(request.DescricaoInterna) ?? TrimOrNull(request.Descricao);
        imovel.DescricaoPublica = TrimOrNull(request.DescricaoPublica);
        imovel.ValorAluguel = request.ValorAluguel;
        imovel.ValorVenda = request.ValorVenda;
        imovel.ValorCondominio = request.ValorCondominio;
        imovel.ValorIptu = request.ValorIptu;
        imovel.Finalidade = request.Finalidade;
        imovel.Status = request.Status;
        imovel.Latitude = request.Latitude;
        imovel.Longitude = request.Longitude;
        imovel.Quartos = request.Quartos;
        imovel.Suites = request.Suites;
        imovel.Banheiros = request.Banheiros;
        imovel.VagasGaragem = request.VagasGaragem;
        imovel.Salas = request.Salas;
        imovel.Cozinhas = request.Cozinhas;
        imovel.Copas = request.Copas;
        imovel.Despensas = request.Despensas;
        imovel.Lavanderias = request.Lavanderias;
        imovel.AreasServico = request.AreasServico;
        imovel.Lavabos = request.Lavabos;
        imovel.Sacadas = request.Sacadas;
        imovel.Churrasqueiras = request.Churrasqueiras;
        imovel.Piscinas = request.Piscinas;
        imovel.Quintais = request.Quintais;
        imovel.HallsEntrada = request.HallsEntrada;
        imovel.Estendais = request.Estendais;
        imovel.AreaConstruida = request.AreaConstruida;
        imovel.AreaTerreno = request.AreaTerreno;
        imovel.Mobiliado = request.Mobiliado;
        imovel.AceitaPets = request.AceitaPets;
        imovel.PublicarNoSite = request.PublicarNoSite;
        imovel.PublicarNoApp = request.PublicarNoApp;
        imovel.Destaque = request.Destaque;
        imovel.MostrarEnderecoCompletoPublicamente = request.MostrarEnderecoCompletoPublicamente;
        imovel.ModoExibicaoEnderecoPublico = request.ModoExibicaoEnderecoPublico;
        imovel.ChavePosse = request.ChavePosse;
        imovel.ChaveCodigo = TrimOrNull(request.ChaveCodigo);
        imovel.ChaveQuemTem = TrimOrNull(request.ChaveQuemTem);
        imovel.ChaveTelefone = DigitsOrNull(request.ChaveTelefone);
        imovel.ChaveContatoNome = TrimOrNull(request.ChaveContatoNome);
        imovel.ChaveContatoDocumento = TrimOrNull(request.ChaveContatoDocumento);
        imovel.ChaveLocalRetirada = TrimOrNull(request.ChaveLocalRetirada);
        imovel.ChaveMelhorHorario = TrimOrNull(request.ChaveMelhorHorario);
        imovel.ChaveAutorizacaoNecessaria = request.ChaveAutorizacaoNecessaria;
        imovel.ChaveObservacoes = TrimOrNull(request.ChaveObservacoes);
        imovel.Observacoes = TrimOrNull(request.Observacoes);
    }

}

