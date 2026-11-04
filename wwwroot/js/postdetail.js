// ===== Trang xem một bài đăng (Views/Post/Detail.cshtml) =====

class PostDetailPage {
    start() {
        this.postId = document.getElementById("PostRoot").dataset.postid;
        this.post = null;
        this.loadPost();
    }

    async loadPost() {
        const result = await Api.getData("/api/post/" + this.postId);
        if (!result.ok) {
            this.showError(result.message);
            return;
        }

        this.post = result.data;
        this.showPost();
        this.countView();
    }

    // Mỗi lần mở trang tính một lượt xem (máy chủ chỉ ghi nhận với bài công khai)
    countView() {
        Api.postData("/api/post/" + this.postId + "/view");
    }

    showPost() {
        this.showHeader();
        this.showContent();
        this.showRecipe();
        this.showProvider();
        this.showOwnerActions();
        this.startComments();
        document.getElementById("PostBody").classList.remove("hidden");
    }

    // Chỉ bài công khai mới có bình luận và đánh giá
    startComments() {
        if (this.post.status !== "Public") {
            document.getElementById("CommentArea").classList.add("hidden");
            return;
        }
        const ratingElement = document.getElementById("PostRating");
        new CommentSection("post", this.post.id, ratingElement).start();
    }

    showHeader() {
        const post = this.post;
        document.getElementById("PostTitle").textContent = post.title;
        const author = document.getElementById("PostAuthor");
        author.textContent = post.author.displayName;
        author.href = "/Profile/Detail/" + post.author.id;
        document.getElementById("AuthorRole").textContent = "(" + post.author.roleName + ")";
        document.getElementById("PostDate").textContent = "· " + Format.formatDate(post.publishedAt ?? post.createdAt);
        document.getElementById("PostRating").textContent = Format.formatRating(post.ratingAverage, post.ratingCount);
        document.getElementById("PostViews").textContent = "· " + post.viewCount + " lượt xem";

        if (post.updatedAt) {
            document.getElementById("PostUpdated").textContent = "· Cập nhật: " + Format.formatDate(post.updatedAt);
        }

        PostCard.addLabels(document.getElementById("PostLabels"), post.labels);
        this.showStatusNotice();
    }

    // Bài chưa công khai chỉ tác giả và quản trị viên xem được, nhắc để họ biết
    showStatusNotice() {
        if (this.post.status === "Public") {
            return;
        }
        const notice = document.getElementById("StatusNotice");
        notice.textContent = "Bài đăng này đang ở trạng thái \"" + Format.formatStatus(this.post.status) + "\" nên người khác không nhìn thấy.";
        notice.classList.remove("hidden");
    }

    showContent() {
        const post = this.post;
        if (post.cover) {
            const cover = document.getElementById("PostCover");
            cover.src = post.cover;
            cover.alt = post.title;
            cover.classList.remove("hidden");
        }
        // contentHtml đã được máy chủ làm sạch nên an toàn khi gán vào innerHTML
        document.getElementById("PostContent").innerHTML = post.contentHtml;
    }

    showRecipe() {
        const recipe = this.post.recipe;
        if (recipe === null) {
            return;
        }

        document.getElementById("RecipeFacts").textContent = this.makeRecipeFacts(recipe);
        this.showIngredients(recipe.ingredients);
        this.showSteps(recipe.steps);

        if (recipe.note) {
            const note = document.getElementById("RecipeNote");
            note.textContent = "Ghi chú: " + recipe.note;
            note.classList.remove("hidden");
        }
        document.getElementById("RecipeBox").classList.remove("hidden");
    }

    // Ví dụ: "Chuẩn bị: 15 phút · Nấu: 30 phút · 4 khẩu phần · Độ khó: Dễ"
    makeRecipeFacts(recipe) {
        const facts = [];
        if (recipe.prepMinutes !== null) {
            facts.push("Chuẩn bị: " + recipe.prepMinutes + " phút");
        }
        if (recipe.cookMinutes !== null) {
            facts.push("Nấu: " + recipe.cookMinutes + " phút");
        }
        if (recipe.servings !== null) {
            facts.push(recipe.servings + " khẩu phần");
        }
        if (recipe.difficulty) {
            facts.push("Độ khó: " + Format.formatDifficulty(recipe.difficulty));
        }
        return facts.join(" · ");
    }

    showIngredients(ingredients) {
        const list = document.getElementById("IngredientList");
        for (const ingredient of ingredients) {
            const item = Template.createItem("IngredientTemplate");
            const amountText = Format.formatAmount(ingredient.amount) + " " + (ingredient.unit ?? "");
            item.querySelector(".ingredientamount").textContent = amountText.trim();
            item.querySelector(".ingredientname").textContent = ingredient.name;
            item.querySelector(".ingredientnote").textContent = ingredient.note ? "(" + ingredient.note + ")" : "";
            list.appendChild(item);
        }
    }

    showSteps(steps) {
        const list = document.getElementById("StepList");
        for (const step of steps) {
            const item = Template.createItem("StepTemplate");
            item.querySelector(".steptext").textContent = step.content;
            if (step.image) {
                const image = item.querySelector(".stepimage");
                image.src = step.image;
                image.classList.remove("hidden");
            }
            list.appendChild(item);
        }
    }

    // Bài quảng bá hiện thêm thông tin nhà cung cấp
    showProvider() {
        const provider = this.post.provider;
        if (provider === null) {
            return;
        }

        document.getElementById("ProviderName").textContent = provider.businessName;
        document.getElementById("ProviderLink").href = "/Profile/Detail/" + provider.accountId;
        document.getElementById("ProviderDescription").textContent = provider.description ?? "";
        this.showProviderLine("ProviderAddress", "Địa chỉ: ", provider.address);
        this.showProviderLine("ProviderPhone", "Điện thoại: ", provider.phone);
        this.showProviderLine("ProviderEmail", "Email: ", provider.contactEmail);

        const website = document.getElementById("ProviderWebsite");
        // Chỉ nhận đường dẫn http hoặc https để tránh đường dẫn nguy hiểm
        if (provider.website && /^https?:\/\//i.test(provider.website)) {
            website.href = provider.website;
            website.textContent = provider.website;
        }

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

    showOwnerActions() {
        if (!this.post.canEdit) {
            return;
        }
        document.getElementById("EditLink").href = "/Post/Edit/" + this.post.id;
        document.getElementById("DeleteButton").addEventListener("click", () => this.deletePost());
        document.getElementById("OwnerActions").classList.remove("hidden");
    }

    async deletePost() {
        if (!confirm("Bạn chắc chắn muốn xóa bài đăng này?")) {
            return;
        }

        const result = await Api.deleteData("/api/post/" + this.post.id);
        if (!result.ok) {
            this.showError(result.message);
            return;
        }
        window.location.href = "/Post/Mine";
    }

    showError(text) {
        const message = document.getElementById("PostMessage");
        message.textContent = text;
        message.classList.remove("hidden");
    }
}

new PostDetailPage().start();
