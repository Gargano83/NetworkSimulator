using Microsoft.UI.Windowing;

namespace NetworkSimulator.Desktop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            window.Title = "Simulatore di Rete Dinamica";

            // Invece di 'Created', usiamo 'Activated' per essere sicuri che la finestra sia pronta
            window.Activated += OnWindowActivated;

            return window;
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            // Annulla l'iscrizione all'evento per assicurarti che questo codice venga eseguito UNA SOLA VOLTA
            window.Activated -= OnWindowActivated;

#if WINDOWS
            var nativeWindow = window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
            if (nativeWindow != null)
            {
                var appWindow = nativeWindow.AppWindow;
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.Maximize();
                }
            }
#endif
        }
    }
}
