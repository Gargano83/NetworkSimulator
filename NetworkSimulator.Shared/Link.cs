namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta un collegamento unidirezionale (o arco) tra due nodi nel grafo.
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Identificativo univoco del collegamento stesso. 
        /// Utile per la gestione, come la modifica o l'eliminazione di un link specifico.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// L'identificativo univoco (la proprietà 'Id') del nodo di PARTENZA del collegamento.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// L'identificativo univoco (la proprietà 'Id') del nodo di DESTINAZIONE del collegamento.
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Il "peso" o "costo" associato all'attraversamento di questo collegamento.
        /// Questa è la metrica chiave che gli algoritmi di routing (Dijkstra, ACO, etc.) useranno per calcolare il percorso migliore. Può rappresentare la latenza, la distanza, o il costo economico.
        /// </summary>
        public double Weight { get; set; } = 1;
    }
}
