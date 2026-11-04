// ===== Khu bình luận và đánh giá (Views/Shared/_CommentSection.cshtml) =====
// Dùng cho trang bài đăng:  new CommentSection("post", mãBàiĐăng, phầnTửHiểnThịĐiểm).start()
// Dùng cho trang hồ sơ:     new CommentSection("profile", mãTàiKhoản, phầnTửHiểnThịĐiểm).start()
// Cần có sẵn: thẻ <template> PagerLinkTemplate (_PostTemplates.cshtml) và file pager.js

class CommentSection {
    // ratingElement (không bắt buộc): phần tử bên ngoài cũng hiển thị điểm, sẽ được cập nhật theo
    constructor(targetType, targetId, ratingElement) {
        this.targetType = targetType;
        this.targetId = targetId;
        this.ratingElement = ratingElement ?? null;
        this.page = 1;
        this.permissions = null;
    }

    start() {
        this.findElements();
        this.prepareRatingSelect();
        this.submitButton.addEventListener("click", () => this.submitComment());
        this.loadComments(1);
    }

    findElements() {
        this.summary = document.getElementById("CommentSummary");
        this.message = document.getElementById("CommentMessage");
        this.formBox = document.getElementById("CommentFormBox");
        this.input = document.getElementById("CommentInput");
        this.ratingPick = document.getElementById("RatingPick");
        this.ratingSelect = document.getElementById("RatingSelect");
        this.submitButton = document.getElementById("CommentSubmit");
        this.hint = document.getElementById("CommentHint");
        this.loginHint = document.getElementById("LoginHint");
        this.list = document.getElementById("CommentList");
        this.emptyNote = document.getElementById("CommentEmpty");
        this.pagerBox = document.getElementById("CommentPager");
    }

    // Hồ sơ chỉ nhận đánh giá có điểm nên bỏ lựa chọn "Không đánh giá"
    prepareRatingSelect() {
        if (this.targetType === "profile") {
            this.ratingSelect.querySelector("option[value='']").remove();
            this.ratingSelect.value = "10";
            this.input.placeholder = "Nhận xét ngắn về hồ sơ này...";
            document.getElementById("CommentTitle").textContent = "Đánh giá hồ sơ";
        }
    }

    // ----- Tải và hiển thị -----

    makeQuery() {
        if (this.targetType === "post") {
            return "postId=" + this.targetId;
        }
        return "profileId=" + this.targetId;
    }

    async loadComments(page) {
        const result = await Api.getData("/api/comment/list?" + this.makeQuery() + "&page=" + page);
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }

        const data = result.data;
        this.page = data.page;
        this.permissions = data.permissions;

        this.showSummary(data.ratingAverage, data.ratingCount);
        this.showForm();
        this.showList(data.items);
        Pager.showPages(this.pagerBox, data.page, data.totalPages, () => "#CommentArea", (newPage) => this.loadComments(newPage));
    }

    showSummary(average, count) {
        const text = Format.formatRating(average, count);
        this.summary.textContent = "Điểm đánh giá: " + text;
        if (this.ratingElement !== null) {
            this.ratingElement.textContent = text;
        }
    }

    // Quyết định hiện hay ẩn biểu mẫu, ô chọn điểm và lời nhắc (máy chủ vẫn kiểm tra lại khi gửi)
    showForm() {
        const permissions = this.permissions;

        // Bài đăng: bình luận hoặc đánh giá đều được. Hồ sơ: chỉ đánh giá
        let canUseForm = permissions.canRate;
        if (this.targetType === "post") {
            canUseForm = permissions.canComment || permissions.canRate;
        }

        this.loginHint.classList.toggle("hidden", permissions.isLoggedIn);
        this.formBox.classList.toggle("hidden", !canUseForm);
        this.ratingPick.classList.toggle("hidden", !permissions.canRate);

        const reason = permissions.rateBlockedReason;
        this.hint.classList.toggle("hidden", !reason);
        this.hint.textContent = reason ?? "";
    }

    showList(items) {
        this.list.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", items.length > 0);

        for (const data of items) {
            this.list.appendChild(this.createCommentItem(data, false));
        }
    }

    // Dựng một bình luận (isReply = true nếu là câu trả lời)
    createCommentItem(data, isReply) {
        const item = Template.createItem("CommentTemplate");

        const author = item.querySelector(".commentauthor");
        author.textContent = data.authorName;
        author.href = "/Profile/Detail/" + data.authorId;
        item.querySelector(".commentrole").textContent = "(" + data.authorRole + ")";
        item.querySelector(".commentdate").textContent = Format.formatDate(data.createdAt) + (data.updatedAt ? " (đã sửa)" : "");
        item.querySelector(".commenttext").textContent = data.content;

        if (data.rating !== null) {
            const badge = item.querySelector(".commentrating");
            badge.textContent = "★ " + data.rating + "/10";
            badge.classList.remove("hidden");
        }

        this.setupActions(item, data, isReply);
        this.setupEditBox(item, data);

        if (isReply) {
            item.classList.add("isreply");
            item.querySelector(".replybox").remove();
            item.querySelector(".replylist").remove();
        } else {
            this.setupReplyBox(item, data);
            this.addReplies(item, data.replies);
        }
        return item;
    }

    addReplies(item, replies) {
        const replyList = item.querySelector(".replylist");
        for (const reply of replies) {
            replyList.appendChild(this.createCommentItem(reply, true));
        }
    }

    // Chỉ giữ lại các nút mà người xem được phép dùng
    setupActions(item, data, isReply) {
        const replyButton = item.querySelector(".replybutton");
        const editButton = item.querySelector(".editbutton");
        const deleteButton = item.querySelector(".deletebutton");

        if (isReply || !this.permissions.canReply) {
            replyButton.remove();
        } else {
            replyButton.addEventListener("click", () => item.querySelector(".replybox").classList.toggle("hidden"));
        }

        if (data.canEdit) {
            editButton.addEventListener("click", () => this.openEditBox(item, data));
        } else {
            editButton.remove();
        }

        if (data.canDelete) {
            deleteButton.addEventListener("click", () => this.deleteComment(data));
        } else {
            deleteButton.remove();
        }
    }

    // ----- Gửi bình luận, đánh giá -----

    async submitComment() {
        this.hideMessage();

        const content = this.input.value.trim();
        if (content === "") {
            this.showMessage("Bạn chưa nhập nội dung.");
            return;
        }

        const ratingHidden = this.ratingPick.classList.contains("hidden");
        const ratingText = ratingHidden ? "" : this.ratingSelect.value;
        const body = {
            postId: this.targetType === "post" ? this.targetId : null,
            targetAccountId: this.targetType === "profile" ? this.targetId : null,
            content: content,
            rating: ratingText === "" ? null : parseInt(ratingText)
        };

        this.submitButton.disabled = true;
        const result = await Api.postData("/api/comment", body);
        this.submitButton.disabled = false;

        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.input.value = "";
        this.ratingSelect.value = this.targetType === "profile" ? "10" : "";
        this.loadComments(1);
    }

    // ----- Trả lời -----

    setupReplyBox(item, data) {
        const box = item.querySelector(".replybox");
        box.querySelector(".replysend").addEventListener("click", () => this.sendReply(box, data));
        box.querySelector(".replycancel").addEventListener("click", () => box.classList.add("hidden"));
    }

    async sendReply(box, data) {
        this.hideMessage();
        const content = box.querySelector(".replyinput").value.trim();
        if (content === "") {
            this.showMessage("Bạn chưa nhập nội dung trả lời.");
            return;
        }

        const result = await Api.postData("/api/comment", { parentId: data.id, content: content });
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.loadComments(this.page);
    }

    // ----- Sửa và xóa -----

    setupEditBox(item, data) {
        const box = item.querySelector(".editbox");
        box.querySelector(".editsave").addEventListener("click", () => this.saveEdit(box, data));
        box.querySelector(".editcancel").addEventListener("click", () => box.classList.add("hidden"));
    }

    openEditBox(item, data) {
        const box = item.querySelector(".editbox");
        box.querySelector(".editinput").value = data.content;

        // Bình luận đánh giá thì cho sửa cả điểm
        const ratingPick = box.querySelector(".editratingpick");
        if (data.rating !== null) {
            box.querySelector(".editrating").value = String(data.rating);
            ratingPick.classList.remove("hidden");
        } else {
            ratingPick.classList.add("hidden");
        }
        box.classList.remove("hidden");
    }

    async saveEdit(box, data) {
        this.hideMessage();
        const content = box.querySelector(".editinput").value.trim();
        if (content === "") {
            this.showMessage("Nội dung không được để trống.");
            return;
        }

        const body = {
            content: content,
            rating: data.rating === null ? null : parseInt(box.querySelector(".editrating").value)
        };
        const result = await Api.putData("/api/comment/" + data.id, body);
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.loadComments(this.page);
    }

    async deleteComment(data) {
        const question = data.rating !== null ? "Bạn chắc chắn muốn xóa đánh giá này?" : "Bạn chắc chắn muốn xóa bình luận này?";
        if (!confirm(question)) {
            return;
        }

        const result = await Api.deleteData("/api/comment/" + data.id);
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }
        this.loadComments(this.page);
    }

    // ----- Thông báo -----

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
        this.message.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    hideMessage() {
        this.message.classList.add("hidden");
    }
}
