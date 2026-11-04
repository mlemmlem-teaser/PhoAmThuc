// ===== Thẻ "Bình luận" của trang quản trị (Views/Admin/_Comments.cshtml) =====

class AdminCommentsTab {
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

        this.message = document.getElementById("CmMessage");
        this.searchInput = document.getElementById("CmSearch");
        this.typeFilter = document.getElementById("CmTypeFilter");
        this.statusFilter = document.getElementById("CmStatusFilter");
        this.rows = document.getElementById("CmRows");
        this.emptyNote = document.getElementById("CmEmpty");
        this.pagerBox = document.getElementById("CmPager");

        document.getElementById("CmFilterButton").addEventListener("click", () => this.loadComments(1));
        this.searchInput.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                this.loadComments(1);
            }
        });
        this.loadComments(1);
    }

    async loadComments(page) {
        const query = "page=" + page
            + "&q=" + encodeURIComponent(this.searchInput.value.trim())
            + "&type=" + encodeURIComponent(this.typeFilter.value)
            + "&status=" + encodeURIComponent(this.statusFilter.value);
        const result = await Api.getData("/api/admin/comments?" + query);
        if (!result.ok) {
            Notice.show(this.message, result.message, false);
            return;
        }

        this.page = result.data.page;
        this.showRows(result.data.items);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, () => "#comments", (newPage) => this.loadComments(newPage));
    }

    showRows(comments) {
        this.rows.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", comments.length > 0);
        for (const comment of comments) {
            this.rows.appendChild(this.createRow(comment));
        }
    }

    createRow(comment) {
        const row = Template.createItem("AdminCommentRowTemplate");
        row.querySelector(".cid").textContent = comment.id;
        row.querySelector(".cdate").textContent = Format.formatDate(comment.createdAt);
        row.querySelector(".ctype").textContent = this.describeType(comment);

        const content = row.querySelector(".ccontent");
        content.textContent = this.shortenText(comment.content, 120);
        content.title = comment.content;

        const author = row.querySelector(".cauthor");
        author.textContent = comment.authorName;
        author.href = "/Profile/Detail/" + comment.authorId;

        const target = row.querySelector(".ctarget");
        if (comment.targetKind === "Post") {
            target.textContent = "Bài: " + comment.postTitle;
            target.href = "/Post/Detail/" + comment.postId;
        } else {
            target.textContent = "Hồ sơ: " + comment.targetName;
            target.href = "/Profile/Detail/" + comment.targetAccountId;
        }

        const statusSelect = row.querySelector(".cstatus");
        statusSelect.value = comment.status;
        statusSelect.addEventListener("change", () => this.changeStatus(comment, statusSelect.value));
        return row;
    }

    describeType(comment) {
        if (comment.rating !== null) {
            return "Đánh giá ★ " + comment.rating + "/10";
        }
        return comment.isReply ? "Trả lời" : "Bình luận";
    }

    async changeStatus(comment, status) {
        if (status === "Deleted" && !confirm("Chuyển bình luận này sang trạng thái đã xóa?")) {
            this.loadComments(this.page);
            return;
        }

        const result = await Api.putData("/api/admin/comments/" + comment.id + "/status", { status: status });
        Notice.show(this.message, result.message, result.ok);
        this.loadComments(this.page);
    }

    shortenText(text, maxLength) {
        if (text.length <= maxLength) {
            return text;
        }
        return text.substring(0, maxLength - 1) + "…";
    }
}
