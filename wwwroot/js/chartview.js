// ===== Vẽ biểu đồ bằng thư viện Chart.js (file wwwroot/lib/chartjs/chart.umd.js) =====
// Class này gói các lệnh của Chart.js lại thành vài hàm đơn giản. Dùng cho khu nhà cung cấp và (sau này) trang quản trị.
// Nếu chưa cài Chart.js thì ChartView.isReady() trả về false, trang phải tự bỏ qua việc vẽ.

class ChartView {
    constructor() {
        // Nhớ các biểu đồ đã vẽ theo id của canvas, vẽ lại thì hủy biểu đồ cũ trước
        this.charts = {};
    }

    static isReady() {
        return typeof Chart !== "undefined";
    }

    // Lấy màu từ biến CSS ở đầu site.css, ví dụ ChartView.getColor("--main-color")
    static getColor(variableName) {
        return getComputedStyle(document.documentElement).getPropertyValue(variableName).trim();
    }

    drawChart(canvasId, config) {
        if (this.charts[canvasId] !== undefined) {
            this.charts[canvasId].destroy();
        }
        const canvas = document.getElementById(canvasId);
        this.charts[canvasId] = new Chart(canvas, config);
    }

    // Biểu đồ đường. lines: [{ label: "Tên đường", values: [số...], color: "#c8651b" }, ...]
    drawLine(canvasId, labels, lines) {
        const datasets = lines.map((line) => ({
            label: line.label,
            data: line.values,
            borderColor: line.color,
            backgroundColor: line.color,
            tension: 0.25,
            pointRadius: 2
        }));

        this.drawChart(canvasId, {
            type: "line",
            data: { labels: labels, datasets: datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    }

    // Biểu đồ cột. isHorizontal = true thì vẽ cột nằm ngang (hợp với tên dài)
    drawBar(canvasId, labels, label, values, color, isHorizontal) {
        const valueAxis = { beginAtZero: true, ticks: { precision: 0 } };

        this.drawChart(canvasId, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{ label: label, data: values, backgroundColor: color }]
            },
            options: {
                indexAxis: isHorizontal ? "y" : "x",
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: isHorizontal ? { x: valueAxis } : { y: valueAxis }
            }
        });
    }
}
