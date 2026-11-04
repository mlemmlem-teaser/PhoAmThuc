// ===== Phần nhập công thức nấu ăn: nguyên liệu và các bước (khu RecipeSection trong Views/Post/Editor.cshtml) =====

class RecipeForm {
    start() {
        this.ingredientRows = document.getElementById("IngredientRows");
        this.stepRows = document.getElementById("StepRows");

        document.getElementById("AddIngredientButton").addEventListener("click", () => this.addIngredientRow(null));
        document.getElementById("AddStepButton").addEventListener("click", () => this.addStepRow(null));

        // Bắt đầu với một dòng trống cho mỗi phần
        this.addIngredientRow(null);
        this.addStepRow(null);
    }

    // ----- Nguyên liệu -----

    addIngredientRow(ingredient) {
        const row = Template.createItem("IngredientRowTemplate");

        if (ingredient !== null) {
            row.querySelector(".rowname").value = ingredient.name;
            row.querySelector(".rowamount").value = ingredient.amount ?? "";
            row.querySelector(".rowunit").value = ingredient.unit ?? "";
            row.querySelector(".rownote").value = ingredient.note ?? "";
        }
        row.querySelector(".rowremove").addEventListener("click", () => row.remove());
        this.ingredientRows.appendChild(row);
    }

    getIngredients() {
        const ingredients = [];
        for (const row of this.ingredientRows.querySelectorAll(".ingredientrow")) {
            const name = row.querySelector(".rowname").value.trim();
            if (name === "") {
                continue; // bỏ qua dòng chưa nhập tên
            }

            const amountText = row.querySelector(".rowamount").value.trim();
            ingredients.push({
                name: name,
                amount: amountText === "" ? null : parseFloat(amountText),
                unit: row.querySelector(".rowunit").value.trim(),
                note: row.querySelector(".rownote").value.trim()
            });
        }
        return ingredients;
    }

    // ----- Các bước thực hiện -----

    addStepRow(step) {
        const row = Template.createItem("StepRowTemplate");
        row.dataset.image = "";

        if (step !== null) {
            row.querySelector(".stepinput").value = step.content;
            this.showStepImage(row, step.image ?? "");
        }

        row.querySelector(".stepfile").addEventListener("change", () => this.uploadStepImage(row));
        row.querySelector(".stepclear").addEventListener("click", () => this.showStepImage(row, ""));
        row.querySelector(".stepup").addEventListener("click", () => this.moveStepUp(row));
        row.querySelector(".stepdown").addEventListener("click", () => this.moveStepDown(row));
        row.querySelector(".rowremove").addEventListener("click", () => this.removeStepRow(row));

        this.stepRows.appendChild(row);
        this.renumberSteps();
    }

    async uploadStepImage(row) {
        const fileInput = row.querySelector(".stepfile");
        const file = fileInput.files[0];
        if (file === undefined) {
            return;
        }

        const result = await Api.uploadFile("/api/upload/image", file);
        fileInput.value = "";
        if (!result.ok) {
            alert(result.message);
            return;
        }
        this.showStepImage(row, result.data.url);
    }

    // Đường dẫn ảnh của bước được nhớ trong row.dataset.image. Truyền chuỗi rỗng để bỏ ảnh
    showStepImage(row, url) {
        row.dataset.image = url;
        const preview = row.querySelector(".steppreview");
        const clearButton = row.querySelector(".stepclear");

        if (url === "") {
            preview.classList.add("hidden");
            clearButton.classList.add("hidden");
        } else {
            preview.src = url;
            preview.classList.remove("hidden");
            clearButton.classList.remove("hidden");
        }
    }

    moveStepUp(row) {
        const previous = row.previousElementSibling;
        if (previous !== null) {
            this.stepRows.insertBefore(row, previous);
            this.renumberSteps();
        }
    }

    moveStepDown(row) {
        const next = row.nextElementSibling;
        if (next !== null) {
            this.stepRows.insertBefore(next, row);
            this.renumberSteps();
        }
    }

    removeStepRow(row) {
        row.remove();
        this.renumberSteps();
    }

    // Đánh lại số thứ tự "Bước 1, Bước 2..." sau mỗi lần thêm, xóa, đổi chỗ
    renumberSteps() {
        const rows = this.stepRows.querySelectorAll(".steprow");
        rows.forEach((row, index) => {
            row.querySelector(".stepnumber").textContent = "Bước " + (index + 1);
        });
    }

    getSteps() {
        const steps = [];
        for (const row of this.stepRows.querySelectorAll(".steprow")) {
            const content = row.querySelector(".stepinput").value.trim();
            if (content === "") {
                continue; // bỏ qua bước chưa nhập nội dung
            }
            steps.push({ content: content, image: row.dataset.image === "" ? null : row.dataset.image });
        }
        return steps;
    }

    // ----- Đọc và ghi toàn bộ công thức -----

    getRecipeData() {
        return {
            prepMinutes: this.readNumber("PrepInput"),
            cookMinutes: this.readNumber("CookInput"),
            servings: this.readNumber("ServingsInput"),
            difficulty: document.getElementById("DifficultySelect").value,
            note: document.getElementById("RecipeNoteInput").value.trim(),
            ingredients: this.getIngredients(),
            steps: this.getSteps()
        };
    }

    // Điền công thức đã có vào biểu mẫu khi sửa bài
    setRecipeData(recipe) {
        document.getElementById("PrepInput").value = recipe.prepMinutes ?? "";
        document.getElementById("CookInput").value = recipe.cookMinutes ?? "";
        document.getElementById("ServingsInput").value = recipe.servings ?? "";
        document.getElementById("DifficultySelect").value = recipe.difficulty ?? "";
        document.getElementById("RecipeNoteInput").value = recipe.note ?? "";

        this.ingredientRows.innerHTML = "";
        this.stepRows.innerHTML = "";
        for (const ingredient of recipe.ingredients) {
            this.addIngredientRow(ingredient);
        }
        for (const step of recipe.steps) {
            this.addStepRow(step);
        }
    }

    // Ô số để trống thì gửi null (không gửi chuỗi rỗng)
    readNumber(inputId) {
        const text = document.getElementById(inputId).value.trim();
        if (text === "") {
            return null;
        }
        return parseInt(text, 10);
    }
}
