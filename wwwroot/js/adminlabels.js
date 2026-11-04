// ===== Thẻ "Nhãn" của trang quản trị (Views/Admin/_Labels.cshtml) =====

class AdminLabelsTab {
    constructor() {
        this.isStarted = false;
        this.editingId = 0; // 0 nghĩa là đang thêm nhãn mới
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.message = document.getElementById("LbMessage");
        this.form = document.getElementById("LbForm");
        this.rows = document.getElementById("LbRows");

        document.getElementById("LbAddButton").addEventListener("click", () => this.openForm(null));
        document.getElementById("LbSave").addEventListener("click", () => this.saveLabel());
        document.getElementById("LbCancel").addEventListener("click", () => this.form.classList.add("hidden"));

        this.loadLabels();
    }

    async loadLabels() {
        const result = await Api.getData("/api/admin/labels");
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.rows.innerHTML = "";
        for (const label of result.data.items) {
            this.rows.appendChild(this.createRow(label));
        }
    }

    createRow(label) {
        const row = Template.createItem("LbRowTemplate");
        row.querySelector(".lbname").textContent = label.name;
        row.querySelector(".lbdesc").textContent = label.description ?? "";
        row.querySelector(".lbtype").textContent = label.systemKey === null ? "Nhãn thường" : "Nhãn hệ thống";
        row.querySelector(".lbcount").textContent = label.postCount;

        const status = row.querySelector(".lbstatus");
        status.textContent = label.isActive ? "Đang bật" : "Đã tắt";
        status.dataset.status = label.isActive ? "Public" : "Hidden";

        row.querySelector(".lbedit").addEventListener("click", () => this.openForm(label));
        const deleteButton = row.querySelector(".lbdelete");
        if (label.systemKey === null) {
            deleteButton.addEventListener("click", () => this.deleteLabel(label));
        } else {
            deleteButton.remove(); // nhãn hệ thống không xóa được
        }
        return row;
    }

    // Mở biểu mẫu để thêm (label = null) hoặc sửa một nhãn
    openForm(label) {
        const isNew = label === null;
        this.editingId = isNew ? 0 : label.id;

        document.getElementById("LbFormTitle").textContent = isNew ? "Thêm nhãn mới" : "Sửa nhãn";
        document.getElementById("LbName").value = isNew ? "" : label.name;
        document.getElementById("LbDescription").value = isNew ? "" : (label.description ?? "");
        document.getElementById("LbActive").checked = isNew ? true : label.isActive;
        // Nhãn hệ thống luôn phải bật
        document.getElementById("LbActive").disabled = !isNew && label.systemKey !== null;

        Notice.hide(this.message);
        this.form.classList.remove("hidden");
        this.form.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    async saveLabel() {
        const data = {
            name: document.getElementById("LbName").value.trim(),
            description: document.getElementById("LbDescription").value.trim(),
            isActive: document.getElementById("LbActive").checked
        };

        let result;
        if (this.editingId === 0) {
            result = await Api.postData("/api/admin/labels", data);
        } else {
            result = await Api.putData("/api/admin/labels/" + this.editingId, data);
        }

        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.form.classList.add("hidden");
            this.loadLabels();
        }
    }

    async deleteLabel(label) {
        if (!confirm("Xóa nhãn \"" + label.name + "\"?")) {
            return;
        }

        const result = await Api.deleteData("/api/admin/labels/" + label.id);
        Notice.show(this.message, result.message, result.ok);
        if (result.ok) {
            this.loadLabels();
        }
    }
}
