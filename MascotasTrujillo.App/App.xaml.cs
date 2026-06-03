using MascotasTrujillo.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MascotasTrujillo.App
{
    public partial class App : Application
    {
        private readonly LoginPage _loginPage;

        public App(LoginPage loginPage) // Inyectamos la página aquí
        {
            InitializeComponent();
            _loginPage = loginPage;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // ¡Aquí está el gran cambio! 
            // Adiós AppShell, hola LoginPage
            return new Window(_loginPage);
        }
    }
}