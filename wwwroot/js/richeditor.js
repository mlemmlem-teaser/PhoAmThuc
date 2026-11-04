// ===== Trình soạn thảo nội dung bài đăng (vùng ContentEditor trong Views/Post/Editor.cshtml) =====
// Dùng thuộc tính contenteditable của trình duyệt. Nội dung cuối cùng vẫn được máy chủ làm sạch lại (HtmlCleaner).

class RichEditor {
    start() {
        this.area = document.getElementById("ContentEditor");
        this.toolbar = document.getElementById("EditorToolbar");
        this.fontSizeSelect = document.getElementById("FontSizeSelect");
        this.colorInput = document.getElementById("TextColorInput");
        this.imageButton = document.getElementById("InsertImageButton");
        this.videoButton = document.getElementById("InsertVideoButton");
        this.imageFile = document.getElementById("EditorImageFile");

        // Vị trí con trỏ/đoạn chữ đang chọn, nhớ lại để khi bấm vào ô chọn màu hay cỡ chữ không bị mất
        this.savedRange = null;

        this.bindEvents();
    }

    bindEvents() {
        document.addEventListener("selectionchange", () => this.saveSelection());

        // Bấm nút trên thanh công cụ không được làm mất đoạn chữ đang chọn
        this.toolbar.addEventListener("mousedown", (event) => {
            if (event.target.closest("button")) {
                event.preventDefault();
            }
        });
        this.toolbar.addEventListener("click", (event) => this.clickToolbar(event));

        this.fontSizeSelect.addEventListener("change", () => this.changeFontSize());
        this.colorInput.addEventListener("input", () => this.runCommand("foreColor", this.colorInput.value));
        this.imageButton.addEventListener("click", () => this.imageFile.click());
        this.imageFile.addEventListener("change", () => this.insertImage());
        this.videoButton.addEventListener("click", () => this.insertVideo());

        // Dán chữ từ nơi khác thì chỉ lấy chữ thường, bỏ định dạng lạ
        this.area.addEventListener("paste", (event) => this.pastePlainText(event));
    }

    // ----- Đọc và ghi nội dung -----

    getHtml() {
        return this.area.innerHTML;
    }

    setHtml(html) {
        this.area.innerHTML = html;
        this.prepareVideoBoxes();
    }

    // Ô video chỉ là khung giữ chỗ (mã video nằm trong data-youtube), không cho gõ chữ vào trong
    prepareVideoBoxes() {
        const boxes = this.area.querySelectorAll("[data-youtube]");
        for (const box of boxes) {
            box.classList.add("videoholder");
            box.setAttribute("contenteditable", "false");
        }
    }

    // ----- Thanh công cụ -----

    clickToolbar(event) {
        const button = event.target.closest("button[data-command]");
        if (button === null) {
            return;
        }
        // data-value (nếu có) là tham số của lệnh, ví dụ formatBlock với giá trị h2 để đổi đoạn thành tiêu đề
        this.runCommand(button.dataset.command, button.dataset.value ?? null);
    }

    changeFontSize() {
        const size = this.fontSizeSelect.value;
        if (size !== "") {
            this.runCommand("fontSize", size);
        }
        this.fontSizeSelect.value = "";
    }

    // Chạy một lệnh định dạng lên đoạn chữ đang chọn
    runCommand(command, value) {
        this.restoreSelection();
        this.area.focus();
        document.execCommand("styleWithCSS", false, true);
        document.execCommand(command, false, value);
    }

    // ----- Chèn ảnh và video -----

    async insertImage() {
        const file = this.imageFile.files[0];
        if (file === undefined) {
            return;
        }

        const result = await Api.uploadFile("/api/upload/image", file);
        this.imageFile.value = "";
        if (!result.ok) {
            alert(result.message);
            return;
        }
        this.runCommand("insertImage", result.data.url);
    }

    insertVideo() {
        const link = prompt("Dán đường dẫn video YouTube:");
        if (link === null || link.trim() === "") {
            return;
        }

        const videoId = this.findYoutubeId(link);
        if (videoId === null) {
            alert("Đường dẫn YouTube không hợp lệ.");
            return;
        }
        const html = "<div class=\"videoholder\" data-youtube=\"" + videoId + "\" contenteditable=\"false\"></div><p><br></p>";
        this.runCommand("insertHTML", html);
    }

    // Lấy mã video 11 ký tự từ các dạng đường dẫn YouTube phổ biến
    findYoutubeId(link) {
        const pattern = /(?:youtu\.be\/|youtube\.com\/(?:watch\?(?:.*&)?v=|embed\/|shorts\/))([A-Za-z0-9_-]{11})/;
        const match = link.match(pattern);
        if (match === null) {
            return null;
        }
        return match[1];
    }

    // ----- Giữ vị trí chọn và dán chữ -----

    saveSelection() {
        const selection = window.getSelection();
        if (selection.rangeCount === 0) {
            return;
        }
        if (this.area.contains(selection.anchorNode)) {
            this.savedRange = selection.getRangeAt(0).cloneRange();
        }
    }

    restoreSelection() {
        if (this.savedRange === null) {
            return;
        }
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(this.savedRange);
    }

    pastePlainText(event) {
        event.preventDefault();
        const text = event.clipboardData.getData("text/plain");
        document.execCommand("insertText", false, text);
    }
}
