// Importa la classe dell'Hub di SignalR che hai creato.
using NetworkSimulator.Server.Hubs;
using NetworkSimulator.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAZIONE DEI SERVIZI ---

// Aggiunge i servizi necessari per far funzionare i Controller API.
builder.Services.AddControllers();

// Aggiunge i servizi per la documentazione automatica dell'API (Swagger/OpenAPI).
// Molto utile per testare le API direttamente dal browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Aggiunge i servizi di SignalR al container delle dipendenze.
// Questo "accende" la funzionalità di SignalR nel tuo server.
builder.Services.AddSignalR();

// Aggiunge e configura la policy per il Cross-Origin Resource Sharing (CORS).
// È FONDAMENTALE per permettere alla tua app Blazor (che gira su un'altra porta) di comunicare con questo server.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        // Sostituire "http://localhost:5129" con l'URL e la porta esatti del progetto Blazor Client.
        // Si trovano nel file launchSettings.json del progetto Client.
        policy.WithOrigins("https://localhost:7105", "http://localhost:5161")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Necessario per SignalR con autenticazione/sessioni.
    });
});

builder.Services.AddSingleton<SimulationService>();

var app = builder.Build();

// --- 2. CONFIGURAZIONE DELLA PIPELINE HTTP ---

// Abilita Swagger solo in ambiente di sviluppo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Reindirizza automaticamente le richieste da HTTP a HTTPS.
app.UseHttpsRedirection();

// Abilita il routing, necessario per mappare le richieste agli endpoint.
app.UseRouting();

// Applica la policy CORS definite prima.
// La posizione di questa riga (dopo UseRouting e prima di UseAuthorization/Map...) è importante.
app.UseCors("AllowBlazorClient");

// Abilita la possibilità per il server di servire file statici dalla sua cartella wwwroot.
app.UseStaticFiles();

// (Opzionale per ora, ma buona norma) Abilita l'autorizzazione.
app.UseAuthorization();

// Mappa le richieste API verso i Controller.
app.MapControllers();

// Mappa l'URL "/simulationhub" alla classe SimulationHub.
// Quando il client si connetterà a questo indirizzo, SignalR gestirà la connessione.
app.MapHub<SimulationHub>("/simulationhub");

// Avvia l'applicazione.
app.Run();