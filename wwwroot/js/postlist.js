// ===== Trang danh sách bài đăng (Views/Post/Index.cshtml) =====
// Các lựa chọn tìm kiếm, lọc, sắp xếp nằm trên đường dẫn (ví dụ /Post?kind=recipe&sort=rating&page=2).
// Trang đọc đường dẫn, điền lại biểu mẫu, rồi chuyển nguyên các tham số đó cho API.

class PostListPage {
    start() {
        this.form = document.getElementById("FilterForm");
        this.labelFilter = document.getElementById("LabelFilter");
        this.listBox = document.getElementById("PostList");
        this.emptyNote = document.getElementById("EmptyNote");
        this.pagerBox = document.getElementById("Pager");
        this.message = document.getElementById("ListMessage");

        this.params = new URLSearchParams(window.location.search);

        this.fillFilterForm();
        this.loadLabels();
        this.loadPosts();
    }

    // Điền lại các ô lọc theo đường dẫn (trừ ô nhãn, vì danh sách nhãn phải tải về trước)
    fillFilterForm() {
        const names = ["q", "kind", "author", "minRating", "sort"];
        for (const name of names) {
            this.form.elements[name].value = this.params.get(name) ?? "";
        }
    }

    // Ô lọc nhãn chỉ chứa nhãn thường (5 loại bài đã có ở ô "Mọi loại bài")
    async loadLabels() {
        const result = await Api.getData("/api/post/labels");
        if (!result.ok) {
            return;
        }

        for (const label of result.data.labels) {
            if (label.systemKey === null) {
                const option = document.createElement("option");
                option.value = label.id;
                option.textContent = label.name;
                this.labelFilter.appendChild(option);
            }
        }
        this.labelFilter.value = this.params.get("labelId") ?? "";
    }

    async loadPosts() {
        const result = await Api.getData("/api/post/list?" + this.params.toString());
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
            this.listBox.appendChild(PostCard.createCard(post));
        }
    }

    // Đường dẫn của một trang trong danh sách, giữ nguyên mọi bộ lọc đang chọn
    makePageUrl(page) {
        const params = new URLSearchParams(this.params);
        params.set("page", page);
        return "/Post?" + params.toString();
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
    }
}

new PostListPage().start();
