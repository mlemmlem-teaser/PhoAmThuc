// ===== Khu vực nhà cung cấp (Views/Provider/Manage.cshtml) =====
// Trang có nhiều thẻ (tab): hồ sơ doanh nghiệp, bài quảng bá, thống kê, phản hồi, thị trường.
// Mỗi thẻ có một class riêng với hàm activate(), chỉ tải dữ liệu khi được mở lần đầu.

// Thẻ "Hồ sơ doanh nghiệp": biểu mẫu tạo và sửa hồ sơ
class ProviderProfileForm {
    constructor() {
        this.isStarted = false;
        this.logoUrl = "";
        this.isSaving = false;
    }

    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.nameInput = document.getElementById("BusinessNameInput");
        this.descriptionInput = document.getElementById("DescriptionInput");
        this.addressInput = document.getElementById("AddressInput");
        this.phoneInput = document.getElementById("PhoneInput");
        this.emailInput = document.getElementById("ContactEmailInput");
        this.websiteInput = document.getElementById("WebsiteInput");
        this.socialInput = document.getElementById("SocialInput");
        this.isPublicInput = document.getElementById("IsPublicInput");
        this.logoFile = document.getElementById("LogoFile");
        this.logoPreview = document.getElementById("LogoPreview");
        this.logoHolder = document.getElementById("LogoHolder");
        this.logoRemove = document.getElementById("LogoRemove");
        this.saveButton = document.getElementById("SaveBusinessButton");
        this.hint = document.getElementById("BusinessHint");
        this.message = document.getElementById("BusinessMessage");

        this.logoFile.addEventListener("change", () => this.uploadLogo());
        this.logoRemove.addEventListener("click", () => this.showLogo(""));
        this.nameInput.addEventListener("input", () => this.showLogo(this.logoUrl));
        this.saveButton.addEventListener("click", () => this.saveProfile());

        this.loadProfile();
    }

    async loadProfile() {
        const result = await Api.getData("/api/provider/mine");
        if (!result.ok) {
            this.showMessage(result.message, false);
            return;
        }

        const profile = result.data;
        if (profile === null) {
            this.hint.classList.remove("hidden");
            this.showLogo("");
            return;
        }

        this.nameInput.value = profile.businessName;
        this.descriptionInput.value = profile.description ?? "";
        this.addressInput.value = profile.address ?? "";
        this.phoneInput.value = profile.phone ?? "";
        this.emailInput.value = profile.contactEmail ?? "";
        this.websiteInput.value = profile.website ?? "";
        this.socialInput.value = profile.socialLink ?? "";
        this.isPublicInput.checked = profile.isPublic;
        this.showLogo(profile.logo ?? "");
    }

    async uploadLogo() {
        const file = this.logoFile.files[0];
        if (file === undefined) {
            return;
        }

        const result = await Api.uploadFile("/api/upload/image", file);
        this.logoFile.value = "";
        if (!result.ok) {
            this.showMessage(result.message, false);
            return;
        }
        this.showLogo(result.data.url);
    }

    // Truyền chuỗi rỗng để bỏ logo (lúc đó hiện chữ cái đầu của tên doanh nghiệp)
    showLogo(url) {
        this.logoUrl = url;
        const name = this.nameInput.value === "" ? "?" : this.nameInput.value;
        Avatar.showAvatar(this.logoPreview, this.logoHolder, url, name);
        this.logoRemove.classList.toggle("hidden", url === "");
    }

    async saveProfile() {
        if (this.isSaving) {
            return;
        }
        this.isSaving = true;
        this.saveButton.disabled = true;
        this.message.classList.add("hidden");

        const data = {
            businessName: this.nameInput.value.trim(),
            description: this.descriptionInput.value.trim(),
            logo: this.logoUrl === "" ? null : this.logoUrl,
            address: this.addressInput.value.trim(),
            phone: this.phoneInput.value.trim(),
            contactEmail: this.emailInput.value.trim(),
            website: this.websiteInput.value.trim(),
            socialLink: this.socialInput.value.trim(),
            isPublic: this.isPublicInput.checked
        };
        const result = await Api.putData("/api/provider/mine", data);

        this.isSaving = false;
        this.saveButton.disabled = false;
        if (result.ok) {
            this.hint.classList.add("hidden");
        }
        this.showMessage(result.message, result.ok);
    }

    // isGood = true: thông báo thành công (màu xanh), false: thông báo lỗi (màu đỏ)
    showMessage(text, isGood) {
        this.message.textContent = text;
        this.message.classList.toggle("notice-good", isGood);
        this.message.classList.toggle("notice-error", !isGood);
        this.message.classList.remove("hidden");
        this.message.scrollIntoView({ behavior: "smooth", block: "center" });
    }
}

// Điều khiển các thẻ: bấm nút thẻ nào thì hiện bảng của thẻ đó. Tên thẻ được nhớ trên đường dẫn (#stats...)
class ProviderManagePage {
    start() {
        const profileId = parseInt(document.getElementById("ManageRoot").dataset.profileid);

        // Thẻ nào có dữ liệu cần tải thì có controller (class có hàm activate), thẻ tĩnh để null
        this.controllers = {
            profile: new ProviderProfileForm(),
            promo: null,
            stats: new ProviderStatsTab(),
            feedback: new ProviderFeedbackTab(profileId),
            market: new ProviderMarketTab()
        };

        this.buttons = document.querySelectorAll("#TabBar .tabbutton");
        for (const button of this.buttons) {
            button.addEventListener("click", () => this.showTab(button.dataset.tab));
        }

        const wanted = window.location.hash.replace("#", "");
        this.showTab(this.controllers[wanted] !== undefined ? wanted : "profile");
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

new ProviderManagePage().start();
