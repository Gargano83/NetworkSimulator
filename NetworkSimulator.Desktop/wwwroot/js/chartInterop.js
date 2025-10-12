let charts = {};

window.chartInterop = {
    createOrUpdateChart: function (canvasId, labels, datasets, xTitle, yTitle) {
        // Controlla se un grafico per questo canvas esiste già
        if (charts[canvasId]) {
            // --- SE ESISTE, FAI UN AGGIORNAMENTO LEGGERO ---
            const chart = charts[canvasId];

            // 1. Aggiorna i dati e le etichette
            chart.data.labels = labels;
            chart.data.datasets = datasets;

            // 2. Chiama il metodo .update() per un ridisegno pulito
            chart.update();

        } else {
            // --- SE NON ESISTE, CREALO DA ZERO (solo la prima volta) ---
            const ctx = document.getElementById(canvasId);
            if (!ctx) return;

            charts[canvasId] = new Chart(ctx.getContext('2d'), {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: datasets
                },
                options: {
                    scales: {
                        x: {
                            title: { display: true, text: xTitle }
                        },
                        y: {
                            beginAtZero: true,
                            title: { display: true, text: yTitle }
                        }
                    },
                    // Disabilitiamo le animazioni per gli aggiornamenti in tempo reale
                    animation: {
                        duration: 0
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                title: function (context) {
                                    return 'Tempo: ' + context[0].label;
                                },
                                label: function (context) {
                                    let datasetLabel = context.dataset.label || '';
                                    let value = context.formattedValue;
                                    return `Tasso consegna (%): ${datasetLabel}: ${value}`;
                                }
                            }
                        }
                    }
                }
            });
        }
    }
};

window.exportCardAsImage = function (elementId, fileName) {
    const elementToCapture = document.getElementById(elementId);

    if (elementToCapture) {
        html2canvas(elementToCapture, {
            useCORS: true, // Permette di renderizzare immagini esterne se ce ne fossero
            allowTaint: true,
            scale: 2 // Aumenta la risoluzione dell'immagine per una qualità migliore
        }).then(canvas => {
            // Crea un link temporaneo
            const link = document.createElement('a');
            link.download = fileName;
            link.href = canvas.toDataURL('image/png');

            // Simula il click sul link per avviare il download
            link.click();
        });
    } else {
        console.error('Elemento non trovato:', elementId);
    }
}