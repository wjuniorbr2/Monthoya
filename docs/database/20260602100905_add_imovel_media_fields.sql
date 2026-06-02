-- Monthoya - Phase 2 media fields for property photos/files.
-- EF Core migration: 20260602100905_AddImovelMediaFields
-- Target: PostgreSQL / Supabase PostgreSQL

ALTER TABLE imovel_imagens ADD "Caption" character varying(500);

ALTER TABLE imovel_imagens ADD "IsCover" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE imovel_imagens ADD "IsPublic" boolean NOT NULL DEFAULT FALSE;

-- 0 PropertyPhoto, 1 Document, 2 InspectionPhoto, 3 MaintenancePhoto, 4 Other
ALTER TABLE imovel_imagens ADD "MediaCategory" integer NOT NULL DEFAULT 0;

-- 0 Windows, 1 AndroidStaff, 2 Website, 3 Import
ALTER TABLE imovel_imagens ADD "Source" integer NOT NULL DEFAULT 0;

CREATE INDEX "IX_imovel_imagens_ImovelId_IsCover"
    ON imovel_imagens ("ImovelId", "IsCover");

-- Operational notes:
-- * Inspection photos must stay private by default. The Windows service enforces IsPublic = false
--   when MediaCategory = 2 (InspectionPhoto).
-- * Existing rows become PropertyPhoto/Windows/private/non-cover by default.
-- * Public listing consumers should use IsPublic plus the property's publication flags; do not expose
--   private media or inspection/maintenance photos to the public website.
