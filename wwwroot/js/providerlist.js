// ===== Trang danh sách nhà cung cấp (Views/Provider/Index.cshtml) =====

class ProviderListPage {
    start() {
        this.listBox = document.getElementById("ProviderList");
        this.emptyNote = document.getElementById("EmptyNote");
        this.pagerBox = document.getElementById("Pager");
        this.message = document.getElementById("ProviderMessage");

        this.params = new URLSearchParams(window.location.search);
        document.getElementById("ProviderSearch").value = this.params.get("q") ?? "";

        this.loadProviders();
    }

    async loadProviders() {
        const result = await Api.getData("/api/provider/list?" + this.params.toString());
        if (!result.ok) {
            this.showMessage(result.message);
            return;
        }

        this.showProviders(result.data.items);
        Pager.showPages(this.pagerBox, result.data.page, result.data.totalPages, (page) => this.makePageUrl(page));
    }

    showProviders(providers) {
        this.listBox.innerHTML = "";
        this.emptyNote.classList.toggle("hidden", providers.length > 0);

        for (const provider of providers) {
            this.listBox.appendChild(this.createProviderCard(provider));
        }
    }

    createProviderCard(provider) {
        const card = Template.createItem("ProviderCardTemplate");
        const url = "/Profile/Detail/" + provider.accountId;

        const link = card.querySelector(".providerlink");
        link.textContent = provider.businessName;
        link.href = url;
        card.querySelector(".providerbutton").href = url;
        card.querySelector(".providerdesc").textContent = provider.description;
        card.querySelector(".providerrating").textContent = Format.formatRating(provider.ratingAverage, provider.ratingCount);

        const address = card.querySelector(".provideraddress");
        if (provider.address) {
            address.textContent = "Địa chỉ: " + provider.address;
        } else {
            address.classList.add("hidden");
        }

        Avatar.showAvatar(card.querySelector(".avatar"), card.querySelector(".avatarholder"), provider.logo, provider.businessName);
        return card;
    }

    makePageUrl(page) {
        const params = new URLSearchParams(this.params);
        params.set("page", page);
        return "/Provider?" + params.toString();
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
    }
}

new ProviderListPage().start();
