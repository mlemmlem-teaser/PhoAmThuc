// ===== Trang quản trị (Views/Admin/Index.cshtml) =====
// Mỗi thẻ là một class riêng ở các file admin*.js. TabControl (tabcontrol.js) lo việc chuyển thẻ.

class AdminPage {
    start() {
        // Tên thẻ lấy từ data-tab của các nút. Mỗi thẻ có class với hàm activate() chỉ tải dữ liệu lần đầu mở
        const controllers = {
            dashboard: new AdminDashboardTab(),
            accounts: new AdminAccountsTab(),
            posts: new AdminPostsTab(),
            comments: new AdminCommentsTab(),
            providers: new AdminProvidersTab(),
            labels: new AdminLabelsTab(),
            pages: new AdminInfoTab(),
            settings: new AdminSettingsTab()
        };

        new TabControl("TabBar", controllers).start("dashboard");
    }
}

new AdminPage().start();
