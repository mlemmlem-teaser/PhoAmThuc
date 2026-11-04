// ===== Trang đăng ký (Views/Account/Register.cshtml) =====

class RegisterPage {
    start() {
        this.form = document.getElementById("RegisterForm");
        if (this.form === null) {
            return; // chức năng đăng ký đang tắt nên trang không có biểu mẫu
        }

        this.message = document.getElementById("RegisterMessage");
        this.button = document.getElementById("RegisterButton");

        this.form.addEventListener("submit", (event) => this.submitForm(event));
    }

    async submitForm(event) {
        event.preventDefault();
        this.hideMessage();

        const password = document.getElementById("PasswordInput").value;
        const confirm = document.getElementById("ConfirmInput").value;
        if (password !== confirm) {
            this.showMessage("Hai mật khẩu nhập vào không giống nhau.");
            return;
        }

        this.button.disabled = true;
        const data = {
            username: document.getElementById("UsernameInput").value,
            displayName: document.getElementById("DisplayNameInput").value,
            email: document.getElementById("EmailInput").value,
            password: password
        };
        const result = await Api.postData("/api/account/register", data);

        if (!result.ok) {
            this.showMessage(result.message);
            this.button.disabled = false;
            return;
        }
        window.location.href = "/";
    }

    showMessage(text) {
        this.message.textContent = text;
        this.message.classList.remove("hidden");
    }

    hideMessage() {
        this.message.classList.add("hidden");
    }
}

new RegisterPage().start();
