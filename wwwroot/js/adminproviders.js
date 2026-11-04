// ===== Thẻ "Nhà cung cấp" của trang quản trị (Views/Admin/_Providers.cshtml) =====

class AdminProvidersTab {
    constructor() {
        this.isStarted = false;
        this.page = 1;
        this.editingAccountId = 0;
        this.logoUrl = ""; // đường dẫn logo hiện có của hồ sơ đang sửa
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.message = document.getElementById("PvMessage");
        this.searchInput = document.getElementById("PvSearch");
        this.newAccountSelect = document.getElementById("PvNewAccount");
        this.form = document.getElementById("PvForm");
        this.rows = document.getElementById("PvRows");
        this.emptyNote = document.getElementById("PvEmpty");
        this.pagerBox = document.getElementById("PvPager");

        document.getElementById("PvFilterButton").addEventListener("click", () => this.loadProviders(1));
        this.searchInput.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                this.loadProviders(1);
            }
        });
        document.getElementById("PvAddButton").addEventListener("click", () => this.startNewProfile());
        document.getElementById("PvSave").addEventListener("click", () => this.saveProfile());
        document.getElementById("PvCancel").addEventListener("click", () => this.form.classList.add("hidden"));

        this.loadProviders(1);
    }

    async loadProviders(page) {
        const query = "page=" + page + "&q=" + encodeURIComponent(this.searchInput.value.trim());
        const result = await Api.getData("/api/admin/providers?" + query);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.page = result.data.page;
        this.showRows(result.data.items);
        this.showAccountsWithoutProfile(result.data.withoutProfile);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, () => "#providers", (newPage) => this.loadProviders(newPage));
    }

    showRows(items) {
        this.rows.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", items.length > 0);
        for (const item of items) {
            this.rows.appendChild(this.createRow(item));
        }
    }

    createRow(item) {
        const row = Template.createItem("PvRowTemplate");
        const account = row.querySelector(".pvaccount");
        account.textContent = item.accountName + (item.isProvider ? "" : " (không còn là nhà cung cấp)");
        account.href = "/Profile/Detail/" + item.accountId;
        row.querySelector(".pvname").textContent = item.businessName;
        row.querySelector(".pvpublic").textContent = item.isPublic ? "Có" : "Không";

        const editButton = row.querySelector(".pvedit");
        if (item.isProvider) {
            editButton.addEventListener("click", () => this.openForm(item.accountId));
        } else {
            editButton.remove(); // tài khoản đã đổi vai trò thì chỉ còn xóa hồ sơ được
        }
        row.querySelector(".pvdelete").addEventListener("click", () => this.deleteProfile(item));
        return row;
    }

    // Danh sách các tài khoản nhà cung cấp chưa có hồ sơ, để chọn và tạo hồ sơ giúp
    showAccountsWithoutProfile(accounts) {
        this.newAccountSelect.innerHTML = "";
        const first = document.createElement("option");
        first.value = "";
        first.textContent = "Nhà cung cấp chưa có hồ sơ...";
        this.newAccountSelect.appendChild(first);

        for (const account of accounts) {
            const option = document.createElement("option");
            option.value = account.accountId;
            option.textContent = account.displayName;
            this.newAccountSelect.appendChild(option);
        }
    }

    startNewProfile() {
        const accountId = parseInt(this.newAccountSelect.value);
        if (isNaN(accountId)) {
            Notice.show(this.message, "Hãy chọn một tài khoản nhà cung cấp chưa có hồ sơ.", false);
            return;
        }
        this.fillForm(accountId, null);
    }

    async openForm(accountId) {
        const result = await Api.getData("/api/admin/providers/" + accountId);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }
        this.fillForm(accountId, result.data);
    }

    // profile = null khi tạo hồ sơ mới
    fillForm(accountId, profile) {
        this.editingAccountId = accountId;
        this.logoUrl = profile === null ? "" : (profile.logo ?? "");

        document.getElementById("PvFormTitle").textContent = profile === null ? "Tạo hồ sơ nhà cung cấp" : "Sửa hồ sơ nhà cung cấp";
        document.getElementById("PvName").value = profile === null ? "" : profile.businessName;
        document.getElementById("PvDescription").value = profile?.description ?? "";
        document.getElementById("PvAddress").value = profile?.address ?? "";
        document.getElementById("PvPhone").value = profile?.phone ?? "";
        document.getElementById("PvEmail").value = profile?.contactEmail ?? "";
        document.getElementById("PvWebsite").value = profile?.website ?? "";
        document.getElementById("PvSocial").value = profile?.socialLink ?? "";
        document.getElementById("PvPublic").checked = profile === null ? true : profile.isPublic;
        document.getElementById("PvClearLogo").checked = false;
        document.getElementById("PvLogoRow").classList.toggle("hidden", this.logoUrl === "");

        Notice.hide(this.message);
        this.form.classList.remove("hidden");
        this.form.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    async saveProfile() {
        const clearLogo = document.getElementById("PvClearLogo").checked;
        const data = {
            businessName: document.getElementById("PvName").value.trim(),
            description: document.getElementById("PvDescription").value.trim(),
            logo: (clearLogo || this.logoUrl === "") ? null : this.logoUrl,
            address: document.getElementById("PvAddress").value.trim(),
            phone: document.getElementById("PvPhone").value.trim(),
            contactEmail: document.getElementById("PvEmail").value.trim(),
            website: document.getElementById("PvWebsite").value.trim(),
            socialLink: document.getElementById("PvSocial").value.trim(),
            isPublic: document.getElementById("PvPublic").checked
        };

        const result = await Api.putData("/api/admin/providers/" + this.editingAccountId, data);
        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.form.classList.add("hidden");
            this.loadProviders(this.page);
        }
    }

    async deleteProfile(item) {
        if (!confirm("Xóa hồ sơ doanh nghiệp \"" + item.businessName + "\"? Tài khoản và bài đăng vẫn được giữ nguyên.")) {
            return;
        }

        const result = await Api.deleteData("/api/admin/providers/" + item.accountId);
        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.loadProviders(this.page);
        }
    }
}
