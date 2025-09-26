namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta un singolo pacchetto di dati che viene generato da un sensore
    /// e deve essere instradato attraverso la rete.
    /// </summary>
    public class DataPacket
    {
        /// <summary>
        /// Un ID univoco per tracciare il pacchetto durante la simulazione.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// L'ID del nodo sensore che ha originato il pacchetto.
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// L'ID del nodo di destinazione finale (es. un nodo 'Internet').
        /// </summary>
        public string DestinationId { get; set; }
        /// <summary>
        /// L'ID del nodo in cui il pacchetto si trova attualmente durante la simulazione.
        /// </summary>
        public string CurrentLocationId { get; set; }
        /// <summary>
        /// L'ID del nodo precedente in cui si trovava il pacchetto.
        /// Sarà null se il pacchetto è appena stato generato sulla sorgente.
        /// </summary>
        public string? PreviousLocationId { get; set; }
        /// <summary>
        /// Contiene l'intero percorso pianificato (lista di ID di nodi)
        /// calcolato quando il pacchetto è stato generato.
        /// </summary>
        public List<string>? FullPath { get; set; }
        /// <summary>
        /// La dimensione del pacchetto, in KB (Kilobyte).
        /// </summary>
        public double Size { get; set; }
        /// <summary>
        /// La priorità del pacchetto (es. 1 per alta priorità, 5 per bassa priorità),
        /// utile per le decisioni di routing basate sulla Qualità del Servizio (QoS).
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// L'indice della posizione attuale del pacchetto all'interno di FullPath.
        /// Inizia a 0 (la sorgente).
        /// </summary>
        public int PathIndex { get; set; } = 0;
        /// <summary>
        /// Il "tick" di tempo della simulazione in cui il pacchetto è stato creato.
        /// Utile per calcolare la latenza totale di consegna.
        /// </summary>
        public int CreationTime { get; set; }
        /// <summary>
        /// Time-To-Live: un contatore per rimuovere i pacchetti bloccati.
        /// </summary>
        public int Ttl { get; set; } = 15; // Un pacchetto "vive" per 15 tick (secondi)
        /// <summary>
        /// Memorizza il tempo di simulazione in cui il pacchetto arriverà al nodo corrente.
        /// </summary>
        public double ArrivalTimeAtCurrentNode { get; set; }
    }
}
