using Monthoya.Core.Entities;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private sealed class ImoveisPageState : IShellPageState
    {
        public string SearchText { get; init; } = string.Empty;
        public Guid? SelectedImovelId { get; init; }
        public Guid? ProprietarioId { get; init; }
        public ImovelFinalidade Finalidade { get; init; } = ImovelFinalidade.Locacao;
        public string Rua { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public string Complemento { get; init; } = string.Empty;
        public string Bairro { get; init; } = string.Empty;
        public string Cidade { get; init; } = "Paranava\u00ED";
        public string Estado { get; init; } = "PR";
        public string Cep { get; init; } = string.Empty;
        public string TipoImovel { get; init; } = string.Empty;
        public string Sanepar { get; init; } = string.Empty;
        public string Copel { get; init; } = string.Empty;
        public string IptuInscricaoImobiliaria { get; init; } = string.Empty;
        public string IptuCadastroImovel { get; init; } = string.Empty;
        public string ColetaLixo { get; init; } = string.Empty;
        public string ValorAluguel { get; init; } = string.Empty;
        public string ValorVenda { get; init; } = string.Empty;
        public string ValorCondominio { get; init; } = string.Empty;
        public string ValorIptu { get; init; } = string.Empty;
        public string Latitude { get; init; } = string.Empty;
        public string Longitude { get; init; } = string.Empty;
        public ImovelStatus Status { get; init; } = ImovelStatus.Disponivel;
        public string Quartos { get; init; } = string.Empty;
        public string Suites { get; init; } = string.Empty;
        public string Banheiros { get; init; } = string.Empty;
        public string Vagas { get; init; } = string.Empty;
        public string AreaConstruida { get; init; } = string.Empty;
        public string AreaTerreno { get; init; } = string.Empty;
        public bool? Mobiliado { get; init; } = false;
        public bool? AceitaPets { get; init; } = false;
        public string Descricao { get; init; } = string.Empty;
        public string DescricaoPublica { get; init; } = string.Empty;
        public string Observacoes { get; init; } = string.Empty;
        public bool PublicarSite { get; init; }
        public bool PublicarApp { get; init; }
        public bool Destaque { get; init; }
        public bool MostrarEnderecoCompleto { get; init; }
        public ImovelEnderecoPublicoModo ModoEnderecoPublico { get; init; } = ImovelEnderecoPublicoModo.BairroCidade;
        public ImovelChavePosse ChavePosse { get; init; } = ImovelChavePosse.NaoCadastrada;
        public string ChaveCodigo { get; init; } = string.Empty;
        public string ChaveQuemTem { get; init; } = string.Empty;
        public string ChaveTelefone { get; init; } = string.Empty;
        public string ChaveContatoNome { get; init; } = string.Empty;
        public string ChaveContatoDocumento { get; init; } = string.Empty;
        public string ChaveLocal { get; init; } = string.Empty;
        public string ChaveHorario { get; init; } = string.Empty;
        public bool ChaveAutorizacao { get; init; }
        public string ChaveObservacoes { get; init; } = string.Empty;

        public static ImoveisPageState Default { get; } = new();
    }
}