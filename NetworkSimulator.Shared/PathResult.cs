namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta il risultato del calcolo di un percorso da parte di un algoritmo.
    /// È un oggetto di trasferimento dati (DTO) che il server invia al client.
    /// </summary>
    public class PathResult
    {
        /// <summary>
        /// Una lista ordinata di stringhe che rappresenta gli ID dei nodi nel percorso calcolato, dal nodo di partenza al nodo di destinazione.
        /// Se non viene trovato alcun percorso, questa lista sarà vuota o null.
        /// </summary>
        public List<string> Path { get; set; }

        /// <summary>
        /// La somma totale dei "pesi" (Weight) dei collegamenti che compongono il percorso trovato.
        /// Rappresenta il costo totale del percorso.
        /// </summary>
        public double TotalCost { get; set; }

        /// <summary>
        /// Una stringa che contiene un messaggio di errore nel caso in cui il calcolo non sia andato a buon fine (es. "Nodo di partenza non trovato" o "Nessun percorso trovato"). 
        /// Se il calcolo ha avuto successo, questa proprietà sarà null.
        /// </summary>
        public string? Error { get; set; }
    }
}
