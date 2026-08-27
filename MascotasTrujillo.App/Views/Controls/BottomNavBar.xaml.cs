namespace MascotasTrujillo.App.Views.Controls;

public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty CurrentPageProperty =
        BindableProperty.Create(
            nameof(CurrentPage),
            typeof(string),
            typeof(BottomNavBar),
            "Radar",
            propertyChanged: OnCurrentPageChanged
        );

    public string CurrentPage
    {
        get => (string)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        Loaded += (_, _) => ActualizarEstado();
    }

    private static void OnCurrentPageChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is BottomNavBar navBar)
            navBar.ActualizarEstado();
    }

    private void ActualizarEstado()
    {
        AplicarEstado(
            RadarCircle,
            RadarIcon,
            RadarText,
            CurrentPage == "Radar"
        );

        AplicarEstado(
            ReportesCircle,
            ReportesIcon,
            ReportesText,
            CurrentPage == "Reportes"
        );

        AplicarEstado(
            MascotasCircle,
            MascotasIcon,
            MascotasText,
            CurrentPage == "Mascotas"
        );

        AplicarEstado(
            PerfilCircle,
            PerfilIcon,
            PerfilText,
            CurrentPage == "Perfil"
        );
    }

    private static void AplicarEstado(
        Border circle,
        Label icon,
        Label text,
        bool activo)
    {
        Color colorActivo = Color.FromArgb("#5B21E6");
        Color fondoActivo = Color.FromArgb("#F3E8FF");
        Color colorInactivo = Color.FromArgb("#64748B");

        circle.BackgroundColor = activo ? fondoActivo : Colors.Transparent;
        icon.TextColor = activo ? colorActivo : colorInactivo;
        text.TextColor = activo ? colorActivo : colorInactivo;

        circle.Scale = activo ? 1.05 : 1.0;
        text.FontAttributes = activo ? FontAttributes.Bold : FontAttributes.None;
    }

    private async void OnRadarTapped(object sender, TappedEventArgs e)
    {
        if (CurrentPage == "Radar")
            return;

        await Shell.Current.GoToAsync("//radar");
    }

    private async void OnReportesTapped(object sender, TappedEventArgs e)
    {
        if (CurrentPage == "Reportes")
            return;

        await Shell.Current.GoToAsync("//reportes");
    }

    private async void OnMascotasTapped(object sender, TappedEventArgs e)
    {
        if (CurrentPage == "Mascotas")
            return;

        await Shell.Current.GoToAsync("//mascotas");
    }

    private async void OnPerfilTapped(object sender, TappedEventArgs e)
    {
        if (CurrentPage == "Perfil")
            return;

        await Shell.Current.GoToAsync("//perfil");
    }
}