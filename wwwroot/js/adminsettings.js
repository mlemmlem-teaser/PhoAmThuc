// ===== Thẻ "Cấu hình" của trang quản trị (Views/Admin/_Settings.cshtml) =====
// Bật hoặc tắt 5 chức năng: đăng ký, đăng bài, bình luận, đánh giá, bài quảng bá

class AdminSettingsTab {
    constructor() {
        this.isStarted = false;
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.message = document.getElementById("StMessage");
        this.list = document.getElementById("StList");
        this.loadSettings();
    }

    async loadSettings() {
        const result = await Api.getData("/api/admin/settings");
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.list.innerHTML = "";
        for (const setting of result.data.items) {
            this.list.appendChild(this.createItem(setting));
        }
    }

    createItem(setting) {
        const item = Template.createItem("StItemTemplate");
        const check = item.querySelector(".stcheck");
        check.checked = setting.enabled;
        item.querySelector(".sttitle").textContent = setting.title;
        item.querySelector(".stdate").textContent = setting.updatedAt ? "Đổi lần cuối: " + Format.formatDate(setting.updatedAt) : "";

        check.addEventListener("change", () => this.saveSetting(setting, check));
        return item;
    }

    async saveSetting(setting, check) {
        const result = await Api.putData("/api/admin/settings/" + setting.name, { enabled: check.checked });
        Notice.show(this.message, result.message, result.ok);

        if (!result.ok) {
            check.checked = !check.checked; // lưu lỗi thì trả công tắc về như cũ
            return;
        }
        this.loadSettings();
    }
}
