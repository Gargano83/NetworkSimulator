let charts = {};

window.chartInterop = {
    createOrUpdateChart: function (canvasId, labels, datasets) {
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
                scales: { y: { beginAtZero: true, max: 100 } },
                animation: { duration: 0 }
            }
        });
    }
};