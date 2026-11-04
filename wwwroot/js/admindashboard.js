// ===== Thẻ "Tổng quan" của trang quản trị (Views/Admin/_Dashboard.cshtml) =====
// Các con số luôn hiện. Biểu đồ chỉ vẽ khi đã cài Chart.js (ChartView.isReady())

class AdminDashboardTab {
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

        this.daysSelect = document.getElementById("DashDays");
        this.message = document.getElementById("DashMessage");
        this.chartNotice = document.getElementById("DashChartNotice");

        this.daysSelect.addEventListener("change", () => this.loadDashboard());
        this.loadDashboard();
    }

    async loadDashboard() {
        const result = await Api.getData("/api/admin/dashboard?days=" + this.daysSelect.value);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }
        Notice.hide(this.message);

        const data = result.data;
        document.getElementById("DashAccounts").textContent = data.totals.accounts;
        document.getElementById("DashPosts").textContent = data.totals.publicPosts;
        document.getElementById("DashComments").textContent = data.totals.comments;
        document.getElementById("DashRatings").textContent = data.totals.ratings;
        this.showTopList(data.topPosts);

        if (!ChartView.isReady()) {
            this.chartNotice.classList.remove("hidden");
            return;
        }
        this.chartNotice.classList.add("hidden");
        this.drawCharts(data);
    }

    // Danh sách chữ các bài nhiều lượt xem nhất, kèm link, luôn hiện
    showTopList(topPosts) {
        const list = document.getElementById("DashTopList");
        list.innerHTML = "";
        if (topPosts.length === 0) {
            list.textContent = "Chưa có lượt xem bài đăng nào.";
            return;
        }

        for (const post of topPosts) {
            const item = document.createElement("li");
            const link = document.createElement("a");
            link.href = "/Post/Detail/" + post.id;
            link.textContent = post.title;
            item.appendChild(link);
            item.appendChild(document.createTextNode(" (" + post.viewCount + " lượt xem)"));
            list.appendChild(item);
        }
    }

    drawCharts(data) {
        const labels = data.days.map((day) => day.date.slice(8, 10) + "/" + day.date.slice(5, 7));
        this.chartView.drawLine("DashViewsChart", labels, [
            { label: "Lượt xem trang", values: data.days.map((day) => day.pageViews), color: ChartView.getColor("--main-color") },
            { label: "Lượt xem bài đăng", values: data.days.map((day) => day.postViews), color: ChartView.getColor("--good-color") },
            { label: "Lượt xem hồ sơ", values: data.days.map((day) => day.profileViews), color: "#1e6fb8" }
        ]);

        const typeNames = data.postTypes.map((item) => item.name);
        const typeValues = data.postTypes.map((item) => item.count);
        this.chartView.drawBar("DashTypesChart", typeNames, "Số bài", typeValues, ChartView.getColor("--main-color"), false);

        const topNames = data.topPosts.map((post) => this.shortenText(post.title, 28));
        const topValues = data.topPosts.map((post) => post.viewCount);
        this.chartView.drawBar("DashTopChart", topNames, "Lượt xem", topValues, ChartView.getColor("--main-dark"), true);
    }

    shortenText(text, maxLength) {
        if (text.length <= maxLength) {
            return text;
        }
        return text.substring(0, maxLength - 1) + "…";
    }
}
