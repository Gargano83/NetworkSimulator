namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta l'intera topologia della rete. 
    /// È un oggetto di trasferimento dati (DTO) utilizzato per inviare lo stato completo del grafo dal client al server.
    /// </summary>
    public class GraphData
    {
        /// <summary>
        /// La lista di tutti i nodi presenti nel grafo.
        /// </summary>
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// La lista di tutti i collegamenti (archi) che connettono i nodi nel grafo.
        /// </summary>
        public List<Link> Links { get; set; }
    }
}
