using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static string GetRoleLabel(UserRole role) =>
        role switch
        {
            UserRole.Usuario => "Usuário",
            UserRole.Administrador => "Administrador",
            UserRole.Desenvolvedor => "Desenvolvedor",
            _ => role.ToString()
        };

    private static string GetAccessLabel(UserAccess access) =>
        access.HasFlag(UserAccess.UserManagement)
            ? "Cadastro de usuários"
            : "Básico";

    private static bool IsGenericModulePage(ShellPage page) =>
        page is ShellPage.Locacoes
            or ShellPage.Financeiro
            or ShellPage.Boletos
            or ShellPage.NotasFiscais
            or ShellPage.Documentos
            or ShellPage.Relatorios
            or ShellPage.Dimob
            or ShellPage.Manutencoes
            or ShellPage.Vistorias
            or ShellPage.Configuracoes;

    private static ModuleDefinition GetModuleDefinition(ShellPage page) =>
        page switch
        {
            ShellPage.Locacoes => new("Locações", "Contratos de locação vinculados a imóvel, proprietário, locatário e fiadores.", "Fundação criada. O cadastro completo de locação deve validar imóvel, locatário, proprietário, fiadores, reajuste e taxas antes de ativar.", "Nova locação"),
            ShellPage.Financeiro => new("Financeiro", "Lançamentos financeiros, contas a pagar e contas a receber.", "Fundação criada para aluguel, manutenção, taxas, descontos, multa, juros, administração, boleto, nota fiscal e outros.", "Novo lançamento"),
            ShellPage.Boletos => new("Boletos", "Controle interno de boletos vinculados a locações e lançamentos.", "Integração bancária ainda não configurada. As ações Gerar, Registrar, Cancelar, Baixar PDF e Consultar status ficam preparadas para provider futuro.", "Ações do boleto"),
            ShellPage.NotasFiscais => new("Notas Fiscais", "Fluxo manual/semi-manual de NFS-e para registrar dados emitidos no portal municipal.", "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual e registre número, código de verificação, PDF/XML e status.", "Ações de NFS-e"),
            ShellPage.Documentos => new("Documentos", "Modelos e documentos gerados em PDF.", "Modelos iniciais foram criados como pendentes de revisão. Não use redação jurídica como definitiva sem validação do cliente.", "Novo documento"),
            ShellPage.Relatorios => new("Relatórios", "Consultas operacionais de aluguéis, imóveis, locações e contas.", "Relatórios oficiais e exportações finais serão detalhados conforme os dados reais e decisões do cliente.", "Gerar relatório"),
            ShellPage.Dimob => new("DIMOB", "Fundação para conferência anual de dados da DIMOB.", "Exportação TXT oficial pendente de confirmação do layout vigente da Receita Federal/PGD/Receitanet.", "Exportar DIMOB"),
            ShellPage.Manutencoes => new("Manutenções", "Solicitações e execução de manutenção de imóveis.", "Fundação criada com status solicitada, em andamento, concluída e cancelada.", "Nova manutenção"),
            ShellPage.Chaves => new("Chaves", "Controle de retirada, devolução e atraso de chaves dos imóveis.", "Retiradas vencidas continuam em atraso até devolução/fechamento. A tela detalhada de lançamento será separada de Imóveis.", "Nova movimentação"),
            ShellPage.Vistorias => new("Vistorias", "Vistorias de entrada, saída, periódicas e outras.", "Fundação criada. Anexos, fotos e laudos em PDF devem usar o módulo de documentos.", "Nova vistoria"),
            ShellPage.Configuracoes => new("Configurações", "Índices de reajuste, certificado A1 e integrações futuras.", "Certificados digitais: registrar somente metadados agora. TODO: armazenamento criptografado, senha segura, auditoria, alertas e ambiente homologação/produção.", "Abrir configurações"),
            _ => new("Módulo", "Fundação do módulo.", "Sem ações disponíveis.", "Abrir")
        };

    private const string MapHtmlTemplate = """
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
  <style>
    html, body, #map { height: 100%; margin: 0; font-family: Segoe UI, Arial, sans-serif; }
    .empty { position: absolute; z-index: 999; top: 18px; left: 68px; width: min(560px, calc(100% - 92px)); background: white; border: 1px solid #dbe8e2; border-radius: 8px; padding: 12px 14px; color: #66756f; box-shadow: 0 10px 30px rgba(20,37,33,.08); }
  </style>
</head>
<body>
  <div id="map"></div>
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
  <script>
    const properties = __PROPERTIES__;
    const map = L.map('map', { zoomControl: true }).setView([-23.0816, -52.4617], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    if (!properties.length) {
      const empty = document.createElement('div');
      empty.className = 'empty';
      empty.textContent = 'Nenhum imóvel disponível para locação com coordenadas cadastrado ainda.';
      document.body.appendChild(empty);
    }

    const markerGroup = L.featureGroup().addTo(map);
    properties.forEach(property => {
      const marker = L.marker([property.Latitude, property.Longitude]).addTo(markerGroup);
      marker.bindPopup(`<strong>${property.Code}</strong><br>${property.AddressLine}<br>${property.City} - ${property.State}<br>${property.RentalPrice ?? ''}`);
    });

    if (properties.length) {
      map.fitBounds(markerGroup.getBounds().pad(0.18));
    }
  </script>
</body>
</html>
""";

    private enum ShellPage
    {
        Dashboard,
        Users,
        Pessoas,
        Imoveis,
        Chaves,
        Notificacoes,
        Locacoes,
        Financeiro,
        Boletos,
        NotasFiscais,
        Documentos,
        Relatorios,
        Dimob,
        Manutencoes,
        Vistorias,
        Configuracoes,
        Diagnostics
    }

    private sealed record UserRoleOption(string Label, UserRole Role);
    private sealed record TipoPessoaOption(string Label, TipoPessoa Tipo);
    private sealed record ImovelFinalidadeOption(string Label, ImovelFinalidade Finalidade);
    private sealed record ImovelStatusOption(string Label, ImovelStatus Status);
    private sealed record ImovelFinalidadeFilterOption(string Label, ImovelFinalidade? Finalidade);
    private sealed record ImovelStatusFilterOption(string Label, string Status);
    private sealed record ImovelPublicacaoFilterOption(string Label, string Publicacao);
    private sealed record ImovelChavePosseOption(string Label, ImovelChavePosse Posse);
    private sealed record ImovelEnderecoPublicoModoOption(string Label, ImovelEnderecoPublicoModo Modo);
    private sealed record ImovelMediaCategoryOption(string Label, ImovelMediaCategory Category);
    private sealed record VistoriaTipoOption(string Label, VistoriaTipo Tipo);
    private sealed record VistoriaStatusOption(string Label, VistoriaStatus Status);
    private sealed record ChavesStatusFilterOption(string Label, string Status);
    private sealed record NotificationCategoryOption(string Label, NotificationCategory? Category);
    private sealed record NotificationPriorityOption(string Label, NotificationPriority? Priority);
    private sealed record PessoaDocumentoTipoOption(string Label, string Tipo);
    private sealed record PessoaDocumentoDonoOption(string Label, string Tipo);
    private sealed record PessoaStatusFilterOption(string Label, string Status);
    private sealed record ModuleDefinition(string Title, string Subtitle, string Notice, string ActionText);

    private static readonly IReadOnlyList<UserRoleOption> UserRoleOptions =
    [
        new("Usuário", UserRole.Usuario),
        new("Administrador", UserRole.Administrador),
        new("Desenvolvedor", UserRole.Desenvolvedor)
    ];

    private static readonly IReadOnlyList<TipoPessoaOption> TipoPessoaOptions =
    [
        new("Física", TipoPessoa.Fisica),
        new("Jurídica", TipoPessoa.Juridica)
    ];

    private static readonly IReadOnlyList<ImovelFinalidadeOption> ImovelFinalidadeOptions =
    [
        new("Locação", ImovelFinalidade.Locacao),
        new("Venda", ImovelFinalidade.Venda),
        new("Ambos", ImovelFinalidade.Ambos)
    ];

    private static readonly IReadOnlyList<string> ImovelTipoOptions =
    [
        "Casa", "Apartamento", "Sobrado", "Kitnet", "Sala comercial", "Barracão", "Terreno", "Chácara", "Outro"
    ];

    private static readonly IReadOnlyList<ImovelStatusOption> ImovelStatusOptions =
    [
        new("Disponível", ImovelStatus.Disponivel),
        new("Reservado", ImovelStatus.Reservado),
        new("Locado", ImovelStatus.Locado),
        new("Vendido", ImovelStatus.Vendido),
        new("Inativo", ImovelStatus.Inativo)
    ];

    private static readonly IReadOnlyList<ImovelFinalidadeFilterOption> ImoveisFinalidadeFilterOptions =
    [
        new("Todas finalidades", null),
        new("Locação", ImovelFinalidade.Locacao),
        new("Venda", ImovelFinalidade.Venda),
        new("Ambos", ImovelFinalidade.Ambos)
    ];

    private static readonly IReadOnlyList<ImovelStatusFilterOption> ImoveisStatusFilterOptions =
    [
        new("Ativos", "ativos"),
        new("Disponíveis", "disponivel"),
        new("Reservados", "reservado"),
        new("Locados", "locado"),
        new("Vendidos", "vendido"),
        new("Inativos", "inativo"),
        new("Todos", "todos")
    ];

    private static readonly IReadOnlyList<ImovelPublicacaoFilterOption> ImoveisPublicacaoFilterOptions =
    [
        new("Todas publicações", "todos"),
        new("Privados", "privado"),
        new("Site", "site"),
        new("App", "app"),
        new("Destaques", "destaque")
    ];

    private static readonly IReadOnlyList<ImovelChavePosseOption> ImovelChavePosseOptions =
    [
        new("Sem chave cadastrada", ImovelChavePosse.NaoCadastrada),
        new("Imobiliária está com a chave", ImovelChavePosse.Imobiliaria),
        new("Proprietário está com a chave", ImovelChavePosse.Proprietario),
        new("Locatário está com a chave", ImovelChavePosse.Locatario),
        new("Terceiro está com a chave", ImovelChavePosse.Terceiro),
        new("Outro", ImovelChavePosse.Outro)
    ];

    private static readonly IReadOnlyList<ImovelEnderecoPublicoModoOption> ImovelEnderecoPublicoModoOptions =
    [
        new("Bairro e cidade", ImovelEnderecoPublicoModo.BairroCidade),
        new("Endereço aproximado", ImovelEnderecoPublicoModo.EnderecoAproximado),
        new("Endereço completo", ImovelEnderecoPublicoModo.EnderecoCompleto)
    ];

    private static readonly IReadOnlyList<ImovelMediaCategoryOption> ImovelMediaCategoryOptions =
    [
        new("Foto pública do imóvel", ImovelMediaCategory.PropertyPhoto),
        new("Foto privada", ImovelMediaCategory.Other),
        new("Documento", ImovelMediaCategory.Document),
        new("Foto de vistoria (privada por padrão)", ImovelMediaCategory.InspectionPhoto),
        new("Foto de manutenção", ImovelMediaCategory.MaintenancePhoto)
    ];

    private static readonly IReadOnlyList<VistoriaTipoOption> VistoriaTipoOptions =
    [
        new("Inicial do proprietário", VistoriaTipo.InicialProprietario),
        new("Entrada da locação", VistoriaTipo.Entrada),
        new("Saída da locação", VistoriaTipo.Saida),
        new("Periódica", VistoriaTipo.Periodica),
        new("Manutenção", VistoriaTipo.Manutencao),
        new("Outra", VistoriaTipo.Outros)
    ];

    private static readonly IReadOnlyList<VistoriaTipoOption> ImovelVistoriaTipoOptions =
    [
        new("Inicial do proprietário", VistoriaTipo.InicialProprietario)
    ];

    private static readonly IReadOnlyList<VistoriaStatusOption> VistoriaStatusOptions =
    [
        new("Rascunho", VistoriaStatus.Draft),
        new("Em andamento", VistoriaStatus.InProgress),
        new("Pronta para revisão", VistoriaStatus.ReadyToReview),
        new("Finalizada", VistoriaStatus.Finished),
        new("Assinada em papel", VistoriaStatus.SignedPaper),
        new("Assinada digitalmente", VistoriaStatus.SignedDigitally),
        new("Cancelada", VistoriaStatus.Canceled)
    ];

    private static readonly IReadOnlyList<ChavesStatusFilterOption> ChavesStatusFilterOptions =
    [
        new("Retiradas ativas", "ativas"),
        new("Em atraso", "atraso"),
        new("Devolvidas", "devolvidas"),
        new("Todas", "todas")
    ];

    private static readonly IReadOnlyList<NotificationCategoryOption> NotificationCategoryFilterOptions =
    [
        new("Todas categorias", null),
        new("Mensagem manual", NotificationCategory.ManualMessage),
        new("Alerta do sistema", NotificationCategory.SystemAlert),
        new("Lembrete", NotificationCategory.ScheduledReminder),
        new("Ação necessária", NotificationCategory.TaskRequired),
        new("Informação", NotificationCategory.Info),
        new("Aviso", NotificationCategory.Warning),
        new("Comunicado", NotificationCategory.AdminAnnouncement),
        new("Chave em atraso", NotificationCategory.KeyOverdue)
    ];

    private static readonly IReadOnlyList<NotificationCategoryOption> NotificationCategoryOptions =
        NotificationCategoryFilterOptions.Skip(1).ToList();

    private static readonly IReadOnlyList<NotificationPriorityOption> NotificationPriorityFilterOptions =
    [
        new("Todas prioridades", null),
        new("Baixa", NotificationPriority.Low),
        new("Normal", NotificationPriority.Normal),
        new("Alta", NotificationPriority.High),
        new("Crítica", NotificationPriority.Critical)
    ];

    private static readonly IReadOnlyList<NotificationPriorityOption> NotificationPriorityOptions =
        NotificationPriorityFilterOptions.Skip(1).ToList();

    private static readonly IReadOnlyList<PessoaDocumentoTipoOption> PessoaDocumentoTipoFisicaOptions =
    [
        new("Pessoal", "pessoal"),
        new("Residência", "residencia"),
        new("Trabalho", "trabalho"),
        new("Pessoal do cônjuge", "pessoal_conjuge"),
        new("Trabalho do cônjuge", "trabalho_conjuge"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoTipoOption> PessoaDocumentoTipoJuridicaOptions =
    [
        new("Documentos da empresa", "documentos_empresa"),
        new("Endereço/residência", "endereco_residencia"),
        new("Identificação pessoal", "identificacao_pessoal"),
        new("Receita/Renda", "receita_renda"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoFisicaOptions =
    [
        new("", ""),
        new("Pessoa", "pessoa"),
        new("Trabalho da pessoa", "empresa_trabalho"),
        new("Cônjuge", "conjuge"),
        new("Trabalho do cônjuge", "trabalho_conjuge"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoJuridicaOptions =
    [
        new("", ""),
        new("Empresa", "empresa"),
        new("Responsável", "responsavel"),
        new("Cônjuge do responsável", "conjuge_responsavel"),
        new("Trabalho do cônjuge do responsável", "trabalho_conjuge_responsavel"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaStatusFilterOption> PessoaStatusFilterOptions =
    [
        new("Ativos", "ativo"),
        new("Inativos", "inativo"),
        new("Todos", "todos")
    ];
}