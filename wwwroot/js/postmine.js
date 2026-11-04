// ===== Trang quản lý bài đăng của tôi (Views/Post/Mine.cshtml) =====

class PostMinePage {
    start() {
        this.listBox = document.getElementById("MineList");
        this.emptyNote = document.getElementById("EmptyNote");
        this.pagerBox = document.getElementById("Pager");
        this.message = document.getElementById("MineMessage");

        this.params = new URLSearchParams(window.location.search);
        this.loadPosts();
    }

    async loadPosts() {
        const result = await Api.getData("/api/post/mine?" + this.params.toString());
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }

        this.showPosts(result.data.items);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, (page) => this.makePageUrl(page));
    }

    showPosts(posts) {
        this.listBox.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", posts.length > 0);

        for (const post of posts) {
            this.listBox.appendChild(this.createItem(post));
        }
    }

    createItem(post) {
        const item = Template.createItem("MineItemTemplate");
        const link = item.querySelector(".minelink");
        link.textContent = post.title;
        link.href = "/Post/Detail/" + post.id;

        const badge = item.querySelector(".statusbadge");
        badge.textContent = Format.formatStatus(post.status);
        badge.dataset.status = post.status;

        item.querySelector(".minedate").textContent = "Sửa lần cuối: " + Format.formatDate(post.updatedAt ?? post.createdAt);
        item.querySelector(".minerating").textContent = Format.formatRating(post.ratingAverage, post.ratingCount);
        item.querySelector(".mineviews").textContent = post.viewCount + " lượt xem";
        PostCard.addLabels(item.querySelector(".postlabels"), post.labels);

        item.querySelector(".mineview").href = "/Post/Detail/" + post.id;
        item.querySelector(".mineedit").href = "/Post/Edit/" + post.id;
        this.setupToggleButton(item, post);
        item.querySelector(".minedelete").addEventListener("click", () => this.deletePost(post));
        return item;
    }

    // Bài công khai thì có nút "Chuyển về nháp", bài nháp thì có nút "Đăng bài", bài bị ẩn thì không có nút
    setupToggleButton(item, post) {
        const button = item.querySelector(".minetoggle");

        if (post.status === "Public") {
            button.textContent = "Chuyển về nháp";
            button.addEventListener("click", () => this.changeStatus(post, "Draft"));
        } else if (post.status === "Draft") {
            button.textContent = "Đăng bài";
            button.addEventListener("click", () => this.changeStatus(post, "Public"));
        } else {
            button.classList.add("hidden");
        }
    }

    async changeStatus(post, status) {
        const result = await Api.postData("/api/post/" + post.id + "/status", { status: status });
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.loadPosts();
    }

    async deletePost(post) {
        if (!confirm("Bạn chắc chắn muốn xóa bài \"" + post.title + "\"?")) {
            return;
        }

        const result = await Api.deleteData("/api/post/" + post.id);
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.loadPosts();
    }

    // Đường dẫn của một trang trong danh sách, giữ nguyên loại bài đang xem (kind)
    makePageUrl(page) {
        const params = new URLSearchParams(this.params);
        params.set("page", page);
        return "/Post/Mine?" + params.toString();
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
    }
}

new PostMinePage().start();
