using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task LoadImovelImagensAsync(Guid imovelId)
    {
        _imovelImagens = await _rentalManagementService.GetImovelImagensAsync(imovelId);
        RefreshImovelMediaGrid();
    }

    private async Task LoadImovelVistoriasAsync(Guid imovelId)
    {
        _imovelVistorias = await _rentalManagementService.GetVistoriasAsync(imovelId);
        ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
    }

    private void BrowseImovelImagemButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelImagemErrorText.Text = string.Empty;

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar foto ou arquivo do imóvel",
            Filter = "Fotos e arquivos|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.pdf;*.txt|Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Documentos|*.pdf;*.txt|Todos os arquivos|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ImovelImagemArquivoBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(ImovelImagemLegendaBox.Text))
        {
            ImovelImagemLegendaBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private async void BrowseImovelImagensBulkButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelImagemErrorText.Text = string.Empty;
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar fotos em lote",
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Todos os arquivos|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var category = GetSelectedImovelMediaCategory();
        var order = ParseNullableInt(ImovelImagemOrdemBox.Text) ?? _pendingImovelMedia.Count + _imovelImagens.Count;
        var markFirstAsCover = ImovelImagemCapaBox.IsChecked == true;

        if (_selectedImovelId.HasValue)
        {
            for (var index = 0; index < dialog.FileNames.Length; index++)
            {
                await CreateImovelMediaAsync(
                    _selectedImovelId.Value,
                    dialog.FileNames[index],
                    Path.GetFileNameWithoutExtension(dialog.FileNames[index]),
                    order + index,
                    category,
                    markFirstAsCover && index == 0,
                    ImovelImagemPublicaBox.IsChecked == true);
            }

            await LoadImovelImagensAsync(_selectedImovelId.Value);
            ClearImovelImagemForm();
            return;
        }

        for (var index = 0; index < dialog.FileNames.Length; index++)
        {
            _pendingImovelMedia.Add(new PendingImovelMedia(
                dialog.FileNames[index],
                Path.GetFileNameWithoutExtension(dialog.FileNames[index]),
                order + index,
                category,
                markFirstAsCover && index == 0,
                ImovelImagemPublicaBox.IsChecked == true));
        }

        RefreshImovelMediaGrid();
        ClearImovelImagemForm();
        ImovelImagemErrorText.Text = "Fotos adicionadas à fila. Elas serão salvas quando o imóvel for salvo.";
    }

    private async void SaveImovelImagemButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelImagemErrorText.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(ImovelImagemArquivoBox.Text))
        {
            ImovelImagemErrorText.Text = "Selecione o arquivo antes de adicionar mídia.";
            return;
        }

        try
        {
            var category = GetSelectedImovelMediaCategory();
            var order = ParseNullableInt(ImovelImagemOrdemBox.Text) ?? _pendingImovelMedia.Count + _imovelImagens.Count;

            if (_selectedImovelId.HasValue)
            {
                await CreateImovelMediaAsync(
                    _selectedImovelId.Value,
                    ImovelImagemArquivoBox.Text,
                    ImovelImagemLegendaBox.Text,
                    order,
                    category,
                    ImovelImagemCapaBox.IsChecked == true,
                    ImovelImagemPublicaBox.IsChecked == true);

                ClearImovelImagemForm();
                await LoadImovelImagensAsync(_selectedImovelId.Value);
            }
            else
            {
                _pendingImovelMedia.Add(new PendingImovelMedia(
                    ImovelImagemArquivoBox.Text,
                    ImovelImagemLegendaBox.Text,
                    order,
                    category,
                    ImovelImagemCapaBox.IsChecked == true,
                    ImovelImagemPublicaBox.IsChecked == true));

                RefreshImovelMediaGrid();
                ClearImovelImagemForm();
                ImovelImagemErrorText.Text = "Mídia adicionada à fila. Ela será salva quando o imóvel for salvo.";
            }
        }
        catch (Exception ex)
        {
            ImovelImagemErrorText.Text = ex.Message;
        }
    }

    private static string GetImovelExceptionMessage(Exception ex)
    {
        var baseException = ex.GetBaseException();
        return baseException is not null && !string.Equals(baseException.Message, ex.Message, StringComparison.Ordinal)
            ? $"{ex.Message} Detalhe: {baseException.Message}"
            : ex.Message;
    }

    private async Task SavePendingImovelMediaAsync(Guid imovelId)
    {
        foreach (var media in _pendingImovelMedia)
        {
            await CreateImovelMediaAsync(
                imovelId,
                media.StoragePath,
                media.Caption,
                media.DisplayOrder,
                media.MediaCategory,
                media.IsCover,
                media.IsPublic);
        }
    }

    private Task CreateImovelMediaAsync(
        Guid imovelId,
        string storagePath,
        string? caption,
        int displayOrder,
        ImovelMediaCategory category,
        bool isCover,
        bool isPublic) =>
        _rentalManagementService.CreateImovelImagemAsync(new CreateImovelImagemRequest(
            ImovelId: imovelId,
            FileName: Path.GetFileName(storagePath),
            StoragePath: storagePath,
            ContentType: GuessImovelMediaContentType(storagePath),
            DisplayOrder: displayOrder,
            Caption: caption,
            IsCover: isCover,
            IsPublic: isPublic,
            MediaCategory: category,
            Source: ImovelMediaSource.Windows));

    private void RefreshImovelMediaGrid()
    {
        var savedRows = _imovelImagens.Select(x => new ImovelMediaListItem(
            x.FileName,
            x.MediaCategory,
            x.Caption,
            x.IsCover,
            x.IsPublic,
            x.Source,
            x.Status,
            GetImagePreviewPath(x.StoragePath),
            GetFileKindLabel(x.FileName)));
        var pendingRows = _pendingImovelMedia.Select(x => new ImovelMediaListItem(
            Path.GetFileName(x.StoragePath),
            GetImovelMediaCategoryLabel(x.MediaCategory),
            x.Caption,
            x.IsCover,
            x.IsPublic,
            "Windows",
            "Pendente",
            GetImagePreviewPath(x.StoragePath),
            GetFileKindLabel(x.StoragePath)));

        ImovelImagensGrid.ItemsSource = savedRows.Concat(pendingRows).ToList();
    }
}


