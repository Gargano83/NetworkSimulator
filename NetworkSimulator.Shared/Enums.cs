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
        /// Connessione cablata in fibra ottica, caratterizzata da bassissima latenza e alta banda.
        /// </summary>
        Fiber,
        /// <summary>
        /// Connessione cablata su doppino telefonico (rame), con performance inferiori alla fibra.
        /// </summary>
        DSL,
        /// <summary>
        /// Tecnologia wireless a corto-medio raggio, tipica delle reti locali (LAN).
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
        /// Collegamento via satelliti in orbita bassa (Low Earth Orbit), con latenza ridotta (es. Starlink).
        /// </summary>
        SatelliteLEO,
        /// <summary>
        /// Collegamento via satelliti in orbita geostazionaria, caratterizzato da altissima latenza.
        /// </summary>
        SatelliteGEO,
        /// <summary>
        /// Rappresenta un collegamento ad alte prestazioni all'interno di una dorsale di rete (Core Network).
        /// </summary>
        CoreNwk
    }
}
