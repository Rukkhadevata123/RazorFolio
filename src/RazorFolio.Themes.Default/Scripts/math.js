(() => {
    // Only process math nodes emitted by Markdig, never scan prose or code for dollar signs.
    const formulas = [...document.querySelectorAll('article .math')].flatMap(element => {
        const text = element.textContent.trim();
        const display = element.tagName === 'DIV';
        const opening = display ? '\\[' : '\\(';
        const closing = display ? '\\]' : '\\)';
        if (!text.startsWith(opening) || !text.endsWith(closing)) return [];
        return [{ element, source: text.slice(2, -2).trim(), display }];
    });
    if (!formulas.length) return;

    const assetUrl = new URL('katex/', document.currentScript.src);

    function loadResource(element) {
        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => finish(false), 10000);
            function finish(loaded) {
                clearTimeout(timer);
                element.onload = element.onerror = null;
                if (loaded) resolve();
                else {
                    element.remove();
                    reject(new Error('KaTeX resource failed to load'));
                }
            }
            element.onload = () => finish(true);
            element.onerror = () => finish(false);
            // Keep theme/custom styles later in the cascade, even though KaTeX loads lazily.
            if (element.tagName === 'LINK') document.head.prepend(element);
            else document.head.append(element);
        });
    }

    async function initialize() {
        const stylesheet = document.createElement('link');
        stylesheet.rel = 'stylesheet';
        stylesheet.href = new URL('katex.min.css', assetUrl).href;
        const script = document.createElement('script');
        script.src = new URL('katex.min.js', assetUrl).href;
        try {
            await Promise.all([loadResource(stylesheet), loadResource(script)]);
            if (typeof window.katex?.render !== 'function') throw new Error('KaTeX runtime unavailable');
        } catch {
            const notice = document.createElement('span');
            notice.className = 'math-message';
            notice.setAttribute('role', 'status');
            notice.textContent = '公式组件未能加载，已保留源码。';
            formulas[0].element.before(notice);
            return;
        }

        for (const { element, source, display } of formulas) {
            try {
                // Render off-DOM so a parse failure cannot destroy the original TeX.
                const result = document.createElement('span');
                window.katex.render(source, result, {
                    displayMode: display,
                    output: 'htmlAndMathml',
                    throwOnError: true,
                    trust: false,
                    maxExpand: 1000,
                    maxSize: 20,
                    macros: {}
                });
                element.replaceChildren(...result.childNodes);
                element.dataset.rendered = 'true';
                if (display) {
                    element.tabIndex = 0;
                    element.setAttribute('role', 'region');
                    element.setAttribute('aria-label', '数学公式，可横向滚动');
                }
            } catch (error) {
                const notice = document.createElement('span');
                notice.className = 'math-message';
                notice.textContent = '（公式语法有误或暂不支持，已保留源码）';
                element.append(notice);
                element.dataset.rendered = 'false';
                console.warn('RazorFolio: math formula could not be rendered.', error);
            }
        }
    }

    initialize();
})();
