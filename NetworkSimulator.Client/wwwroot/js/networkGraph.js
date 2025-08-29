// Colori di default e per l'evidenziazione
const defaultEdgeColor = '#848484';
const pathColor = '#ADD8E6';            // Azzurro tenue per il percorso pianificato
const highlightColor = '#00BFFF';       // Blu elettrico per l'impulso attivo

// Variabili globali per l'applicazione
let network = null;
let nodes = new vis.DataSet([]);
let edges = new vis.DataSet([]);
let dotNetHelper = null; // Riferimento globale al componente Blazor

// Stato per la modalità "Aggiungi Collegamento", gestito interamente in JS
let isAddingLinkMode = false;
let firstNodeIdSelected = null;
let isDeletingNodeMode = false;

window.networkGraph = {
    /**
     * Inizializza la rete vis.js e imposta i gestori di eventi.
     */
    init: function (containerId, helper) {
        dotNetHelper = helper;
        let container = document.getElementById(containerId);
        let data = { nodes: nodes, edges: edges };
        let options = {
            interaction: {
                dragNodes: true,
                hover: true // Abilita l'evidenziazione al passaggio del mouse
            },
            physics: {
                // Usiamo il solver "barnesHut" che ci dà più controllo sulla spaziatura
                barnesHut: {
                    // Aumenta la repulsione tra i nodi. Valori più negativi = più spazio.
                    gravitationalConstant: -20000,
                    // La "lunghezza" ideale di un collegamento (molla)
                    springLength: 250,
                    // Rigidità della molla. Un valore basso la rende più "morbida"
                    springConstant: 0.04,
                    // Evita che i nodi si sovrappongano
                    avoidOverlap: 0.5
                },
                // Mantiene la simulazione fisica attiva per un po' per stabilizzare il grafo
                stabilization: {
                    iterations: 200
                }
            },
            nodes: {
                font: { size: 14, face: 'arial' }
            },
            edges: {
                font: {
                    align: 'top', // Allinea l'etichetta sopra il collegamento
                    size: 12,
                    strokeWidth: 2, // Aggiunge un piccolo contorno bianco al testo
                    strokeColor: '#ffffff'
                },
                // --- NUOVE OPZIONI PER I LINK ---
                arrows: {
                    to: { enabled: true, scaleFactor: 0.7 } // Mostra una freccia verso la destinazione
                },
                smooth: {
                    enabled: true,
                    type: "curvedCW", // Rende i link curvi (senso orario)
                    roundness: 0.1   // Quanto devono essere curvi
                },
                color: {
                    color: '#848484', // Colore grigio di default
                    highlight: highlightColor // Mantiene il colore per l'evidenziazione
                },
                dashes: true, // Tutti i link saranno tratteggiati di default
                width: 1      // Spessore di default
            }
        };
        network = new vis.Network(container, data, options);

        network.on("click", function (params) {
            if (params.nodes.length > 0) {
                const nodeId = params.nodes[0];
                // Se siamo in modalità eliminazione NODO...
                if (isDeletingNodeMode) {
                    dotNetHelper.invokeMethodAsync('DeleteNode', nodeId); // <-- CHIAMA IL NUOVO METODO
                    isDeletingNodeMode = false; // Disattiva la modalità
                }
                // Se siamo in modalità aggiunta LINK...
                else if (isAddingLinkMode) {
                    if (firstNodeIdSelected === null) {
                        firstNodeIdSelected = nodeId;
                    } else {
                        if (firstNodeIdSelected !== nodeId) {
                            dotNetHelper.invokeMethodAsync('CreateLink', firstNodeIdSelected, nodeId);
                        }
                        isAddingLinkMode = false;
                        firstNodeIdSelected = null;
                    }
                }
            }
            // Se clicchiamo un ARCO (per eliminazione link)
            else if (params.edges.length > 0) {
                dotNetHelper.invokeMethodAsync('HandleLinkClick', params.edges[0]);
            }
        });

        // Gestore del doppio click
        network.on("doubleClick", function (params) {
            if (params.nodes.length > 0) {
                dotNetHelper.invokeMethodAsync('HandleNodeDoubleClick', params.nodes[0]);
            }
            else if (params.edges.length > 0) {
                dotNetHelper.invokeMethodAsync('HandleLinkDoubleClick', params.edges[0]);
            }
        });
    },

    // Funzione chiamata da Blazor per "armare" la modalità
    enterAddLinkMode: function () {
        isAddingLinkMode = true;
        isDeletingNodeMode = false;
        firstNodeIdSelected = null;
    },

    enterDeleteNodeMode: function () {
        isDeletingNodeMode = true;
        isAddingLinkMode = false;
    },

    drawTopology: function (loadedNodes, loadedLinks) {
        // Pulisce tutti gli elementi esistenti
        nodes.clear();
        edges.clear();

        // Itera e aggiunge i nuovi nodi e link usando le funzioni esistenti
        // in modo che vengano applicati gli stili corretti
        loadedNodes.forEach(node => window.networkGraph.addNode(node));
        loadedLinks.forEach(link => {
            window.networkGraph.addLink(link);
        });

        if (network) {
            network.once("stabilized", function () {
                // Questo codice verrà eseguito solo quando la fisica ha finito.
                network.fit({
                    animation: {
                        duration: 500,
                        easingFunction: "easeInOutQuad"
                    }
                });
            });
        }
    },

    updatePackets: function (packetData) {
        try {
            // 1. Resetta tutte le evidenziazioni del tick precedente
            window.networkGraph.resetHighlights();

            // 2. Per ogni pacchetto, disegna il suo percorso accumulato
            packetData.forEach(p => {
                if (p.fullPath && p.pathIndex > 0) {

                    // --- LOGICA CHIAVE ---
                    // Estrae la porzione di percorso già attraversata dal pacchetto
                    const traversedPath = p.fullPath.slice(0, p.pathIndex + 1);

                    // Evidenzia l'intero percorso attraversato finora
                    highlightPath(traversedPath, highlightColor, 4);
                }
            });

        } catch (e) {
            console.error("[JS] ERRORE CRITICO in updatePackets:", e);
        }
    },

    resetHighlights: function () {
        let allEdges = edges.get({ fields: ['id'] });
        if (allEdges.length > 0) {
            let updates = allEdges.map(edge => ({
                id: edge.id,
                color: { color: defaultEdgeColor },
                width: null
            }));
            edges.update(updates);
        }
    },

    addNode: function (node) {
        switch (node.type.toLowerCase()) {
            case "sensor":
                node.shape = "dot";
                node.color = "#FFC107"; // Giallo
                node.size = 15;
                break;
            case "gateway":
                node.shape = "triangle";
                node.color = "#198754"; // Verde
                node.size = 20;
                break;
            case "router":
                node.shape = "square";
                node.color = "#0D6EFD"; // Blu
                node.size = 20;
                break;
            case "internet":
                node.shape = "icon";
                node.icon = {
                    face: "'Font Awesome 5 Free'",
                    weight: "900", // Necessario per le icone solid
                    code: '\uf0ac', // Codice unicode per l'icona "globe"
                    size: 50,
                    color: '#6c757d'
                };
                break;
        }
        nodes.add(node);
    },

    addLink: function (link) {
        edges.add(link);
    },

    // Funzioni helper generiche (aggiornamento, rimozione, prompt)
    updateNode: function (nodeId, newLabel) { nodes.update({ id: nodeId, label: newLabel }); },
    removeNode: function (nodeId) { nodes.remove({ id: nodeId }); },
    updateLink: function (linkId, newLabel) { edges.update({ id: linkId, label: newLabel }); },
    removeLink: function (linkId) { edges.remove({ id: linkId }); },
    promptForValue: function (message, defaultValue) {
        const value = prompt(message, defaultValue);
        // Se l'utente preme "Annulla", il prompt restituisce null. In questo caso, restituiamo il valore di default.
        return value === null ? defaultValue : value;
    },

    // Funzioni per la visualizzazione dei risultati (invariate)
    resetHighlight: function () {
        let allNodes = nodes.get({ fields: ['id'] });
        let allEdges = edges.get({ fields: ['id'] });
        let updatedNodes = allNodes.map(node => ({ id: node.id, color: null })); // Rimuove il colore custom per tornare al default
        let updatedEdges = allEdges.map(edge => ({ id: edge.id, color: null, width: null }));
        if (updatedNodes.length > 0) nodes.update(updatedNodes);
        if (updatedEdges.length > 0) edges.update(updatedEdges);
    },
    highlightPath: function (pathNodeIds) {
        if (network) { network.selectNodes([]); }
        window.networkGraph.resetHighlight();
        if (!pathNodeIds || pathNodeIds.length === 0) { return; }
        let updatedNodes = pathNodeIds.map(nodeId => ({ id: nodeId, color: { background: highlightColor, border: '#146c43' } }));
        nodes.update(updatedNodes);
        for (let i = 0; i < pathNodeIds.length - 1; i++) {
            const fromNode = pathNodeIds[i];
            const toNode = pathNodeIds[i + 1];
            const connectedEdges = edges.get({ filter: item => (item.from == fromNode && item.to == toNode) || (item.from == toNode && item.to == fromNode) });
            if (connectedEdges.length > 0) {
                edges.update({ id: connectedEdges[0].id, color: { color: highlightColor }, width: 3 });
            }
        }
    }
};

function highlightPath(nodeIds, color, width) {
    for (let i = 0; i < nodeIds.length - 1; i++) {
        highlightLink(nodeIds[i], nodeIds[i + 1], color, width);
    }
}

function highlightLink(fromNode, toNode, color, width) {
    const edge = findEdge(fromNode, toNode);
    if (edge) {
        edges.update({ id: edge.id, color: { color: color }, width: width });
    }
}

function findEdge(fromNode, toNode) {
    const connectedEdges = edges.get({
        filter: item => (item.from == fromNode && item.to == toNode) || (item.from == toNode && item.to == fromNode)
    });
    return connectedEdges.length > 0 ? connectedEdges[0] : null;
}