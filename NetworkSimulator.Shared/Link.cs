namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta un collegamento tra due nodi, caratterizzato da una tecnologia di rete specifica.
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Identificativo univoco del collegamento.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// ID del nodo di partenza.
        /// </summary>
        public string From { get; set; }
        /// <summary>
        /// ID del nodo di destinazione.
        /// </summary>
        public string To { get; set; }
        /// <summary>
        /// La tecnologia di rete usata da questo collegamento (es. WiFi, 5G).
        /// </summary>
        public LinkTechnology Technology { get; set; }
        /// <summary>
        /// La larghezza di banda massima del collegamento, in Mbps (Megabit al secondo).
        /// </summary>
        public double Bandwidth { get; set; }
        /// <summary>
        /// La latenza intrinseca del collegamento (ritardo di propagazione), in ms (millisecondi).
        /// </summary>
        public double Latency { get; set; }
        /// <summary>
        /// L'affidabilità del collegamento, rappresentata come probabilità (da 0.0 a 1.0) che un pacchetto venga trasmesso con successo. 1.0 significa 100% affidabile.
        /// </summary>
        public double Reliability { get; set; } = 1.0;
    }
}
