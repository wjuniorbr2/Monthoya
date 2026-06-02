START TRANSACTION;
ALTER TABLE imoveis ADD "AceitaPets" boolean;

ALTER TABLE imoveis ADD "AreaConstruida" numeric(12,2);

ALTER TABLE imoveis ADD "AreaTerreno" numeric(12,2);

ALTER TABLE imoveis ADD "Banheiros" integer;

ALTER TABLE imoveis ADD "ChaveAutorizacaoNecessaria" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE imoveis ADD "ChaveCodigo" character varying(80);

ALTER TABLE imoveis ADD "ChaveContatoDocumento" character varying(40);

ALTER TABLE imoveis ADD "ChaveContatoNome" character varying(220);

ALTER TABLE imoveis ADD "ChaveLocalRetirada" character varying(500);

ALTER TABLE imoveis ADD "ChaveMelhorHorario" character varying(120);

ALTER TABLE imoveis ADD "ChaveObservacoes" character varying(2000);

ALTER TABLE imoveis ADD "ChavePosse" integer NOT NULL DEFAULT 0;

ALTER TABLE imoveis ADD "ChaveQuemTem" character varying(220);

ALTER TABLE imoveis ADD "ChaveTelefone" character varying(40);

ALTER TABLE imoveis ADD "DescricaoInterna" character varying(4000);

ALTER TABLE imoveis ADD "DescricaoPublica" character varying(4000);

ALTER TABLE imoveis ADD "Destaque" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE imoveis ADD "Mobiliado" boolean;

ALTER TABLE imoveis ADD "ModoExibicaoEnderecoPublico" integer NOT NULL DEFAULT 0;

ALTER TABLE imoveis ADD "MostrarEnderecoCompletoPublicamente" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE imoveis ADD "PublicarNoApp" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE imoveis ADD "PublicarNoSite" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE imoveis ADD "Quartos" integer;

ALTER TABLE imoveis ADD "Suites" integer;

ALTER TABLE imoveis ADD "VagasGaragem" integer;

ALTER TABLE imoveis ADD "ValorCondominio" numeric(18,2);

ALTER TABLE imoveis ADD "ValorIptu" numeric(18,2);

UPDATE imoveis
SET "DescricaoInterna" = "Descricao"
WHERE "DescricaoInterna" IS NULL
  AND "Descricao" IS NOT NULL
  AND btrim("Descricao") <> '';

CREATE TABLE imovel_chave_movimentos (
    "Id" uuid NOT NULL,
    "ImovelId" uuid NOT NULL,
    "ChaveCodigo" character varying(80),
    "Tipo" integer NOT NULL,
    "RetiradoPorNome" character varying(220),
    "RetiradoPorTelefone" character varying(40),
    "RetiradoPorDocumento" character varying(40),
    "RetiradoPorRelacao" character varying(120),
    "Motivo" character varying(120),
    "RetiradoEm" timestamp with time zone,
    "PrevisaoDevolucaoEm" timestamp with time zone,
    "DevolvidoEm" timestamp with time zone,
    "DevolvidoParaNome" character varying(220),
    "Status" integer NOT NULL,
    "Observacoes" character varying(2000),
    "CreatedByUserId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_imovel_chave_movimentos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_imovel_chave_movimentos_imoveis_ImovelId" FOREIGN KEY ("ImovelId") REFERENCES imoveis ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_imovel_chave_movimentos_ImovelId_Status" ON imovel_chave_movimentos ("ImovelId", "Status");

CREATE INDEX "IX_imovel_chave_movimentos_PrevisaoDevolucaoEm" ON imovel_chave_movimentos ("PrevisaoDevolucaoEm");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260601115306_AddImovelPhaseOneFields', '10.0.4');

COMMIT;

