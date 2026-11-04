// ===== Thẻ "Phản hồi" trong khu vực nhà cung cấp (Views/Provider/Manage.cshtml) =====
// Hiện các bình luận và đánh giá người khác để lại trên bài đăng và hồ sơ của nhà cung cấp

class ProviderFeedbackTab {
    constructor(profileId) {
        this.profileId = profileId;
        this.isStarted = false;
        this.page = 1;
    }

    // Được gọi mỗi khi mở thẻ, chỉ tải dữ liệu lần đầu
    activate() {
        if (this.isStarted) {
            return;
        }
        this.isStarted = true;

        this.filterSelect = document.getElementById("FeedbackFilter");
        this.listBox = document.getElementById("FeedbackList");
        this.emptyNote = document.getElementById("FeedbackEmpty");
        this.pagerBox = document.getElementById("FeedbackPager");
        this.message = document.getElementById("FeedbackMessage");

        this.filterSelect.addEventListener("change", () => this.loadFeedback(1));
        this.loadFeedback(1);
    }

    async loadFeedback(page) {
        const url = "/api/provider/feedback?page=" + page + "&filter=" + encodeURIComponent(this.filterSelect.value);
        const result = await Api.getData(url);
        if (!result.ok) {
            this.message.textContent = result.message;
            this.message.classList.remove("hidden");
            return;
        }
        this.message.classList.add("hidden");

        this.page = result.data.page;
        this.showItems(result.data.items);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, () => "#feedback", (newPage) => this.loadFeedback(newPage));
    }

    showItems(items) {
        this.listBox.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", items.length > 0);

        for (const data of items) {
            this.listBox.appendChild(this.createItem(data));
        }
    }

    createItem(data) {
        const item = Template.createItem("FeedbackItemTemplate");

        const author = item.querySelector(".commentauthor");
        author.textContent = data.authorName;
        author.href = "/Profile/Detail/" + data.authorId;

        item.querySelector(".commentdate").textContent = Format.formatDate(data.createdAt);
        item.querySelector(".commenttext").textContent = data.content;

        if (data.rating !== null) {
            const badge = item.querySelector(".commentrating");
            badge.textContent = "★ " + data.rating + "/10";
            badge.classList.remove("hidden");
        }

        const target = item.querySelector(".feedbacktarget");
        if (data.kind === "Post") {
            target.textContent = "bài đăng \"" + data.postTitle + "\"";
            target.href = "/Post/Detail/" + data.postId;
        } else {
            target.textContent = "hồ sơ của tôi";
            target.href = "/Profile/Detail/" + this.profileId;
        }
        return item;
    }
}
