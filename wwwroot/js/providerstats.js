// ===== Thẻ "Thống kê của tôi" và "Thị trường" trong khu vực nhà cung cấp (Views/Provider/Manage.cshtml) =====
// Các con số luôn hiện. Biểu đồ chỉ vẽ khi đã cài Chart.js (ChartView.isReady())

class ProviderStatsTab {
    constructor() {
        this.isStarted = false;
        this.chartView = new ChartView();
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.daysSelect = document.getElementById("DaysSelect");
        this.message = document.getElementById("StatsMessage");
        this.chartNotice = document.getElementById("ChartNotice");

        this.daysSelect.addEventListener("change", () => this.loadStats());
        this.loadStats();
    }

    async loadStats() {
        const result = await Api.getData("/api/provider/stats?days=" + this.daysSelect.value);
        if (!result.ok) {
            this.message.textContent = result.message;
            this.message.classList.remove("hidden");
            return;
        }
        this.message.classList.add("hidden");

        this.showTotals(result.data.totals);
        this.showCharts(result.data);
    }

    showTotals(totals) {
        document.getElementById("StatProfileViews").textContent = totals.profileViews;
        document.getElementById("StatPostViews").textContent = totals.postViews;
        document.getElementById("StatRating").textContent = Format.formatRating(totals.ratingAverage, totals.ratingCount);
        document.getElementById("StatPostComments").textContent = totals.postComments;
        document.getElementById("StatPostRatings").textContent = totals.postRatings;
    }

    showCharts(data) {
        if (!ChartView.isReady()) {
            this.chartNotice.classList.remove("hidden");
            return;
        }
        this.chartNotice.classList.add("hidden");

        this.drawViewsChart(data.days);
        this.drawRatingChart(data.ratingDistribution);
        this.drawTopPostsChart(data.topPosts);
    }

    // Hai đường: lượt xem hồ sơ và lượt xem bài đăng theo từng ngày
    drawViewsChart(days) {
        const labels = days.map((day) => day.date.slice(8, 10) + "/" + day.date.slice(5, 7));
        this.chartView.drawLine("ViewsChart", labels, [
            { label: "Lượt xem hồ sơ", values: days.map((day) => day.profileViews), color: ChartView.getColor("--main-color") },
            { label: "Lượt xem bài đăng", values: days.map((day) => day.postViews), color: ChartView.getColor("--good-color") }
        ]);
    }

    // Cột: số lượt đánh giá hồ sơ cho từng mức điểm từ 1 đến 10
    drawRatingChart(distribution) {
        const labels = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];
        this.chartView.drawBar("RatingChart", labels, "Số lượt đánh giá", distribution, ChartView.getColor("--main-color"), false);
    }

    drawTopPostsChart(topPosts) {
        const labels = topPosts.map((post) => this.shortenText(post.title, 28));
        const values = topPosts.map((post) => post.viewCount);
        this.chartView.drawBar("TopPostsChart", labels, "Lượt xem", values, ChartView.getColor("--main-dark"), true);
    }

    shortenText(text, maxLength) {
        if (text.length <= maxLength) {
            return text;
        }
        return text.substring(0, maxLength - 1) + "…";
    }
}

class ProviderMarketTab {
    constructor() {
        this.isStarted = false;
        this.chartView = new ChartView();
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;
        this.message = document.getElementById("MarketMessage");
        this.loadMarket();
    }

    async loadMarket() {
        const result = await Api.getData("/api/provider/market");
        if (!result.ok) {
            this.message.textContent = result.message;
            this.message.classList.remove("hidden");
            return;
        }

        const data = result.data;
        document.getElementById("MarketTotal").textContent = data.totalPosts;
        this.fillList("TypesList", data.types);
        this.fillList("LabelsList", data.topLabels);

        if (ChartView.isReady()) {
            this.drawCharts(data);
        }
    }

    // Danh sách chữ luôn hiện, dùng thay cho biểu đồ khi chưa cài Chart.js
    fillList(listId, items) {
        const list = document.getElementById(listId);
        list.innerHTML = "";
        if (items.length === 0) {
            list.textContent = "Chưa có dữ liệu.";
            return;
        }
        for (const item of items) {
            const line = document.createElement("li");
            line.textContent = item.name + ": " + item.count + " bài";
            list.appendChild(line);
        }
    }

    drawCharts(data) {
        const typeLabels = data.types.map((item) => item.name);
        const typeValues = data.types.map((item) => item.count);
        this.chartView.drawBar("TypesChart", typeLabels, "Số bài", typeValues, ChartView.getColor("--main-color"), false);

        const labelNames = data.topLabels.map((item) => item.name);
        const labelValues = data.topLabels.map((item) => item.count);
        this.chartView.drawBar("LabelsChart", labelNames, "Số bài", labelValues, ChartView.getColor("--main-dark"), true);
    }
}
