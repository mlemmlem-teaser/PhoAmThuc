// ===== Thẻ "Tài khoản" của trang quản trị (Views/Admin/_Accounts.cshtml) =====

class AdminAccountsTab {
    constructor() {
        this.isStarted = false;
        this.page = 1;
        this.editingId = 0; // 0 nghĩa là đang thêm tài khoản mới
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.message = document.getElementById("AccMessage");
        this.searchInput = document.getElementById("AccSearch");
        this.roleFilter = document.getElementById("AccRoleFilter");
        this.form = document.getElementById("AccForm");
        this.rows = document.getElementById("AccRows");
        this.emptyNote = document.getElementById("AccEmpty");
        this.pagerBox = document.getElementById("AccPager");

        document.getElementById("AccFilterButton").addEventListener("click", () => this.loadAccounts(1));
        this.searchInput.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                this.loadAccounts(1);
            }
        });
        document.getElementById("AccAddButton").addEventListener("click", () => this.openForm(null));
        document.getElementById("AccSave").addEventListener("click", () => this.saveAccount());
        document.getElementById("AccCancel").addEventListener("click", () => this.form.classList.add("hidden"));

        this.loadAccounts(1);
    }

    async loadAccounts(page) {
        const query = "page=" + page
            + "&q=" + encodeURIComponent(this.searchInput.value.trim())
            + "&role=" + encodeURIComponent(this.roleFilter.value);
        const result = await Api.getData("/api/admin/accounts?" + query);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.page = result.data.page;
        this.showRows(result.data.items);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, () => "#accounts", (newPage) => this.loadAccounts(newPage));
    }

    showRows(accounts) {
        this.rows.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", accounts.length > 0);
        for (const account of accounts) {
            this.rows.appendChild(this.createRow(account));
        }
    }

    createRow(account) {
        const row = Template.createItem("AccRowTemplate");
        row.querySelector(".accid").textContent = account.id;
        row.querySelector(".accusername").textContent = account.username;
        row.querySelector(".accname").textContent = account.displayName;
        row.querySelector(".accemail").textContent = account.email;
        row.querySelector(".accrole").textContent = account.roleName;
        row.querySelector(".accposts").textContent = account.postCount;

        const status = row.querySelector(".accstatus");
        status.textContent = account.isLocked ? "Đã khóa" : "Hoạt động";
        status.dataset.status = account.isLocked ? "Hidden" : "Public";

        row.querySelector(".accedit").addEventListener("click", () => this.openForm(account));
        const deleteButton = row.querySelector(".accdelete");
        if (account.isMe) {
            deleteButton.remove(); // không tự xóa chính mình
        } else {
            deleteButton.addEventListener("click", () => this.deleteAccount(account));
        }
        return row;
    }

    // Mở biểu mẫu để thêm (account = null) hoặc sửa một tài khoản
    openForm(account) {
        const isNew = account === null;
        this.editingId = isNew ? 0 : account.id;

        document.getElementById("AccFormTitle").textContent = isNew ? "Thêm tài khoản mới" : "Sửa tài khoản: " + account.username;
        document.getElementById("AccUsername").value = isNew ? "" : account.username;
        document.getElementById("AccUsername").disabled = !isNew; // không đổi tên đăng nhập
        document.getElementById("AccDisplayName").value = isNew ? "" : account.displayName;
        document.getElementById("AccEmail").value = isNew ? "" : account.email;
        document.getElementById("AccPassword").value = "";
        document.getElementById("AccPasswordHint").textContent = isNew ? "Bắt buộc, tối thiểu 4 ký tự." : "Để trống nếu không đổi mật khẩu.";
        document.getElementById("AccRole").value = isNew ? "User" : account.role;
        document.getElementById("AccLocked").checked = isNew ? false : account.isLocked;

        // Quản trị viên không được tự đổi vai trò hoặc tự khóa mình
        const isMe = !isNew && account.isMe;
        document.getElementById("AccRole").disabled = isMe;
        document.getElementById("AccLocked").disabled = isMe;

        Notice.hide(this.message);
        this.form.classList.remove("hidden");
        this.form.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    async saveAccount() {
        const isNew = this.editingId === 0;
        const data = {
            email: document.getElementById("AccEmail").value.trim(),
            displayName: document.getElementById("AccDisplayName").value.trim(),
            role: document.getElementById("AccRole").value,
            password: document.getElementById("AccPassword").value
        };

        let result;
        if (isNew) {
            data.username = document.getElementById("AccUsername").value.trim();
            result = await Api.postData("/api/admin/accounts", data);
        } else {
            data.isLocked = document.getElementById("AccLocked").checked;
            result = await Api.putData("/api/admin/accounts/" + this.editingId, data);
        }

        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.form.classList.add("hidden");
            this.loadAccounts(this.page);
        }
    }

    async deleteAccount(account) {
        if (!confirm("Xóa tài khoản \"" + account.username + "\"? Thao tác này không hoàn tác được.")) {
            return;
        }

        const result = await Api.deleteData("/api/admin/accounts/" + account.id);
        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.loadAccounts(this.page);
        }
    }
}
