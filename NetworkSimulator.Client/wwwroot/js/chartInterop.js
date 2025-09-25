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
                animation: { duration: 0 }
            }
        });
    }
};