// ===== Trang chủ (Views/Home/Index.cshtml): các khu đề xuất =====

class HomePage {
    start() {
        this.message = document.getElementById("HomeMessage");
        this.loadSummary();
    }

    async loadSummary() {
        const result = await Api.getData("/api/home/summary");
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }

        const data = result.data;
        this.showPosts("TopPosts", data.topPosts);
        this.showPosts("MonthPosts", data.monthPosts);
        this.showProfiles("FeaturedProfiles", data.profiles);
        this.showPosts("LatestRecipes", data.recipes);
    }

    // Hiện các thẻ bài đăng vào một khu. Khu nào không có gì thì ẩn cả khu
    showPosts(containerId, posts) {
        const container = document.getElementById(containerId);
        for (const post of posts) {
            container.appendChild(PostCard.createCard(post));
        }
        this.showSection(container, posts.length);
    }

    showProfiles(containerId, profiles) {
        const container = document.getElementById(containerId);
        for (const profile of profiles) {
            container.appendChild(this.createProfileCard(profile));
        }
        this.showSection(container, profiles.length);
    }

    createProfileCard(profile) {
        const card = Template.createItem("ProfileCardTemplate");
        card.href = "/Profile/Detail/" + profile.id;
        card.querySelector(".profilecardname").textContent = profile.displayName;
        card.querySelector(".profilecardrole").textContent = profile.roleName;
        card.querySelector(".profilecardrating").textContent = Format.formatRating(profile.ratingAverage, profile.ratingCount);
        Avatar.showAvatar(card.querySelector(".avatar"), card.querySelector(".avatarholder"), profile.avatar, profile.displayName);
        return card;
    }

    showSection(container, itemCount) {
        if (itemCount > 0) {
            container.closest(".homesection").classList.remove("hidden");
        }
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
    }
}

new HomePage().start();
