(() => {
    const button = document.querySelector('[data-print-article]');
    const article = document.querySelector('.article-body');
    if (!button || !article || typeof window.print !== 'function') return;
    const status = document.querySelector('[data-print-status]');
    const scaledFormulas = new Map();

    function waitForContent() {
        for (const image of article.querySelectorAll('img')) image.loading = 'eager';
        return new Promise(resolve => {
            const observer = new MutationObserver(check);
            const timer = setTimeout(() => finish(false), 12000);
            function finish(ready) {
                clearTimeout(timer);
                observer.disconnect();
                article.removeEventListener('load', check, true);
                article.removeEventListener('error', check, true);
                document.fonts.removeEventListener('loadingdone', check);
                resolve(ready);
            }
            function check() {
                const rendering = article.querySelector('.math[data-rendered="pending"], .mermaid-diagram[data-rendered="pending"]');
                const imagesReady = [...article.querySelectorAll('img')].every(image => image.complete);
                if (!rendering && imagesReady && document.fonts.status === 'loaded') finish(true);
            }
            observer.observe(article, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-rendered'] });
            article.addEventListener('load', check, true);
            article.addEventListener('error', check, true);
            document.fonts.addEventListener('loadingdone', check);
            check();
        });
    }

    function restoreFormulas() {
        for (const [formula, zoom] of scaledFormulas) formula.style.zoom = zoom;
        scaledFormulas.clear();
    }

    addEventListener('beforeprint', () => {
        restoreFormulas();
        for (const image of article.querySelectorAll('img')) image.loading = 'eager';
        for (const formula of article.querySelectorAll('div.math > .katex-display')) {
            const available = formula.parentElement.clientWidth;
            const width = formula.scrollWidth;
            if (available > 0 && width > available) {
                scaledFormulas.set(formula, formula.style.zoom);
                formula.style.zoom = String(available / width);
            }
        }
    });
    addEventListener('afterprint', restoreFormulas);

    button.hidden = false;
    button.addEventListener('click', async () => {
        button.disabled = true;
        button.textContent = '准备打印…';
        status.hidden = true;
        try {
            if (await waitForContent()) {
                window.print();
            } else {
                status.textContent = '部分图片或图表仍在加载，请稍后再试。';
                status.hidden = false;
            }
        } finally {
            button.disabled = false;
            button.textContent = '打印文章';
        }
    });
})();
