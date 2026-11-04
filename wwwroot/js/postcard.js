// ===== Dựng thẻ bài đăng và các nhãn (dùng ở danh sách, trang quản lý bài, sau này cả trang chủ và hồ sơ) =====
// Cần có thẻ <template> PostCardTemplate và LabelChipTemplate trên trang (xem Views/Shared/_PostTemplates.cshtml)

class PostCard {
    // Tạo một thẻ bài đăng từ dữ liệu JSON của API
    static createCard(data) {
        const card = Template.createItem("PostCardTemplate");
        const url = "/Post/Detail/" + data.id;

        card.querySelector(".postlink").textContent = data.title;
        card.querySelector(".postlink").href = url;
        card.querySelector(".postbutton").href = url;
        card.querySelector(".postsummary").textContent = data.summary ?? "";
        const author = card.querySelector(".postauthor");
        author.textContent = data.authorName;
        author.href = "/Profile/Detail/" + data.authorId;
        card.querySelector(".postdate").textContent = Format.formatDate(data.publishedAt ?? data.createdAt);
        card.querySelector(".postrating").textContent = Format.formatRating(data.ratingAverage, data.ratingCount);
        card.querySelector(".postviews").textContent = data.viewCount + " lượt xem";

        PostCard.showCover(card, data, url);
        PostCard.showUpdated(card, data);
        PostCard.addLabels(card.querySelector(".postlabels"), data.labels);
        return card;
    }

    // Có ảnh đại diện thì hiện ảnh, không thì hiện khung trống
    static showCover(card, data, url) {
        const imageLink = card.querySelector(".postimagelink");
        const image = card.querySelector(".postimage");
        imageLink.href = url;

        if (data.cover) {
            image.src = data.cover;
            image.alt = data.title;
        } else {
            imageLink.classList.add("noimage");
        }
    }

    // Chỉ hiện "cập nhật lần cuối" khi bài đã từng được sửa
    static showUpdated(card, data) {
        const updated = card.querySelector(".postupdated");
        if (data.updatedAt) {
            updated.textContent = "Cập nhật: " + Format.formatDate(data.updatedAt);
        } else {
            updated.classList.add("hidden");
        }
    }

    // Thêm các nhãn vào một khung chứa
    static addLabels(container, labels) {
        for (const label of labels) {
            const chip = Template.createItem("LabelChipTemplate");
            chip.textContent = label.name;
            if (label.systemKey) {
                chip.dataset.key = label.systemKey;
            }
            container.appendChild(chip);
        }
    }
}
