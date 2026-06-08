namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    // This file used to intercept the Imóveis menu button with PreviewMouseLeftButtonDown
    // to open the page through a separate fast-load path. That bypassed the normal
    // page-transition guard and allowed Imóveis/Pessoas loads to overlap when the user
    // clicked through the left menu quickly, which caused EF Core DbContext concurrency
    // exceptions.
    //
    // Keep this partial file intentionally passive. Imóveis now opens through the normal
    // ImoveisNavButton_Click -> UpdateActiveTabAsync -> ShowPageAsync flow, where menu
    // navigation is serialized and buttons are temporarily disabled during loading.
}
