// ===== Thanh chuyển trang (1 2 3 ...) dùng chung cho các danh sách =====
// Cần có thẻ <template> PagerLinkTemplate trên trang (xem Views/Shared/_PostTemplates.cshtml)

class Pager {
    // container: khung chứa các nút; currentPage, totalPages: số trang;
    // makeUrl: hàm nhận số trang và trả về đường dẫn của trang đó
    // onClick (không bắt buộc): hàm nhận số trang; nếu có thì bấm nút sẽ gọi hàm này thay vì chuyển trang
    static showPages(container, currentPage, totalPages, makeUrl, onClick) {
        container.innerHTML = "";
        if (totalPages <= 1) {
            return;
        }

        if (currentPage > 1) {
            container.appendChild(Pager.createLink("« Trước", currentPage - 1, false, makeUrl, onClick));
        }

        // Chỉ hiện các trang gần trang hiện tại cho gọn
        const first = Math.max(1, currentPage - 2);
        const last = Math.min(totalPages, currentPage + 2);
        for (let page = first; page <= last; page++) {
            container.appendChild(Pager.createLink(String(page), page, page === currentPage, makeUrl, onClick));
        }

        if (currentPage < totalPages) {
            container.appendChild(Pager.createLink("Sau »", currentPage + 1, false, makeUrl, onClick));
        }
    }

    static createLink(text, page, isCurrent, makeUrl, onClick) {
        const link = Template.createItem("PagerLinkTemplate");
        link.textContent = text;
        link.href = makeUrl(page);
        if (isCurrent) {
            link.classList.add("current");
        }
        if (onClick !== undefined) {
            link.addEventListener("click", (event) => {
                event.preventDefault();
                onClick(page);
            });
        }
        return link;
    }
}
