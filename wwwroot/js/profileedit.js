// ===== Trang sửa hồ sơ của tôi (Views/Profile/Edit.cshtml) =====

class ProfileEditPage {
    start() {
        this.profileId = parseInt(document.getElementById("EditRoot").dataset.profileid);
        this.avatarUrl = "";
        this.isSaving = false;

        this.avatarFile = document.getElementById("AvatarFile");
        this.avatarPreview = document.getElementById("AvatarPreview");
        this.avatarHolder = document.getElementById("AvatarHolder");
        this.avatarRemove = document.getElementById("AvatarRemove");
        this.nameInput = document.getElementById("DisplayNameInput");
        this.bioInput = document.getElementById("BioInput");
        this.saveButton = document.getElementById("SaveProfileButton");
        this.message = document.getElementById("ProfileEditMessage");

        this.avatarFile.addEventListener("change", () => this.uploadAvatar());
        this.avatarRemove.addEventListener("click", () => this.showAvatar(""));
        this.nameInput.addEventListener("input", () => this.showAvatar(this.avatarUrl));
        this.saveButton.addEventListener("click", () => this.saveProfile());

        this.loadProfile();
    }

    async loadProfile() {
        const result = await Api.getData("/api/profile/" + this.profileId);
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }

        this.nameInput.value = result.data.displayName;
        this.bioInput.value = result.data.bio ?? "";
        this.showAvatar(result.data.avatar ?? "");
    }

    async uploadAvatar() {
        const file = this.avatarFile.files[0];
        if (file === undefined) {
            return;
        }

        const result = await Api.uploadFile("/api/upload/image", file);
        this.avatarFile.value = "";
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.showAvatar(result.data.url);
    }

    // Truyền chuỗi rỗng để bỏ ảnh (lúc đó hiện chữ cái đầu của tên)
    showAvatar(url) {
        this.avatarUrl = url;
        const name = this.nameInput.value === "" ? "?" : this.nameInput.value;
        Avatar.showAvatar(this.avatarPreview, this.avatarHolder, url, name);
        this.avatarRemove.classList.toggle("hidden", url === "");
    }

    async saveProfile() {
        if (this.isSaving) {
            return;
        }
        this.isSaving = true;
        this.saveButton.disabled = true;
        this.hideMessage();

        const data = {
            displayName: this.nameInput.value.trim(),
            bio: this.bioInput.value.trim(),
            avatar: this.avatarUrl === "" ? null : this.avatarUrl
        };
        const result = await Api.putData("/api/profile/me", data);

        if (!result.ok) {
            this.showMessage(result.message);
            this.isSaving = false;
            this.saveButton.disabled = false;
            return;
        }
        window.location.href = "/Profile/Detail/" + this.profileId;
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

new ProfileEditPage().start();
