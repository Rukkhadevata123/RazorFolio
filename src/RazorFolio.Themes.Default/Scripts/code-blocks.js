(() => {
    function getCodeBlock(code) {
        const pre = code.parentElement;
        if (pre.parentElement.matches(".code-block")) return pre.parentElement;

        // Preserve enhancement for author-provided HTML code blocks as well as Markdown.
        const block = document.createElement("div");
        block.className = "code-block";
        block.dataset.language = [...code.classList].find(name => name.startsWith("language-"))?.slice(9) || "";
        const toolbar = document.createElement("div");
        toolbar.className = "code-toolbar";
        const label = document.createElement("span");
        label.className = "code-language";
        label.textContent = block.dataset.language || "纯文本";
        toolbar.append(label);
        pre.before(block);
        block.append(toolbar, pre);
        return block;
    }

    function addCopyButton(toolbar, text) {
        if (!navigator.clipboard?.writeText) return;

        const controls = document.createElement("span");
        controls.className = "code-copy-controls";
        const status = document.createElement("span");
        status.setAttribute("role", "status");
        const button = document.createElement("button");
        button.type = "button";
        button.textContent = "复制代码";
        controls.append(status, button);
        toolbar.append(controls);

        let resetTimer;
        button.addEventListener("click", async () => {
            if (button.getAttribute("aria-disabled") === "true") return;

            clearTimeout(resetTimer);
            status.textContent = "";
            button.setAttribute("aria-disabled", "true");
            try {
                await navigator.clipboard.writeText(text);
                status.textContent = "已复制";
                resetTimer = setTimeout(() => { status.textContent = ""; }, 2000);
            } catch {
                status.textContent = "复制失败，请手动选择代码复制。";
            } finally {
                button.removeAttribute("aria-disabled");
            }
        });
    }

    for (const code of document.querySelectorAll("article pre > code")) {
        const block = getCodeBlock(code);
        const toolbar = block.querySelector(":scope > .code-toolbar");
        if (!code || !toolbar || block.dataset.enhanced) continue;
        block.dataset.enhanced = "true";

        // Capture the original text before adding any syntax markup or controls.
        const text = code.textContent;
        addCopyButton(toolbar, text);

        const language = (block.dataset.language || "").toLowerCase();
        if (!language || !window.hljs?.getLanguage(language)) continue;

        try {
            const result = window.hljs.highlight(text, { language, ignoreIllegals: true });
            code.innerHTML = result.value;
            code.classList.add("hljs");
        } catch {
            // A failed grammar must leave the original code readable and copyable.
            code.textContent = text;
        }
    }
})();
