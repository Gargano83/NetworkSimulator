let charts = {};

window.chartInterop = {
    createOrUpdateChart: function (canvasId, labels, datasets, xTitle, yTitle) {
        if (charts[canvasId]) {
            charts[canvasId].destroy();
        }
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
                        title: {
                            display: true,
                            text: xTitle
                        }
                    },
                    y: {
                        beginAtZero: true,
                        max: 100,
                        title: {
                            display: true,
                            text: yTitle
                        }
                    }
                },
                animation: { duration: 0 },
                plugins: {
                    tooltip: {
                        callbacks: {
                            // Funzione per personalizzare il titolo del tooltip (la prima riga)
                            title: function (context) {
                                return 'Tempo: ' + context[0].label;
                            },
                            // Funzione per personalizzare il corpo del tooltip (la seconda riga)
                            label: function (context) {
                                let datasetLabel = context.dataset.label || '';
                                let value = context.formattedValue;
                                // Componiamo la stringa finale
                                return `Tasso consegna (%): ${datasetLabel}: ${value}`;
                            }
                        }
                    }
                }
            }
        });
    }
};