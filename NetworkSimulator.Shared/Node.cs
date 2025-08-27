namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Rappresenta un singolo nodo (es. un router o un computer) nel grafo della rete.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Identificativo univoco del nodo (es. "1", "A", o un GUID).
        /// È fondamentale sia per la libreria vis.js (che lo richiede) sia per sapere quali nodi sono collegati dai link.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// L'etichetta testuale che viene visualizzata sul nodo nell'interfaccia utente per renderlo riconoscibile (es. "Router 1", "Server Web", etc.).
        /// </summary>
        public string Label { get; set; }
    }
}
