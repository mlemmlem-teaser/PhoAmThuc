// ===== File JavaScript dùng chung cho mọi trang (được nạp trong _Layout.cshtml) =====

// Gọi API của website. Mọi API trả về dạng { ok: true/false, message: "...", data: ... }
class Api {
    static async sendRequest(method, url, data) {
        const options = {
            method: method,
            headers: { "Content-Type": "application/json" }
        };
        if (data !== undefined) {
            options.body = JSON.stringify(data);
        }

        try {
            const response = await fetch(url, options);
            return await response.json();
        } catch (error) {
            return { ok: false, message: "Không kết nối được với máy chủ. Bạn thử lại sau." };
        }
    }

    static getData(url) {
        return Api.sendRequest("GET", url);
    }

    static postData(url, data) {
        return Api.sendRequest("POST", url, data);
    }

    static putData(url, data) {
        return Api.sendRequest("PUT", url, data);
    }

    static deleteData(url) {
        return Api.sendRequest("DELETE", url);
    }

    // Tải một file (ảnh) lên máy chủ. Gửi dạng form nên không đặt Content-Type thủ công
    static async uploadFile(url, file) {
        const formData = new FormData();
        formData.append("file", file);

        try {
            const response = await fetch(url, { method: "POST", body: formData });
            return await response.json();
        } catch (error) {
            return { ok: false, message: "Không tải được ảnh lên. Bạn thử lại sau." };
        }
    }
}

// Hiện và ẩn khung thông báo (thẻ có class "notice"). isGood = true là màu xanh (thành công), false là màu đỏ (lỗi)
class Notice {
    static show(element, text, isGood) {
        element.textContent = text;
        element.classList.toggle("notice-good", isGood);
        element.classList.toggle("notice-error", !isGood);
        element.classList.remove("hidden");
    }

    static hide(element) {
        element.classList.add("hidden");
    }
}

// Đọc tham số trên thanh địa chỉ, ví dụ ?page=2
class UrlParam {
    static getValue(name) {
        const params = new URLSearchParams(window.location.search);
        return params.get(name);
    }
}

// Lấy một bản sao của phần tử nằm trong thẻ <template> (khai báo trong file .cshtml)
class Template {
    static createItem(templateId) {
        const template = document.getElementById(templateId);
        return template.content.firstElementChild.cloneNode(true);
    }
}

// Các hàm định dạng dữ liệu để hiển thị
class Format {
    // Ví dụ: "8.5/10 (12 lượt)" hoặc "Chưa có đánh giá"
    static formatRating(average, count) {
        if (average === null || average === undefined || count === 0) {
            return "Chưa có đánh giá";
        }
        return average + "/10 (" + count + " lượt)";
    }

    // Ví dụ: "19/10/2026 14:30"
    static formatDate(dateText) {
        const date = new Date(dateText);
        return date.toLocaleString("vi-VN", { dateStyle: "short", timeStyle: "short" });
    }

    // Chỉ lấy ngày, ví dụ: "19/10/2026"
    static formatDay(dateText) {
        return new Date(dateText).toLocaleDateString("vi-VN");
    }

    // Ví dụ: 1.5 thành "1,5", 200 thành "200"; không có số thì trả về chuỗi rỗng
    static formatAmount(amount) {
        if (amount === null || amount === undefined) {
            return "";
        }
        return Number(amount).toLocaleString("vi-VN");
    }

    static formatDifficulty(difficulty) {
        if (difficulty === "Easy") {
            return "Dễ";
        }
        if (difficulty === "Medium") {
            return "Trung bình";
        }
        if (difficulty === "Hard") {
            return "Khó";
        }
        return "";
    }

    static formatStatus(status) {
        if (status === "Draft") {
            return "Bản nháp";
        }
        if (status === "Public") {
            return "Công khai";
        }
        if (status === "Hidden") {
            return "Bị ẩn";
        }
        return "Đã xóa";
    }

    static formatCommentStatus(status) {
        if (status === "Visible") {
            return "Đang hiển thị";
        }
        if (status === "Hidden") {
            return "Đã ẩn";
        }
        return "Đã xóa";
    }
}

// Ảnh đại diện: có ảnh thì hiện ảnh, không có thì hiện chữ cái đầu của tên trong vòng tròn
class Avatar {
    static showAvatar(imageElement, holderElement, url, name) {
        if (url) {
            imageElement.src = url;
            imageElement.alt = name;
            imageElement.classList.remove("hidden");
            holderElement.classList.add("hidden");
        } else {
            holderElement.textContent = name.trim().charAt(0).toUpperCase();
            holderElement.classList.remove("hidden");
            imageElement.classList.add("hidden");
        }
    }
}

// Thanh điều hướng: nút mở danh sách trang trên điện thoại và tô sáng trang đang xem
class NavBar {
    start() {
        this.toggleButton = document.getElementById("NavToggle");
        this.navList = document.getElementById("NavList");

        this.toggleButton.addEventListener("click", () => this.toggleNavList());
        this.markActiveLink();
    }

    toggleNavList() {
        this.navList.classList.toggle("open");
    }

    // Link nào trùng đường dẫn (và tham số) của trang hiện tại thì thêm class "active"
    markActiveLink() {
        const path = window.location.pathname.toLowerCase();
        const search = window.location.search.toLowerCase();
        const links = this.navList.querySelectorAll(".navitem");

        for (const link of links) {
            const url = new URL(link.href);
            const samePath = url.pathname.toLowerCase() === path;
            const sameSearch = url.search.toLowerCase() === search;
            if (samePath && sameSearch) {
                link.classList.add("active");
            }
        }
    }
}

// Menu tài khoản ở góc trên bên phải (chỉ có khi đã đăng nhập)
class AccountMenu {
    start() {
        this.button = document.getElementById("AccountButton");
        if (this.button === null) {
            return; // chưa đăng nhập nên không có menu
        }

        this.panel = document.getElementById("AccountPanel");
        this.logoutButton = document.getElementById("LogoutButton");

        this.button.addEventListener("click", () => this.togglePanel());
        document.addEventListener("click", (event) => this.closePanelWhenClickOutside(event));
        this.logoutButton.addEventListener("click", () => this.logoutAccount());

        this.loadStats();
    }

    togglePanel() {
        this.panel.classList.toggle("open");
    }

    closePanelWhenClickOutside(event) {
        const clickInside = this.panel.contains(event.target) || this.button.contains(event.target);
        if (!clickInside) {
            this.panel.classList.remove("open");
        }
    }

    // Điểm đánh giá và số bài đăng cần tính trong database nên lấy qua API
    async loadStats() {
        const result = await Api.getData("/api/account/me");
        if (!result.ok) {
            return;
        }
        const data = result.data;
        document.getElementById("MenuRating").textContent = Format.formatRating(data.ratingAverage, data.ratingCount);
        document.getElementById("MenuPostCount").textContent = data.postCount;
    }

    async logoutAccount() {
        await Api.postData("/api/account/logout");
        window.location.href = "/";
    }
}

// Điểm bắt đầu: khởi động các phần chung của mọi trang
class SiteScript {
    start() {
        new NavBar().start();
        new AccountMenu().start();
    }
}

new SiteScript().start();
