// ===== Thẻ "Bài đăng" của trang quản trị (Views/Admin/_Posts.cshtml) =====

class AdminPostsTab {
    constructor() {
        this.isStarted = false;
        this.page = 1;
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.message = document.getElementById("PostMessage");
        this.searchInput = document.getElementById("PostSearch");
        this.statusFilter = document.getElementById("PostStatusFilter");
        this.rows = document.getElementById("PostRows");
        this.emptyNote = document.getElementById("PostEmpty");
        this.pagerBox = document.getElementById("PostPager");

        document.getElementById("PostFilterButton").addEventListener("click", () => this.loadPosts(1));
        this.searchInput.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                this.loadPosts(1);
            }
        });
        this.loadPosts(1);
    }

    async loadPosts(page) {
        const query = "page=" + page
            + "&q=" + encodeURIComponent(this.searchInput.value.trim())
            + "&status=" + encodeURIComponent(this.statusFilter.value);
        const result = await Api.getData("/api/admin/posts?" + query);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.page = result.data.page;
        this.showRows(result.data.items);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, () => "#posts", (newPage) => this.loadPosts(newPage));
    }

    showRows(posts) {
        this.rows.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", posts.length > 0);
        for (const post of posts) {
            this.rows.appendChild(this.createRow(post));
        }
    }

    createRow(post) {
        const row = Template.createItem("AdminPostRowTemplate");
        row.querySelector(".pid").textContent = post.id;
        row.querySelector(".pviews").textContent = post.viewCount;
        row.querySelector(".pdate").textContent = Format.formatDate(post.createdAt);

        const title = row.querySelector(".ptitle");
        title.textContent = post.title;
        const author = row.querySelector(".pauthor");
        author.textContent = post.authorName;
        author.href = "/Profile/Detail/" + post.authorId;

        // Bài đã xóa thì không xem và không sửa nội dung được
        const editLink = row.querySelector(".pedit");
        if (post.status === "Deleted") {
            editLink.remove();
        } else {
            title.href = "/Post/Detail/" + post.id;
            editLink.href = "/Post/Edit/" + post.id;
        }

        const statusSelect = row.querySelector(".pstatus");
        statusSelect.value = post.status;
        statusSelect.addEventListener("change", () => this.changeStatus(post, statusSelect.value));
        return row;
    }

    async changeStatus(post, status) {
        if (status === "Deleted" && !confirm("Chuyển bài \"" + post.title + "\" sang trạng thái đã xóa?")) {
            this.loadPosts(this.page);
            return;
        }

        const result = await Api.putData("/api/admin/posts/" + post.id + "/status", { status: status });
        Notice.show(this.message, result.message, result.ok);
        this.loadPosts(this.page);
    }
}
