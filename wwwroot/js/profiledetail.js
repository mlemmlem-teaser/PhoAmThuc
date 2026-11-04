// ===== Trang hồ sơ công khai (Views/Profile/Detail.cshtml) =====

class ProfileDetailPage {
    start() {
        this.profileId = parseInt(document.getElementById("ProfileRoot").dataset.profileid);
        this.page = parseInt(UrlParam.getValue("page")) || 1;
        this.profile = null;
        this.loadProfile();
    }

    async loadProfile() {
        const result = await Api.getData("/api/profile/" + this.profileId);
        if (!result.ok) {
            this.showError(result.message);
            return;
        }

        this.profile = result.data;
        this.showProfile();
        this.showProvider();
        document.getElementById("ProfileBody").classList.remove("hidden");

        this.countView();
        this.loadPosts();
        // Khu đánh giá hồ sơ dùng chung với bài đăng (commentsection.js)
        new CommentSection("profile", this.profileId, document.getElementById("ProfileRating")).start();
    }

    // Mỗi lần mở trang tính một lượt xem hồ sơ
    countView() {
        Api.postData("/api/profile/" + this.profileId + "/view");
    }

    showProfile() {
        const profile = this.profile;
        document.getElementById("ProfileName").textContent = profile.displayName;
        document.getElementById("ProfileRole").textContent = profile.roleName;
        document.getElementById("ProfileJoined").textContent = "Tham gia từ " + Format.formatDay(profile.joinedAt);
        document.getElementById("ProfileRating").textContent = Format.formatRating(profile.ratingAverage, profile.ratingCount);
        document.getElementById("ProfilePosts").textContent = "· " + profile.postCount + " bài đăng";
        document.getElementById("ProfileViews").textContent = "· " + profile.viewCount + " lượt xem hồ sơ";
        Avatar.showAvatar(document.getElementById("ProfileAvatar"), document.getElementById("ProfileHolder"), profile.avatar, profile.displayName);

        if (profile.bio) {
            const bio = document.getElementById("ProfileBio");
            bio.textContent = profile.bio;
            bio.classList.remove("hidden");
        }
        if (profile.isMine) {
            document.getElementById("EditProfileLink").classList.remove("hidden");
        }
    }

    // Nhà cung cấp có hồ sơ công khai thì hiện thêm thông tin doanh nghiệp
    showProvider() {
        const provider = this.profile.provider;
        if (provider === null) {
            return;
        }

        document.getElementById("ProviderName").textContent = provider.businessName;
        document.getElementById("ProviderDescription").textContent = provider.description ?? "";
        this.showProviderLine("ProviderAddress", "Địa chỉ: ", provider.address);
        this.showProviderLine("ProviderPhone", "Điện thoại: ", provider.phone);
        this.showProviderLine("ProviderEmail", "Email: ", provider.contactEmail);
        this.showProviderLink("ProviderWebsite", provider.website);
        this.showProviderLink("ProviderSocial", provider.socialLink);

        if (provider.logo) {
            const logo = document.getElementById("ProviderLogo");
            logo.src = provider.logo;
            logo.classList.remove("hidden");
        }
        document.getElementById("ProviderBox").classList.remove("hidden");
    }

    showProviderLine(elementId, prefix, value) {
        const line = document.getElementById(elementId);
        if (value) {
            line.textContent = prefix + value;
        } else {
            line.classList.add("hidden");
        }
    }

    // Chỉ nhận đường dẫn http hoặc https để tránh đường dẫn nguy hiểm
    showProviderLink(elementId, url) {
        const link = document.getElementById(elementId);
        if (url && /^https?:\/\//i.test(url)) {
            link.href = url;
            link.textContent = url;
        } else {
            link.parentElement.classList.add("hidden");
        }
    }

    // Danh sách bài đăng công khai của hồ sơ này (dùng lại API danh sách bài, lọc theo người đăng)
    async loadPosts() {
        const url = "/api/post/list?authorId=" + this.profileId + "&page=" + this.page;
        const result = await Api.getData(url);
        if (!result.ok) {
            this.showError(result.message);
            return;
        }

        const listBox = document.getElementById("PostList");
        listBox.innerHTML = "";
        document.getElementById("EmptyNote").classList.toggle("hidden", result.data.items.length > 0);
        for (const post of result.data.items) {
            listBox.appendChild(PostCard.createCard(post));
        }

        const makeUrl = (page) => "/Profile/Detail/" + this.profileId + "?page=" + page;
        Pager.showPages(document.getElementById("Pager"), result.data.page, result.data.totalPages, makeUrl);
    }

    showError(text) {
        const message = document.getElementById("ProfileMessage");
        message.textContent = text;
        message.classList.remove("hidden");
    }
}

new ProfileDetailPage().start();
