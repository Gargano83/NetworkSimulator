namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta un nodo generico nella topologia di rete (sensore, router, etc.).
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Identificativo univoco del nodo.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Etichetta visualizzata nell'interfaccia utente.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Il tipo di nodo, definito dall'enumerazione NodeType.
        /// </summary>
        public NodeType Type { get; set; }
        /// <summary>
        /// La quantità di dati che il sensore genera al secondo, in KB/s (Kilobyte al secondo).
        /// Sarà 'null' se il nodo non è un sensore.
        /// </summary>
        public double? DataRate { get; set; }
        /// <summary>
        /// Il requisito massimo di latenza per i dati generati da questo sensore, in ms (millisecondi).
        /// Sarà 'null' se il nodo non è un sensore.
        /// </summary>
        public double? LatencyRequirement { get; set; }
        /// <summary>
        /// La dimensione tipica di un pacchetto di dati generato da questo sensore, in KB (Kilobyte).
        /// Sarà 'null' se il nodo non è un sensore.
        /// </summary>
        public double? PacketSize { get; set; }
    }
}
