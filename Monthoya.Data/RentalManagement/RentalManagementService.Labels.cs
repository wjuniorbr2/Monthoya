using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{



    private static string GetVistoriaTipoLabel(VistoriaTipo tipo) =>
        tipo switch
        {
            VistoriaTipo.InicialProprietario => "Inicial do proprietário",
            VistoriaTipo.Entrada => "Entrada da locação",
            VistoriaTipo.Saida => "Saída da locação",
            VistoriaTipo.Periodica => "Periódica",
            VistoriaTipo.Manutencao => "Manutenção",
            VistoriaTipo.Outros => "Outra",
            _ => tipo.ToString()
        };

    private static string GetVistoriaStatusLabel(VistoriaStatus status) =>
        status switch
        {
            VistoriaStatus.Draft => "Rascunho",
            VistoriaStatus.InProgress => "Em andamento",
            VistoriaStatus.ReadyToReview => "Pronta para revisão",
            VistoriaStatus.Finished => "Finalizada",
            VistoriaStatus.SignedPaper => "Assinada em papel",
            VistoriaStatus.SignedDigitally => "Assinada digitalmente",
            VistoriaStatus.Canceled => "Cancelada",
            _ => status.ToString()
        };
}

