// ===== Điều khiển các thẻ (tab) trên một trang: bấm nút thẻ nào thì hiện bảng của thẻ đó =====
// Mỗi nút trong khung thẻ có data-tab (tên thẻ) và data-panel (id của bảng nội dung tương ứng).
// Tên thẻ đang mở được nhớ trên đường dẫn (ví dụ #accounts) để tải lại trang vẫn ở đúng thẻ.

class TabControl {
    // tabBarId: id của khung chứa các nút thẻ
    // controllers: { tênThẻ: đốiTượng có hàm activate() hoặc null nếu thẻ không cần tải dữ liệu }
    constructor(tabBarId, controllers) {
        this.buttons = document.querySelectorAll("#" + tabBarId + " .tabbutton");
        this.controllers = controllers;
    }

    start(defaultTab) {
        for (const button of this.buttons) {
            button.addEventListener("click", () => this.showTab(button.dataset.tab));
        }

        const wanted = window.location.hash.replace("#", "");
        this.showTab(this.controllers[wanted] !== undefined ? wanted : defaultTab);
    }

    showTab(tabName) {
        for (const button of this.buttons) {
            const isSelected = button.dataset.tab === tabName;
            button.classList.toggle("active", isSelected);
            document.getElementById(button.dataset.panel).classList.toggle("hidden", !isSelected);
        }

        const controller = this.controllers[tabName];
        if (controller !== null) {
            controller.activate();
        }
        window.history.replaceState(null, "", "#" + tabName);
    }
}
