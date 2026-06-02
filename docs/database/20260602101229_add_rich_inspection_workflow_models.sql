-- Monthoya - Phase 3 rich inspection/vistoria workflow preparation.
-- EF Core migration: 20260602101229_AddRichInspectionWorkflowModels
-- Target: PostgreSQL / Supabase PostgreSQL

ALTER TABLE vistorias ADD "AiErrorMessage" character varying(2000);
ALTER TABLE vistorias ADD "AiProcessedAt" timestamp with time zone;
ALTER TABLE vistorias ADD "AiStatus" character varying(80);
ALTER TABLE vistorias ADD "AiSummary" text;
ALTER TABLE vistorias ADD "DescricaoGeral" text;
ALTER TABLE vistorias ADD "PdfPath" character varying(1000);
ALTER TABLE vistorias ADD "StableId" uuid NOT NULL DEFAULT gen_random_uuid();
ALTER TABLE vistorias ADD "WorkflowStatus" integer NOT NULL DEFAULT 0;

CREATE TABLE vistoria_ambientes (
    "Id" uuid NOT NULL,
    "StableId" uuid NOT NULL DEFAULT gen_random_uuid(),
    "VistoriaId" uuid NOT NULL,
    "Nome" character varying(160) NOT NULL,
    "TipoAmbiente" integer NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "Observacoes" character varying(4000),
    "CondicaoGeral" character varying(120),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_vistoria_ambientes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_vistoria_ambientes_vistorias_VistoriaId"
        FOREIGN KEY ("VistoriaId") REFERENCES vistorias ("Id") ON DELETE CASCADE
);

CREATE TABLE vistoria_itens (
    "Id" uuid NOT NULL,
    "StableId" uuid NOT NULL DEFAULT gen_random_uuid(),
    "VistoriaAmbienteId" uuid NOT NULL,
    "Nome" character varying(160) NOT NULL,
    "Categoria" integer NOT NULL,
    "Condicao" integer NOT NULL,
    "Descricao" text,
    "Observacoes" text,
    "ResponsabilidadeSugerida" character varying(120),
    "AiDetectedDamage" boolean,
    "AiSuggestedDescription" text,
    "AiConfidence" numeric(5,4),
    "AiStatus" character varying(80),
    "AiProcessedAt" timestamp with time zone,
    "AiErrorMessage" character varying(2000),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_vistoria_itens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_vistoria_itens_vistoria_ambientes_VistoriaAmbienteId"
        FOREIGN KEY ("VistoriaAmbienteId") REFERENCES vistoria_ambientes ("Id") ON DELETE CASCADE
);

CREATE TABLE vistoria_fotos (
    "Id" uuid NOT NULL,
    "StableId" uuid NOT NULL DEFAULT gen_random_uuid(),
    "VistoriaId" uuid NOT NULL,
    "VistoriaAmbienteId" uuid,
    "VistoriaItemId" uuid,
    "ImovelId" uuid NOT NULL,
    "LocacaoId" uuid,
    "FileName" character varying(260) NOT NULL,
    "LocalDevicePath" character varying(1000),
    "StoragePath" character varying(1000),
    "ContentType" character varying(100),
    "DisplayOrder" integer NOT NULL,
    "Caption" character varying(500),
    "TakenAt" timestamp with time zone,
    "UploadedAt" timestamp with time zone,
    "UploadStatus" integer NOT NULL,
    "Source" integer NOT NULL,
    "IsPublicWebsite" boolean NOT NULL,
    "VisibleToClientApp" boolean,
    "AiDescription" text,
    "AiDetectedDamage" boolean,
    "AiSuggestedCaption" text,
    "AiConfidence" numeric(5,4),
    "AiStatus" character varying(80),
    "AiProcessedAt" timestamp with time zone,
    "AiErrorMessage" character varying(2000),
    "MetadataJson" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_vistoria_fotos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_vistoria_fotos_imoveis_ImovelId"
        FOREIGN KEY ("ImovelId") REFERENCES imoveis ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_vistoria_fotos_vistorias_VistoriaId"
        FOREIGN KEY ("VistoriaId") REFERENCES vistorias ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_vistoria_fotos_vistoria_ambientes_VistoriaAmbienteId"
        FOREIGN KEY ("VistoriaAmbienteId") REFERENCES vistoria_ambientes ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_vistoria_fotos_vistoria_itens_VistoriaItemId"
        FOREIGN KEY ("VistoriaItemId") REFERENCES vistoria_itens ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_vistorias_ImovelId_DataVistoria" ON vistorias ("ImovelId", "DataVistoria");
CREATE UNIQUE INDEX "IX_vistorias_StableId" ON vistorias ("StableId");
CREATE UNIQUE INDEX "IX_vistoria_ambientes_StableId" ON vistoria_ambientes ("StableId");
CREATE INDEX "IX_vistoria_ambientes_VistoriaId_DisplayOrder" ON vistoria_ambientes ("VistoriaId", "DisplayOrder");
CREATE UNIQUE INDEX "IX_vistoria_itens_StableId" ON vistoria_itens ("StableId");
CREATE INDEX "IX_vistoria_itens_VistoriaAmbienteId" ON vistoria_itens ("VistoriaAmbienteId");
CREATE UNIQUE INDEX "IX_vistoria_fotos_StableId" ON vistoria_fotos ("StableId");
CREATE INDEX "IX_vistoria_fotos_ImovelId_UploadStatus" ON vistoria_fotos ("ImovelId", "UploadStatus");
CREATE INDEX "IX_vistoria_fotos_VistoriaAmbienteId" ON vistoria_fotos ("VistoriaAmbienteId");
CREATE INDEX "IX_vistoria_fotos_VistoriaId_DisplayOrder" ON vistoria_fotos ("VistoriaId", "DisplayOrder");
CREATE INDEX "IX_vistoria_fotos_VistoriaItemId" ON vistoria_fotos ("VistoriaItemId");

ALTER TABLE vistorias ADD CONSTRAINT "FK_vistorias_imoveis_ImovelId"
    FOREIGN KEY ("ImovelId") REFERENCES imoveis ("Id") ON DELETE CASCADE;

-- Enum notes:
-- Vistoria WorkflowStatus: 0 Draft, 1 InProgress, 2 ReadyToReview, 3 Finished,
-- 4 SignedPaper, 5 SignedDigitally, 6 Canceled.
-- VistoriaFoto UploadStatus: 0 LocalOnly, 1 PendingUpload, 2 Uploaded, 3 Failed.
-- VistoriaFoto Source reuses ImovelMediaSource: 0 Windows, 1 AndroidStaff, 2 Website, 3 Import.

-- Operational notes:
-- * StableId is for future Android/offline sync and API DTOs. The main Id remains the database PK.
-- * Inspection photos default to private. Do not expose IsPublicWebsite=true automatically.
-- * StoragePath may be null while Android staff photos remain local/offline; use UploadStatus to track sync.
-- * AI fields are optional placeholders for later inspection summary, photo damage detection, and entry/exit comparison.
