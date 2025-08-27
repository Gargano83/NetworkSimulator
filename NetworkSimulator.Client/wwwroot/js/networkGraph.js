// Colori di default e per l'evidenziazione
const defaultNodeColor = '#0d6efd'; // Blu Bootstrap
const highlightColor = '#198754';   // Verde Bootstrap

// Variabili globali per gestire lo stato
let network = null;
let nodes = new vis.DataSet([]);
let edges = new vis.DataSet([]);

window.networkGraph = {
    init: function (containerId, dotNetHelper) {
        let container = document.getElementById(containerId);
        let data = {
            nodes: nodes,
            edges: edges,
        };
        let options = {
            // Qui è possibile configurare l'aspetto e il comportamento del grafo
            interaction: {
                dragNodes: true,
            },
            physics: {
                enabled: true,
            },
        };
        network = new vis.Network(container, data, options);

        network.on("click", function (params) {
            // Se l'utente ha cliccato su un nodo...
            if (params.nodes.length > 0) {
                // ...invochiamo il metodo C#!
                dotNetHelper.invokeMethodAsync('HandleNodeClick', params.nodes[0]);
            }
            else if (params.edges.length > 0) {
                dotNetHelper.invokeMethodAsync('HandleLinkClick', params.edges[0]);
            }
        });

        network.on("doubleClick", function (params) {
            // Se l'utente ha fatto doppio click su un arco...
            if (params.edges.length > 0) {
                // ...invochiamo un nuovo metodo C# per la modifica
                dotNetHelper.invokeMethodAsync('HandleLinkDoubleClick', params.edges[0]);
            }
            else if (params.nodes.length > 0) {
                dotNetHelper.invokeMethodAsync('HandleNodeDoubleClick', params.nodes[0]);
            }
        });
    },

    addNode: function (node) {
        try {
            nodes.add(node);
        } catch (err) {
            console.error(err);
        }
    },

    removeNode: function (nodeId) {
        nodes.remove({ id: nodeId });
    },

    promptForLabel: function (currentLabel) {
        const newLabel = prompt("Inserisci la nuova etichetta per il nodo:", currentLabel);
        // Restituisce la nuova etichetta o quella vecchia se l'utente annulla
        return newLabel === null ? currentLabel : newLabel;
    },

    updateNode: function (nodeId, newLabel) {
        nodes.update({
            id: nodeId,
            label: newLabel
        });
    },

    addLink: function (link) {
        try {
            edges.add(link);
        } catch (err) {
            console.error(err);
        }
    },

    updateLink: function (linkId, newWeight) {
        edges.update({
            id: linkId,
            label: newWeight.toString()
        });
    },

    removeLink: function (linkId) {
        edges.remove({ id: linkId });
    },

    promptForWeight: function (fromNode, toNode) {
        // Usiamo il prompt base del browser per semplicità
        const weight = prompt(`Inserisci il peso per il collegamento da ${fromNode} a ${toNode}:`, "1");
        // Convertiamo in numero e restituiamo un valore di default se l'input non è valido
        const parsedWeight = parseFloat(weight);
        return isNaN(parsedWeight) ? 1.0 : parsedWeight;
    },

    resetHighlight: function () {
        // Resetta tutti i nodi e gli archi al loro colore di default
        let allNodes = nodes.get({ fields: ['id'] });
        let allEdges = edges.get({ fields: ['id'] });

        let updatedNodes = allNodes.map(node => ({ id: node.id, color: { background: defaultNodeColor, border: '#0a58ca' } }));
        let updatedEdges = allEdges.map(edge => ({ id: edge.id, color: { color: '#848484' } }));

        if (updatedNodes.length > 0) nodes.update(updatedNodes);
        if (updatedEdges.length > 0) edges.update(updatedEdges);
    },

    highlightPath: function (pathNodeIds) {
        // Deseleziona qualsiasi nodo o arco prima di iniziare. Questo risolve il problema del nodo di destinazione selezionato.
        if (network) {
            network.selectNodes([]);
        }

        // Prima resetta ogni evidenziazione precedente
        window.networkGraph.resetHighlight();

        if (!pathNodeIds || pathNodeIds.length === 0) {
            return; // Non fare nulla se il percorso è vuoto
        }

        // Evidenzia tutti i nodi del percorso, inclusa la destinazione.
        let updatedNodes = pathNodeIds.map(nodeId => ({
            id: nodeId,
            color: { background: highlightColor, border: '#146c43' }
        }));
        nodes.update(updatedNodes);

        // Evidenzia gli archi del percorso
        for (let i = 0; i < pathNodeIds.length - 1; i++) {
            const fromNode = pathNodeIds[i];
            const toNode = pathNodeIds[i + 1];

            // Trova l'arco che collega i due nodi nel percorso
            const connectedEdges = edges.get({
                filter: function (item) {
                    // Controlla in entrambe le direzioni per sicurezza
                    return (item.from == fromNode && item.to == toNode) ||
                        (item.from == toNode && item.to == fromNode);
                }
            });

            // --- QUESTA È LA MODIFICA CHIAVE ---
            // Aggiorna l'arco solo SE ne è stato trovato uno.
            // Questo previene errori che bloccano lo script.
            if (connectedEdges.length > 0) {
                edges.update({
                    id: connectedEdges[0].id,
                    color: { color: highlightColor },
                    width: 3
                });
            }
        }
    }
};