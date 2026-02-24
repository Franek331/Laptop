using RozpoznawanieMatwarzy.Services;
using RozpoznawanieMatwarzy.ViewModels;

namespace RozpoznawanieMatwarzy.Views;

public partial class StronaRaportu : ContentPage
{
    private ModelRaportu _viewModel;

    public StronaRaportu()
    {
        InitializeComponent();
        _viewModel = new ModelRaportu();
        BindingContext = _viewModel;

        // Załaduj dane jeśli są dostępne
        if (RaportHelper.OstatniaRozpoznana != null)
        {
            _viewModel.WczytajDaneOsoby(
                RaportHelper.OstatniaRozpoznana,
                RaportHelper.WybraneZdjecie
            );
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // ✅ CZYŚĆ RAPORT HELPER ZAWSZE
        RaportHelper.OstatniaRozpoznana = null;
        RaportHelper.WybraneZdjecie = null;
    }
}