namespace Monthoya.Core.Entities;

public sealed class AgenciaPerfil : BaseEntity
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Creci { get; set; }

    public string? ResponsavelNome { get; set; }
    public string? ResponsavelCpf { get; set; }
    public string? ResponsavelCargo { get; set; }

    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Site { get; set; }

    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }

    public string? DadosBancarios { get; set; }
    public string? TextoPadraoRodape { get; set; }
    public string? Observacoes { get; set; }
}
