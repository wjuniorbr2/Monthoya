using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed class RentalManagementService(
    MonthoyaDbContext dbContext,
    IDocumentOcrService? documentOcrService = null,
    IFileStorageService? fileStorageService = null) : IRentalManagementService
{
    public async Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default)
    {
        var pessoas = await dbContext.Pessoas
            .AsNoTracking()
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .OrderBy(x => x.NomeDisplay)
            .ToListAsync(cancellationToken);

        var proprietarioSet = (await dbContext.Imoveis
            .AsNoTracking()
            .Where(x => x.Status != ImovelStatus.Inativo)
            .Select(x => x.ProprietarioId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var locatarioSet = (await dbContext.Locacoes
            .AsNoTracking()
            .Where(x => x.Status == LocacaoStatus.Ativa)
            .Select(x => x.LocatarioId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var fiadorSet = (await dbContext.LocacaoFiadores
            .AsNoTracking()
            .Where(x => x.Locacao != null && x.Locacao.Status == LocacaoStatus.Ativa)
            .Select(x => x.FiadorId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        return pessoas.Select(x =>
        {
            var isProprietario = proprietarioSet.Contains(x.Id);
            var isLocatario = locatarioSet.Contains(x.Id);
            var isFiador = fiadorSet.Contains(x.Id);

            return new PessoaSummary(
                x.Id,
                x.NomeDisplay,
                x.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
                GetPessoaRolesLabel(isProprietario, isLocatario, isFiador),
                x.TipoPessoa == TipoPessoa.Fisica ? x.PessoaFisica?.Cpf : x.PessoaJuridica?.Cnpj,
                x.Telefone,
                x.Email,
                x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
                isProprietario,
                isLocatario,
                isFiador);
        }).ToList();
    }

    public async Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default)
    {
        var pessoa = await dbContext.Pessoas
            .AsNoTracking()
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken);

        if (pessoa is null)
        {
            return null;
        }

        var summary = (await GetPessoasAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
        return new PessoaDetails(summary, ToPessoaRequest(pessoa));
    }

    public async Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }

        var pessoa = new Pessoa
        {
            TipoPessoa = request.TipoPessoa,
            NomeDisplay = request.NomeDisplay.Trim(),
            Telefone = request.Telefone?.Trim(),
            Email = request.Email?.Trim(),
            Observacoes = request.Observacoes?.Trim()
        };

        if (request.TipoPessoa == TipoPessoa.Fisica)
        {
            pessoa.PessoaFisica = new PessoaFisica
            {
                Nome = pessoa.NomeDisplay,
                Rua = TrimOrNull(request.Rua),
                Numero = TrimOrNull(request.Numero),
                Complemento = TrimOrNull(request.Complemento),
                Bairro = TrimOrNull(request.Bairro),
                Cidade = TrimOrNull(request.Cidade),
                Estado = NormalizeState(request.Estado),
                Cep = TrimOrNull(request.Cep),
                EstadoCivil = TrimOrNull(request.EstadoCivil),
                Nacionalidade = TrimOrNull(request.Nacionalidade),
                DataNascimento = request.DataNascimento,
                Rg = TrimOrNull(request.Rg),
                Cpf = request.Documento?.Trim(),
                Telefone = pessoa.Telefone,
                Email = pessoa.Email,
                Profissao = TrimOrNull(request.Profissao),
                OndeTrabalha = TrimOrNull(request.OndeTrabalha),
                EnderecoTrabalho = TrimOrNull(request.EnderecoTrabalho),
                NomeEmpresaTrabalho = TrimOrNull(request.NomeEmpresaTrabalho),
                TelefoneEmpresaTrabalho = TrimOrNull(request.TelefoneEmpresaTrabalho),
                DadosBancarios = TrimOrNull(request.DadosBancarios),
                ConjugeNome = TrimOrNull(request.ConjugeNome),
                ConjugeRg = TrimOrNull(request.ConjugeRg),
                ConjugeCpf = TrimOrNull(request.ConjugeCpf),
                ConjugeDataNascimento = request.ConjugeDataNascimento,
                ConjugeProfissao = TrimOrNull(request.ConjugeProfissao),
                ConjugeNacionalidade = TrimOrNull(request.ConjugeNacionalidade),
                ConjugeTelefone = TrimOrNull(request.ConjugeTelefone)
            };
        }
        else
        {
            pessoa.PessoaJuridica = new PessoaJuridica
            {
                NomeEmpresa = pessoa.NomeDisplay,
                Cnpj = request.Documento?.Trim(),
                EmpresaRua = TrimOrNull(request.Rua),
                EmpresaNumero = TrimOrNull(request.Numero),
                EmpresaComplemento = TrimOrNull(request.Complemento),
                EmpresaBairro = TrimOrNull(request.Bairro),
                EmpresaCidade = TrimOrNull(request.Cidade),
                EmpresaEstado = NormalizeState(request.Estado),
                EmpresaCep = TrimOrNull(request.Cep),
                ResponsavelNome = TrimOrNull(request.ResponsavelNome),
                ResponsavelRua = TrimOrNull(request.ResponsavelRua),
                ResponsavelNumero = TrimOrNull(request.ResponsavelNumero),
                ResponsavelComplemento = TrimOrNull(request.ResponsavelComplemento),
                ResponsavelBairro = TrimOrNull(request.ResponsavelBairro),
                ResponsavelCidade = TrimOrNull(request.ResponsavelCidade),
                ResponsavelEstado = NormalizeState(request.ResponsavelEstado),
                ResponsavelCep = TrimOrNull(request.ResponsavelCep),
                ResponsavelEstadoCivil = TrimOrNull(request.ResponsavelEstadoCivil),
                ResponsavelNacionalidade = TrimOrNull(request.ResponsavelNacionalidade),
                ResponsavelDataNascimento = request.ResponsavelDataNascimento,
                ResponsavelEmail = pessoa.Email,
                ResponsavelTelefone = pessoa.Telefone,
                ResponsavelRg = TrimOrNull(request.ResponsavelRg),
                ResponsavelCpf = TrimOrNull(request.ResponsavelCpf),
                ResponsavelProfissao = TrimOrNull(request.ResponsavelProfissao),
                ResponsavelOndeTrabalha = TrimOrNull(request.ResponsavelOndeTrabalha),
                ResponsavelEnderecoTrabalho = TrimOrNull(request.ResponsavelEnderecoTrabalho),
                ResponsavelNomeEmpresaTrabalho = TrimOrNull(request.ResponsavelNomeEmpresaTrabalho),
                ResponsavelTelefoneEmpresaTrabalho = TrimOrNull(request.ResponsavelTelefoneEmpresaTrabalho),
                ResponsavelDadosBancarios = TrimOrNull(request.ResponsavelDadosBancarios)
            };
        }

        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoasAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }

    public async Task<PessoaSummary> UpdatePessoaAsync(UpdatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a pessoa para editar.");
        }

        if (string.IsNullOrWhiteSpace(request.Pessoa.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }

        var pessoa = await dbContext.Pessoas
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa nÃ£o encontrada.");

        pessoa.TipoPessoa = request.Pessoa.TipoPessoa;
        pessoa.NomeDisplay = request.Pessoa.NomeDisplay.Trim();
        pessoa.Telefone = TrimOrNull(request.Pessoa.Telefone);
        pessoa.Email = TrimOrNull(request.Pessoa.Email);
        pessoa.Observacoes = TrimOrNull(request.Pessoa.Observacoes);
        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (request.Pessoa.TipoPessoa == TipoPessoa.Fisica)
        {
            if (pessoa.PessoaJuridica is not null)
            {
                dbContext.PessoasJuridicas.Remove(pessoa.PessoaJuridica);
                pessoa.PessoaJuridica = null;
            }

            pessoa.PessoaFisica ??= new PessoaFisica { PessoaId = pessoa.Id };
            pessoa.PessoaFisica.Nome = pessoa.NomeDisplay;
            pessoa.PessoaFisica.Rua = TrimOrNull(request.Pessoa.Rua);
            pessoa.PessoaFisica.Numero = TrimOrNull(request.Pessoa.Numero);
            pessoa.PessoaFisica.Complemento = TrimOrNull(request.Pessoa.Complemento);
            pessoa.PessoaFisica.Bairro = TrimOrNull(request.Pessoa.Bairro);
            pessoa.PessoaFisica.Cidade = TrimOrNull(request.Pessoa.Cidade);
            pessoa.PessoaFisica.Estado = NormalizeState(request.Pessoa.Estado);
            pessoa.PessoaFisica.Cep = TrimOrNull(request.Pessoa.Cep);
            pessoa.PessoaFisica.EstadoCivil = TrimOrNull(request.Pessoa.EstadoCivil);
            pessoa.PessoaFisica.Nacionalidade = TrimOrNull(request.Pessoa.Nacionalidade);
            pessoa.PessoaFisica.DataNascimento = request.Pessoa.DataNascimento;
            pessoa.PessoaFisica.Rg = TrimOrNull(request.Pessoa.Rg);
            pessoa.PessoaFisica.Cpf = TrimOrNull(request.Pessoa.Documento);
            pessoa.PessoaFisica.Telefone = pessoa.Telefone;
            pessoa.PessoaFisica.Email = pessoa.Email;
            pessoa.PessoaFisica.Profissao = TrimOrNull(request.Pessoa.Profissao);
            pessoa.PessoaFisica.OndeTrabalha = TrimOrNull(request.Pessoa.OndeTrabalha);
            pessoa.PessoaFisica.EnderecoTrabalho = TrimOrNull(request.Pessoa.EnderecoTrabalho);
            pessoa.PessoaFisica.NomeEmpresaTrabalho = TrimOrNull(request.Pessoa.NomeEmpresaTrabalho);
            pessoa.PessoaFisica.TelefoneEmpresaTrabalho = TrimOrNull(request.Pessoa.TelefoneEmpresaTrabalho);
            pessoa.PessoaFisica.DadosBancarios = TrimOrNull(request.Pessoa.DadosBancarios);
            pessoa.PessoaFisica.ConjugeNome = TrimOrNull(request.Pessoa.ConjugeNome);
            pessoa.PessoaFisica.ConjugeRg = TrimOrNull(request.Pessoa.ConjugeRg);
            pessoa.PessoaFisica.ConjugeCpf = TrimOrNull(request.Pessoa.ConjugeCpf);
            pessoa.PessoaFisica.ConjugeDataNascimento = request.Pessoa.ConjugeDataNascimento;
            pessoa.PessoaFisica.ConjugeProfissao = TrimOrNull(request.Pessoa.ConjugeProfissao);
            pessoa.PessoaFisica.ConjugeNacionalidade = TrimOrNull(request.Pessoa.ConjugeNacionalidade);
            pessoa.PessoaFisica.ConjugeTelefone = TrimOrNull(request.Pessoa.ConjugeTelefone);
        }
        else
        {
            if (pessoa.PessoaFisica is not null)
            {
                dbContext.PessoasFisicas.Remove(pessoa.PessoaFisica);
                pessoa.PessoaFisica = null;
            }

            pessoa.PessoaJuridica ??= new PessoaJuridica { PessoaId = pessoa.Id };
            pessoa.PessoaJuridica.NomeEmpresa = pessoa.NomeDisplay;
            pessoa.PessoaJuridica.Cnpj = TrimOrNull(request.Pessoa.Documento);
            pessoa.PessoaJuridica.EmpresaRua = TrimOrNull(request.Pessoa.Rua);
            pessoa.PessoaJuridica.EmpresaNumero = TrimOrNull(request.Pessoa.Numero);
            pessoa.PessoaJuridica.EmpresaComplemento = TrimOrNull(request.Pessoa.Complemento);
            pessoa.PessoaJuridica.EmpresaBairro = TrimOrNull(request.Pessoa.Bairro);
            pessoa.PessoaJuridica.EmpresaCidade = TrimOrNull(request.Pessoa.Cidade);
            pessoa.PessoaJuridica.EmpresaEstado = NormalizeState(request.Pessoa.Estado);
            pessoa.PessoaJuridica.EmpresaCep = TrimOrNull(request.Pessoa.Cep);
            pessoa.PessoaJuridica.ResponsavelNome = TrimOrNull(request.Pessoa.ResponsavelNome);
            pessoa.PessoaJuridica.ResponsavelRua = TrimOrNull(request.Pessoa.ResponsavelRua);
            pessoa.PessoaJuridica.ResponsavelNumero = TrimOrNull(request.Pessoa.ResponsavelNumero);
            pessoa.PessoaJuridica.ResponsavelComplemento = TrimOrNull(request.Pessoa.ResponsavelComplemento);
            pessoa.PessoaJuridica.ResponsavelBairro = TrimOrNull(request.Pessoa.ResponsavelBairro);
            pessoa.PessoaJuridica.ResponsavelCidade = TrimOrNull(request.Pessoa.ResponsavelCidade);
            pessoa.PessoaJuridica.ResponsavelEstado = NormalizeState(request.Pessoa.ResponsavelEstado);
            pessoa.PessoaJuridica.ResponsavelCep = TrimOrNull(request.Pessoa.ResponsavelCep);
            pessoa.PessoaJuridica.ResponsavelEstadoCivil = TrimOrNull(request.Pessoa.ResponsavelEstadoCivil);
            pessoa.PessoaJuridica.ResponsavelNacionalidade = TrimOrNull(request.Pessoa.ResponsavelNacionalidade);
            pessoa.PessoaJuridica.ResponsavelDataNascimento = request.Pessoa.ResponsavelDataNascimento;
            pessoa.PessoaJuridica.ResponsavelTelefone = TrimOrNull(request.Pessoa.ResponsavelTelefone) ?? pessoa.Telefone;
            pessoa.PessoaJuridica.ResponsavelEmail = TrimOrNull(request.Pessoa.ResponsavelEmail) ?? pessoa.Email;
            pessoa.PessoaJuridica.ResponsavelRg = TrimOrNull(request.Pessoa.ResponsavelRg);
            pessoa.PessoaJuridica.ResponsavelCpf = TrimOrNull(request.Pessoa.ResponsavelCpf);
            pessoa.PessoaJuridica.ResponsavelProfissao = TrimOrNull(request.Pessoa.ResponsavelProfissao);
            pessoa.PessoaJuridica.ResponsavelOndeTrabalha = TrimOrNull(request.Pessoa.ResponsavelOndeTrabalha);
            pessoa.PessoaJuridica.ResponsavelEnderecoTrabalho = TrimOrNull(request.Pessoa.ResponsavelEnderecoTrabalho);
            pessoa.PessoaJuridica.ResponsavelNomeEmpresaTrabalho = TrimOrNull(request.Pessoa.ResponsavelNomeEmpresaTrabalho);
            pessoa.PessoaJuridica.ResponsavelTelefoneEmpresaTrabalho = TrimOrNull(request.Pessoa.ResponsavelTelefoneEmpresaTrabalho);
            pessoa.PessoaJuridica.ResponsavelDadosBancarios = TrimOrNull(request.Pessoa.ResponsavelDadosBancarios);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetPessoasAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }

    public async Task SetPessoaActiveAsync(Guid pessoaId, bool isActive, CancellationToken cancellationToken = default)
    {
        var pessoa = await dbContext.Pessoas.SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa nÃ£o encontrada.");

        pessoa.Status = isActive ? RegistroStatus.Ativo : RegistroStatus.Inativo;
        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PessoaId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a pessoa do documento.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            throw new InvalidOperationException("Informe o nome do documento.");
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new InvalidOperationException("Informe o caminho do arquivo digitalizado.");
        }

        var pessoaExists = await dbContext.Pessoas.AnyAsync(x => x.Id == request.PessoaId, cancellationToken);
        if (!pessoaExists)
        {
            throw new InvalidOperationException("Pessoa não encontrada.");
        }

        var documento = new PessoaDocumento
        {
            PessoaId = request.PessoaId,
            Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "outros" : request.Tipo.Trim(),
            DocumentoDe = string.IsNullOrWhiteSpace(request.DocumentoDe) ? "pessoa" : request.DocumentoDe.Trim(),
            Nome = request.Nome.Trim(),
            ContentType = TrimOrNull(request.ContentType),
            DataValidade = request.DataValidade,
            Observacoes = TrimOrNull(request.Observacoes)
        };
        documento.StoragePath = await StorePessoaDocumentoAsync(documento.Id, request.PessoaId, request.StoragePath.Trim(), documento.ContentType, cancellationToken);

        if (documentOcrService is not null)
        {
            var ocrResult = await documentOcrService.ExtractTextAsync(documento.StoragePath, documento.ContentType, cancellationToken);
            documento.OcrTextoExtraido = TrimOrNull(ocrResult.ExtractedText);
            documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
            documento.OcrStatus = ocrResult.Succeeded ? DocumentoOcrStatus.Processado : DocumentoOcrStatus.Erro;
            documento.OcrErroMensagem = TrimOrNull(ocrResult.ErrorMessage);

            if (ocrResult.Succeeded && !string.IsNullOrWhiteSpace(ocrResult.ExtractedText))
            {
                var filledFields = await ApplyPessoaOcrFieldsAsync(request.PessoaId, ocrResult.ExtractedText, cancellationToken);
                documento.OcrCamposAplicados = filledFields.Count == 0 ? null : string.Join(", ", filledFields);
            }
        }

        dbContext.PessoaDocumentos.Add(documento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoaDocumentosAsync(request.PessoaId, cancellationToken)).Single(x => x.Id == documento.Id);
    }

    public async Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PessoaDocumentos
            .AsNoTracking()
            .Include(x => x.Pessoa)
            .AsQueryable();

        if (pessoaId.HasValue)
        {
            query = query.Where(x => x.PessoaId == pessoaId.Value);
        }

        var documentos = await query
            .OrderBy(x => x.Pessoa!.NomeDisplay)
            .ThenBy(x => x.Tipo)
            .ToListAsync(cancellationToken);

        return documentos.Select(x => new PessoaDocumentoSummary(
            x.Id,
            x.PessoaId,
            x.Pessoa?.NomeDisplay ?? "-",
            GetPessoaDocumentoTipoLabel(x.Tipo),
            GetDocumentoDeLabel(x.DocumentoDe),
            x.Nome,
            x.StoragePath,
            x.DataValidade,
            x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
            GetEnumLabel(x.OcrStatus),
            x.OcrTextoExtraido,
            x.OcrProcessadoEmUtc,
            x.OcrErroMensagem,
            x.OcrCamposAplicados)).ToList();
    }

    public async Task<PessoaContratoAutofillContext?> GetPessoaContratoAutofillContextAsync(Guid pessoaId, CancellationToken cancellationToken = default)
    {
        var pessoa = (await GetPessoasAsync(cancellationToken)).SingleOrDefault(x => x.Id == pessoaId);
        if (pessoa is null)
        {
            return null;
        }

        var documentos = await GetPessoaDocumentosAsync(pessoaId, cancellationToken);
        var textoOcr = string.Join(
            Environment.NewLine,
            documentos
                .Where(x => !string.IsNullOrWhiteSpace(x.OcrTextoExtraido))
                .Select(x => $"[{x.Tipo} - {x.Nome}]{Environment.NewLine}{x.OcrTextoExtraido}"));

        return new PessoaContratoAutofillContext(pessoa, documentos, textoOcr);
    }

    public async Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default)
    {
        var imoveis = await dbContext.Imoveis
            .AsNoTracking()
            .Include(x => x.Proprietario)
            .OrderBy(x => x.Rua)
            .ToListAsync(cancellationToken);

        return imoveis.Select(x => new ImovelSummary(
            x.Id,
            $"{x.Rua}, {x.Numero}".Trim().Trim(','),
            x.Bairro,
            x.Proprietario?.NomeDisplay ?? "-",
            GetEnumLabel(x.Finalidade),
            GetEnumLabel(x.Status),
            x.ValorAluguel)).ToList();
    }

    public async Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProprietarioId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um proprietário.");
        }

        if (string.IsNullOrWhiteSpace(request.Rua))
        {
            throw new InvalidOperationException("Informe a rua do imóvel.");
        }

        var proprietario = await dbContext.Pessoas
            .SingleOrDefaultAsync(x => x.Id == request.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("Proprietário não encontrado.");

        var imovel = new Imovel
        {
            ProprietarioId = proprietario.Id,
            Rua = request.Rua.Trim(),
            Numero = request.Numero?.Trim(),
            Complemento = TrimOrNull(request.Complemento),
            Bairro = request.Bairro?.Trim(),
            Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? "Paranavaí" : request.Cidade.Trim(),
            Estado = string.IsNullOrWhiteSpace(request.Estado) ? "PR" : request.Estado.Trim().ToUpperInvariant(),
            Cep = TrimOrNull(request.Cep),
            SaneparMatricula = TrimOrNull(request.SaneparMatricula),
            CopelMatricula = TrimOrNull(request.CopelMatricula),
            IptuMatricula = TrimOrNull(request.IptuMatricula),
            TipoImovel = TrimOrNull(request.TipoImovel),
            Descricao = TrimOrNull(request.Descricao),
            ValorAluguel = request.ValorAluguel,
            ValorVenda = request.ValorVenda,
            Finalidade = request.Finalidade,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Observacoes = request.Observacoes?.Trim()
        };

        dbContext.Imoveis.Add(imovel);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }

    public async Task<ImovelImagemSummary> CreateImovelImagemAsync(CreateImovelImagemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da foto.");
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new InvalidOperationException("Informe o caminho da foto do imóvel.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("Imóvel não encontrado.");
        }

        var imagem = new ImovelImagem
        {
            ImovelId = request.ImovelId,
            FileName = string.IsNullOrWhiteSpace(request.FileName)
                ? Path.GetFileName(request.StoragePath)
                : request.FileName.Trim(),
            StoragePath = await StoreImovelImagemAsync(request.ImovelId, request.StoragePath.Trim(), request.ContentType, cancellationToken),
            ContentType = TrimOrNull(request.ContentType),
            DisplayOrder = request.DisplayOrder
        };

        dbContext.Set<ImovelImagem>().Add(imagem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelImagensAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == imagem.Id);
    }

    public async Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensAsync(Guid imovelId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ImovelImagem>()
            .AsNoTracking()
            .Where(x => x.ImovelId == imovelId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.FileName)
            .Select(x => new ImovelImagemSummary(
                x.Id,
                x.ImovelId,
                x.FileName,
                x.StoragePath,
                x.ContentType,
                x.DisplayOrder,
                x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default)
    {
        var locacoes = await dbContext.Locacoes
            .AsNoTracking()
            .Include(x => x.Imovel)
            .Include(x => x.Proprietario)
            .Include(x => x.Locatario)
            .Include(x => x.Fiadores)
                .ThenInclude(x => x.Fiador)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return locacoes.Select(x => new LocacaoSummary(
            x.Id,
            x.Imovel is null ? "-" : $"{x.Imovel.Rua}, {x.Imovel.Numero}".Trim().Trim(','),
            x.Proprietario?.NomeDisplay ?? "-",
            x.Locatario?.NomeDisplay ?? "-",
            string.Join(", ", x.Fiadores.Select(f => f.Fiador!.NomeDisplay).Order()),
            x.ValorAluguel,
            GetEnumLabel(x.Status))).ToList();
    }

    public async Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default) =>
        await dbContext.IndicesReajuste.AsNoTracking().OrderBy(x => x.Nome)
            .Select(x => new IndiceReajusteSummary(x.Id, x.Nome, x.Codigo, x.Tipo == ReajusteTipo.Oficial ? "Oficial" : "Custom/manual", x.Percentual, x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FinanceiroSummary>> GetLancamentosFinanceirosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.LancamentosFinanceiros.AsNoTracking().OrderBy(x => x.DataVencimento)
            .Select(x => new FinanceiroSummary(x.Id, x.Tipo.ToString(), x.Categoria, x.Descricao, x.Valor, x.DataVencimento, x.Status.ToString()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BoletoSummary>> GetBoletosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Boletos.AsNoTracking().OrderBy(x => x.DataVencimento)
            .Select(x => new BoletoSummary(x.Id, x.Status.ToString(), x.Valor, x.DataVencimento, x.BancoProvider))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotaFiscalSummary>> GetNotasFiscaisAsync(CancellationToken cancellationToken = default) =>
        await dbContext.NotasFiscais.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotaFiscalSummary(x.Id, x.Status.ToString(), x.ValorServico, x.Provider, x.Numero, x.CodigoVerificacao))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DocumentosModelos.AsNoTracking().OrderBy(x => x.Tipo)
            .Select(x => new DocumentoModeloSummary(x.Id, x.Tipo, x.Nome, x.StatusRevisao.ToString(), x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DimobDeclaracoes.AsNoTracking().OrderByDescending(x => x.AnoCalendario)
            .Select(x => new DimobDeclaracaoSummary(x.Id, x.AnoCalendario, x.Status.ToString(), x.Observacoes))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ManutencoesImovel.AsNoTracking().OrderByDescending(x => x.DataSolicitacao)
            .Select(x => new ManutencaoSummary(x.Id, x.Descricao, x.Status.ToString(), x.DataSolicitacao, x.Valor))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Vistorias.AsNoTracking().OrderByDescending(x => x.DataVistoria)
            .Select(x => new VistoriaSummary(x.Id, x.Tipo.ToString(), x.DataVistoria, x.Responsavel, x.Status))
            .ToListAsync(cancellationToken);

    private async Task<string> StorePessoaDocumentoAsync(
        Guid documentoId,
        Guid pessoaId,
        string storagePath,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var objectPath = $"pessoas/{pessoaId}/documentos/{documentoId}/{safeFileName}";
        await using var stream = File.OpenRead(storagePath);
        var stored = await fileStorageService.SaveAsync(
            stream,
            new FileStorageSaveRequest("monthoya-documents", objectPath, fileName, contentType ?? GuessContentType(fileName)),
            cancellationToken);

        return $"{stored.Bucket}/{stored.ObjectPath}";
    }

    private async Task<string> StoreImovelImagemAsync(
        Guid imovelId,
        string storagePath,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var objectPath = $"imoveis/{imovelId}/fotos/{imageId}/{safeFileName}";
        await using var stream = File.OpenRead(storagePath);
        var stored = await fileStorageService.SaveAsync(
            stream,
            new FileStorageSaveRequest("monthoya-property-images", objectPath, fileName, contentType ?? GuessContentType(fileName)),
            cancellationToken);

        return $"{stored.Bucket}/{stored.ObjectPath}";
    }

    private async Task<IReadOnlyList<string>> ApplyPessoaOcrFieldsAsync(Guid pessoaId, string ocrText, CancellationToken cancellationToken)
    {
        var pessoa = await dbContext.Pessoas
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken);

        if (pessoa is null)
        {
            return [];
        }

        var values = ExtractPessoaOcrValues(ocrText);
        var filledFields = new List<string>();

        FillIfBlank(() => pessoa.Telefone, value => pessoa.Telefone = value, values.Telefone, "Telefone", filledFields);
        FillIfBlank(() => pessoa.Email, value => pessoa.Email = value, values.Email, "Email", filledFields);

        if (pessoa.TipoPessoa == TipoPessoa.Fisica && pessoa.PessoaFisica is not null)
        {
            FillIfBlank(() => pessoa.PessoaFisica.Cpf, value => pessoa.PessoaFisica.Cpf = value, values.Cpf, "CPF", filledFields);
            FillIfBlank(() => pessoa.PessoaFisica.Rg, value => pessoa.PessoaFisica.Rg = value, values.Rg, "RG", filledFields);
            FillIfBlank(() => pessoa.PessoaFisica.Cep, value => pessoa.PessoaFisica.Cep = value, values.Cep, "CEP", filledFields);
            FillIfBlank(() => pessoa.PessoaFisica.Rua, value => pessoa.PessoaFisica.Rua = value, values.Endereco, "Rua", filledFields);
            FillIfBlank(() => pessoa.PessoaFisica.Nome, value =>
            {
                pessoa.PessoaFisica.Nome = value;
                if (string.IsNullOrWhiteSpace(pessoa.NomeDisplay))
                {
                    pessoa.NomeDisplay = value;
                }
            }, values.Nome, "Nome", filledFields);
        }

        if (pessoa.TipoPessoa == TipoPessoa.Juridica && pessoa.PessoaJuridica is not null)
        {
            FillIfBlank(() => pessoa.PessoaJuridica.Cnpj, value => pessoa.PessoaJuridica.Cnpj = value, values.Cnpj, "CNPJ", filledFields);
            FillIfBlank(() => pessoa.PessoaJuridica.EmpresaCep, value => pessoa.PessoaJuridica.EmpresaCep = value, values.Cep, "CEP da empresa", filledFields);
            FillIfBlank(() => pessoa.PessoaJuridica.EmpresaRua, value => pessoa.PessoaJuridica.EmpresaRua = value, values.Endereco, "Rua da empresa", filledFields);
            FillIfBlank(() => pessoa.PessoaJuridica.NomeEmpresa, value =>
            {
                pessoa.PessoaJuridica.NomeEmpresa = value;
                if (string.IsNullOrWhiteSpace(pessoa.NomeDisplay))
                {
                    pessoa.NomeDisplay = value;
                }
            }, values.Nome, "Nome da empresa", filledFields);
        }

        if (filledFields.Count > 0)
        {
            pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return filledFields;
    }

    private static PessoaOcrValues ExtractPessoaOcrValues(string text)
    {
        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        return new PessoaOcrValues(
            FindLabeledValue(normalized, "nome") ?? FindLabeledValue(normalized, "razão social") ?? FindLabeledValue(normalized, "razao social"),
            FindRegex(normalized, @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b"),
            FindRegex(normalized, @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b"),
            FindLabeledValue(normalized, "rg"),
            FindRegex(normalized, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase),
            FindRegex(normalized, @"(?:\(?\d{2}\)?\s?)?(?:9\s?)?\d{4}[-\s]?\d{4}"),
            FindRegex(normalized, @"\b\d{5}-?\d{3}\b"),
            FindLabeledValue(normalized, "endereço") ?? FindLabeledValue(normalized, "endereco"));
    }

    private static void FillIfBlank(Func<string?> getCurrent, Action<string> setValue, string? newValue, string fieldName, ICollection<string> filledFields)
    {
        if (!string.IsNullOrWhiteSpace(getCurrent()) || string.IsNullOrWhiteSpace(newValue))
        {
            return;
        }

        setValue(newValue.Trim());
        filledFields.Add(fieldName);
    }

    private static string? FindLabeledValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*{Regex.Escape(label)}\s*[:\-]\s*(?<value>.+?)\s*$",
            RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? FindRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
    {
        var match = Regex.Match(text, pattern, options | RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string GetPessoaRolesLabel(bool isProprietario, bool isLocatario, bool isFiador)
    {
        var roles = new List<string>();
        if (isProprietario) roles.Add("Proprietário");
        if (isLocatario) roles.Add("Locatário");
        if (isFiador) roles.Add("Fiador");
        return roles.Count == 0 ? "-" : string.Join(", ", roles);
    }

    private static CreatePessoaRequest ToPessoaRequest(Pessoa pessoa)
    {
        if (pessoa.TipoPessoa == TipoPessoa.Fisica && pessoa.PessoaFisica is not null)
        {
            var fisica = pessoa.PessoaFisica;
            return new CreatePessoaRequest(
                pessoa.TipoPessoa,
                pessoa.NomeDisplay,
                pessoa.Telefone,
                pessoa.Email,
                fisica.Cpf,
                null,
                pessoa.Observacoes,
                null,
                fisica.Rua,
                fisica.Numero,
                fisica.Complemento,
                fisica.Bairro,
                fisica.Cidade,
                fisica.Estado,
                fisica.Cep,
                fisica.EstadoCivil,
                fisica.Nacionalidade,
                fisica.DataNascimento,
                fisica.Rg,
                fisica.Profissao,
                fisica.OndeTrabalha,
                fisica.EnderecoTrabalho,
                fisica.NomeEmpresaTrabalho,
                fisica.TelefoneEmpresaTrabalho,
                fisica.DadosBancarios,
                fisica.ConjugeNome,
                fisica.ConjugeRg,
                fisica.ConjugeCpf,
                fisica.ConjugeDataNascimento,
                fisica.ConjugeProfissao,
                fisica.ConjugeNacionalidade,
                fisica.ConjugeTelefone);
        }

        var juridica = pessoa.PessoaJuridica;
        return new CreatePessoaRequest(
            pessoa.TipoPessoa,
            pessoa.NomeDisplay,
            pessoa.Telefone,
            pessoa.Email,
            juridica?.Cnpj,
            null,
            pessoa.Observacoes,
            null,
            juridica?.EmpresaRua,
            juridica?.EmpresaNumero,
            juridica?.EmpresaComplemento,
            juridica?.EmpresaBairro,
            juridica?.EmpresaCidade,
            juridica?.EmpresaEstado,
            juridica?.EmpresaCep,
            ResponsavelNome: juridica?.ResponsavelNome,
            ResponsavelEndereco: null,
            ResponsavelRua: juridica?.ResponsavelRua,
            ResponsavelNumero: juridica?.ResponsavelNumero,
            ResponsavelComplemento: juridica?.ResponsavelComplemento,
            ResponsavelBairro: juridica?.ResponsavelBairro,
            ResponsavelCidade: juridica?.ResponsavelCidade,
            ResponsavelEstado: juridica?.ResponsavelEstado,
            ResponsavelCep: juridica?.ResponsavelCep,
            ResponsavelEstadoCivil: juridica?.ResponsavelEstadoCivil,
            ResponsavelNacionalidade: juridica?.ResponsavelNacionalidade,
            ResponsavelDataNascimento: juridica?.ResponsavelDataNascimento,
            ResponsavelTelefone: juridica?.ResponsavelTelefone,
            ResponsavelEmail: juridica?.ResponsavelEmail,
            ResponsavelRg: juridica?.ResponsavelRg,
            ResponsavelCpf: juridica?.ResponsavelCpf,
            ResponsavelProfissao: juridica?.ResponsavelProfissao,
            ResponsavelOndeTrabalha: juridica?.ResponsavelOndeTrabalha,
            ResponsavelEnderecoTrabalho: juridica?.ResponsavelEnderecoTrabalho,
            ResponsavelNomeEmpresaTrabalho: juridica?.ResponsavelNomeEmpresaTrabalho,
            ResponsavelTelefoneEmpresaTrabalho: juridica?.ResponsavelTelefoneEmpresaTrabalho,
            ResponsavelDadosBancarios: juridica?.ResponsavelDadosBancarios);
    }

    private static string NormalizeStoredPath(string storagePath) =>
        storagePath.Replace("\\", "/", StringComparison.Ordinal).Trim();

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

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

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();

    private sealed record PessoaOcrValues(
        string? Nome,
        string? Cpf,
        string? Cnpj,
        string? Rg,
        string? Email,
        string? Telefone,
        string? Cep,
        string? Endereco);
}
