// ===== Trang viết và sửa bài đăng (Views/Post/Editor.cshtml) =====
// Dùng chung cho tạo mới (postId = 0) và sửa bài (postId > 0)

class PostEditorPage {
    start() {
        this.postId = parseInt(document.getElementById("EditorRoot").dataset.postid);
        this.coverUrl = "";
        this.isSaving = false;
        this.labelData = null;

        this.findElements();
        this.editor = new RichEditor();
        this.editor.start();
        this.recipeForm = new RecipeForm();
        this.recipeForm.start();

        this.bindEvents();
        this.loadPage();
    }

    findElements() {
        this.titleInput = document.getElementById("TitleInput");
        this.summaryInput = document.getElementById("SummaryInput");
        this.coverFile = document.getElementById("CoverFile");
        this.coverPreview = document.getElementById("CoverPreview");
        this.coverRemove = document.getElementById("CoverRemove");
        this.labelList = document.getElementById("LabelList");
        this.recipeSection = document.getElementById("RecipeSection");
        this.isRecipeInput = document.getElementById("IsRecipeInput");
        this.recipeFields = document.getElementById("RecipeFields");
        this.saveDraftButton = document.getElementById("SaveDraftButton");
        this.publishButton = document.getElementById("PublishButton");
        this.message = document.getElementById("EditorMessage");
    }

    bindEvents() {
        this.coverFile.addEventListener("change", () => this.uploadCover());
        this.coverRemove.addEventListener("click", () => this.showCover(""));
        this.isRecipeInput.addEventListener("change", () => this.toggleRecipeFields());
        this.saveDraftButton.addEventListener("click", () => this.savePost("Draft"));
        this.publishButton.addEventListener("click", () => this.savePost("Public"));
    }

    // ----- Tải dữ liệu ban đầu -----

    async loadPage() {
        const labelResult = await Api.getData("/api/post/labels");
        if (!labelResult.ok) {
            this.showMessage(labelResult.message);
            return;
        }
        this.labelData = labelResult.data;
        this.recipeSection.classList.toggle("hidden", !this.labelData.canRecipe);

        let post = null;
        if (this.postId > 0) {
            const postResult = await Api.getData("/api/post/" + this.postId);
            if (!postResult.ok) {
                this.showMessage(postResult.message);
                this.setButtonsDisabled(true);
                return;
            }
            post = postResult.data;
        }

        this.showLabels(post === null ? [] : post.labelIds);
        if (post !== null) {
            this.fillForm(post);
        } else {
            this.preselectPromo();
            this.checkCreateAllowed();
        }
    }

    // Chức năng đăng bài bị tắt thì báo ngay, không để người dùng soạn xong mới biết
    checkCreateAllowed() {
        if (!this.labelData.postFeatureOn) {
            this.showMessage("Chức năng đăng bài đang tạm đóng.");
            this.setButtonsDisabled(true);
        }
    }

    // Điền dữ liệu bài đã có vào biểu mẫu
    fillForm(post) {
        document.getElementById("EditorTitle").textContent = "Sửa bài đăng";
        this.titleInput.value = post.title;
        this.summaryInput.value = post.summary ?? "";
        this.editor.setHtml(post.content);
        this.showCover(post.cover ?? "");

        if (post.status === "Hidden") {
            this.showMessage("Bài đăng này đang bị quản trị viên ẩn. Bạn sửa được nhưng không tự công khai lại được.");
        }

        if (post.recipe !== null) {
            this.fillRecipe(post.recipe);
        }
    }

    // Bài có công thức mà người sửa không phải đầu bếp thì không cho lưu, tránh làm mất dữ liệu công thức
    fillRecipe(recipe) {
        if (!this.labelData.canRecipe) {
            this.showMessage("Bài đăng này có phần công thức, bạn không còn quyền sửa phần này.");
            this.setButtonsDisabled(true);
            return;
        }
        this.isRecipeInput.checked = true;
        this.recipeForm.setRecipeData(recipe);
        this.toggleRecipeFields();
    }

    // ----- Nhãn -----

    // Hiện danh sách nhãn. Nhãn Công thức nấu ăn không hiện vì hệ thống tự gắn khi bài có công thức
    showLabels(selectedIds) {
        this.labelList.innerHTML = "";

        for (const label of this.labelData.labels) {
            if (label.systemKey === "Recipe") {
                continue;
            }

            const option = Template.createItem("LabelOptionTemplate");
            option.dataset.key = label.systemKey ?? "";
            const check = option.querySelector(".labelcheck");
            check.value = label.id;
            check.checked = selectedIds.includes(label.id);
            option.querySelector(".labeltext").textContent = label.name;
            if (label.description) {
                option.title = label.description;
            }

            if (label.systemKey === "Promo" && !this.canUsePromo()) {
                check.disabled = true;
                option.querySelector(".labeltext").textContent = label.name + " (chỉ dành cho nhà cung cấp)";
            }
            this.labelList.appendChild(option);
        }
    }

    // Vào từ khu vực nhà cung cấp (?kind=promo) thì chọn sẵn nhãn Quảng bá
    preselectPromo() {
        if (UrlParam.getValue("kind") !== "promo") {
            return;
        }
        for (const option of this.labelList.querySelectorAll(".labeloption")) {
            if (option.dataset.key === "Promo") {
                option.querySelector(".labelcheck").checked = true;
            }
        }
    }

    canUsePromo() {
        return this.labelData.canPromo && this.labelData.promoFeatureOn;
    }

    getSelectedLabelIds() {
        const ids = [];
        for (const check of this.labelList.querySelectorAll(".labelcheck")) {
            if (check.checked) {
                ids.push(parseInt(check.value));
            }
        }
        return ids;
    }

    // ----- Ảnh đại diện -----

    async uploadCover() {
        const file = this.coverFile.files[0];
        if (file === undefined) {
            return;
        }

        const result = await Api.uploadFile("/api/upload/image", file);
        this.coverFile.value = "";
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.showCover(result.data.url);
    }

    // Truyền chuỗi rỗng để bỏ ảnh đại diện
    showCover(url) {
        this.coverUrl = url;
        if (url === "") {
            this.coverPreview.classList.add("hidden");
            this.coverRemove.classList.add("hidden");
        } else {
            this.coverPreview.src = url;
            this.coverPreview.classList.remove("hidden");
            this.coverRemove.classList.remove("hidden");
        }
    }

    toggleRecipeFields() {
        this.recipeFields.classList.toggle("hidden", !this.isRecipeInput.checked);
    }

    // ----- Lưu bài -----

    collectData(status) {
        return {
            title: this.titleInput.value.trim(),
            summary: this.summaryInput.value.trim(),
            content: this.editor.getHtml(),
            cover: this.coverUrl === "" ? null : this.coverUrl,
            status: status,
            labelIds: this.getSelectedLabelIds(),
            recipe: this.isRecipeInput.checked ? this.recipeForm.getRecipeData() : null
        };
    }

    async savePost(status) {
        if (this.isSaving) {
            return;
        }
        this.isSaving = true;
        this.setButtonsDisabled(true);
        this.hideMessage();

        const data = this.collectData(status);
        let result;
        if (this.postId === 0) {
            result = await Api.postData("/api/post", data);
        } else {
            result = await Api.putData("/api/post/" + this.postId, data);
        }

        if (!result.ok) {
            this.showMessage(result.message);
            this.isSaving = false;
            this.setButtonsDisabled(false);
            return;
        }

        // Bài công khai thì xem luôn, bài nháp thì về trang quản lý
        if (result.data.status === "Public") {
            window.location.href = "/Post/Detail/" + result.data.id;
        } else {
            window.location.href = "/Post/Mine";
        }
    }

    setButtonsDisabled(disabled) {
        this.saveDraftButton.disabled = disabled;
        this.publishButton.disabled = disabled;
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
        this.message.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    hideMessage() {
        this.message.classList.add("hidden");
    }
}

new PostEditorPage().start();
