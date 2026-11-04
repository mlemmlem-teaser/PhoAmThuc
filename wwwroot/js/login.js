// ===== Trang đăng nhập (Views/Account/Login.cshtml) =====

class LoginPage {
    start() {
        this.form = document.getElementById("LoginForm");
        this.message = document.getElementById("LoginMessage");
        this.button = document.getElementById("LoginButton");

        this.form.addEventListener("submit", (event) => this.submitForm(event));
    }

    async submitForm(event) {
        event.preventDefault();
        this.hideMessage();
        this.button.disabled = true;

        const data = {
            username: document.getElementById("UsernameInput").value,
            password: document.getElementById("PasswordInput").value
        };
        const result = await Api.postData("/api/account/login", data);

        if (!result.ok) {
            this.showMessage(result.message);
            this.button.disabled = false;
            return;
        }
        window.location.href = this.getReturnUrl();
    }

    // Quay lại trang trước khi bị yêu cầu đăng nhập. Chỉ nhận đường dẫn nội bộ cho an toàn
    getReturnUrl() {
        const params = new URLSearchParams(window.location.search);
        const url = params.get("returnUrl");
        if (url !== null && url.startsWith("/") && !url.startsWith("//")) {
            return url;
        }
        return "/";
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
    }

    hideMessage() {
        this.message.classList.add("hidden");
    }
}

new LoginPage().start();
