namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta il risultato del calcolo del percorso per un singolo flusso di dati
    /// (da una sorgente specifica a una destinazione).
    /// </summary>
    public class FlowResult
    {
        public string SourceId { get; set; }
        public List<string> Path { get; set; }
        public double TotalCost { get; set; }
    }
}
