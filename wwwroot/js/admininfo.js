// ===== Thẻ "Trang thông tin" của trang quản trị (Views/Admin/_Pages.cshtml) =====
// Sửa nội dung các trang Giới thiệu, Liên hệ, Trợ giúp bằng trình soạn thảo RichEditor

class AdminInfoTab {
    constructor() {
        this.isStarted = false;
        this.editingId = 0;
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.message = document.getElementById("PgMessage");
        this.form = document.getElementById("PgForm");
        this.rows = document.getElementById("PgRows");
        this.titleInput = document.getElementById("PgTitle");
        this.publicInput = document.getElementById("PgPublic");

        this.editor = new RichEditor();
        this.editor.start();

        document.getElementById("PgSave").addEventListener("click", () => this.savePage());
        document.getElementById("PgCancel").addEventListener("click", () => this.form.classList.add("hidden"));

        this.loadPages();
    }

    async loadPages() {
        const result = await Api.getData("/api/admin/pages");
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.rows.innerHTML = "";
        for (const page of result.data.items) {
            this.rows.appendChild(this.createRow(page));
        }
    }

    createRow(page) {
        const row = Template.createItem("PgRowTemplate");
        row.querySelector(".pgtitle").textContent = page.title;
        row.querySelector(".pgslug").textContent = page.slug;
        row.querySelector(".pgpublic").textContent = page.isPublic ? "Có" : "Không";
        row.querySelector(".pgdate").textContent = page.updatedAt ? Format.formatDate(page.updatedAt) : "Chưa sửa";
        row.querySelector(".pgedit").addEventListener("click", () => this.openForm(page.id));
        return row;
    }

    async openForm(id) {
        const result = await Api.getData("/api/admin/pages/" + id);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        const page = result.data;
        this.editingId = page.id;
        document.getElementById("PgFormTitle").textContent = "Sửa trang: " + page.title;
        this.titleInput.value = page.title;
        this.editor.setHtml(page.content);
        this.publicInput.checked = page.isPublic;

        Notice.hide(this.message);
        this.form.classList.remove("hidden");
        this.form.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    async savePage() {
        const data = {
            title: this.titleInput.value.trim(),
            content: this.editor.getHtml(),
            isPublic: this.publicInput.checked
        };

        const result = await Api.putData("/api/admin/pages/" + this.editingId, data);
        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.form.classList.add("hidden");
            this.loadPages();
            window.scrollTo({ top: 0, behavior: "smooth" });
        }
    }
}
