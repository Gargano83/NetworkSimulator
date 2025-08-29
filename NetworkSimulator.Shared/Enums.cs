namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Definisce i tipi di nodi che possono esistere nella topologia di rete.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// Un dispositivo che genera dati (es. un sensore di temperatura, una videocamera).
        /// </summary>
        Sensor,
        /// <summary>
        /// Un nodo che aggrega i dati da più sensori prima di inoltrarli (es. un concentratore LoRaWAN).
        /// </summary>
        Gateway,
        /// <summary>
        /// Un dispositivo di rete che instrada i pacchetti tra diverse sottoreti.
        /// </summary>
        Router,
        /// <summary>
        /// Un nodo speciale che rappresenta la destinazione finale dei dati su Internet.
        /// </summary>
        Internet
    }

    /// <summary>
    /// Definisce i tipi di tecnologie di rete che possono essere usate per i collegamenti.
    /// </summary>
    public enum LinkTechnology
    {
        /// <summary>
        /// Collegamento fisico ad alta velocità e stabilità (es. Fibra, Ethernet).
        /// </summary>
        Wired,
        /// <summary>
        /// Tecnologia wireless a corto-medio raggio, alta banda.
        /// </summary>
        WiFi,
        /// <summary>
        /// Rete mobile cellulare ad alta velocità e bassa latenza.
        /// </summary>
        FiveG,
        /// <summary>
        /// Tecnologia wireless a lungo raggio, bassa potenza e bassa banda, tipica dell'IoT.
        /// </summary>
        LoRa,
        /// <summary>
        /// Collegamento via satellite, caratterizzato da alta latenza ma copertura globale.
        /// </summary>
        Satellite
    }
}
