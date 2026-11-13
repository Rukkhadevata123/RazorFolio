(() => {
    const blocks = [...document.querySelectorAll('article .code-block')]
        .filter(block => block.dataset.language?.trim().toLowerCase() === 'mermaid');
    if (!blocks.length) return;

    const libraryUrl = new URL('mermaid.tiny.js', document.currentScript.src);
    const root = document.documentElement;
    let renderSequence = 0;
    let requestedTheme = currentTheme();
    let rendering = false;

    const diagrams = blocks.map((block, index) => {
        const source = block.querySelector('pre > code').textContent;
        const figure = document.createElement('figure');
        figure.className = 'mermaid-diagram';
        const viewport = document.createElement('div');
        viewport.className = 'mermaid-viewport';
        viewport.hidden = true;
        viewport.tabIndex = 0;
        viewport.setAttribute('role', 'region');
        viewport.setAttribute('aria-label', `图表 ${index + 1}，可横向滚动`);
        const status = document.createElement('p');
        status.className = 'mermaid-status';
        status.setAttribute('role', 'status');
        status.textContent = '正在绘制图表…';
        const details = document.createElement('details');
        details.className = 'mermaid-source';
        details.open = true;
        const summary = document.createElement('summary');
        summary.textContent = '查看图表源码';

        block.before(figure);
        details.append(summary, block);
        figure.append(viewport, status, details);
        return { source, figure, viewport, status, details };
    });

    function currentTheme() {
        return root.dataset.theme || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    }

    function loadLibrary() {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = libraryUrl.href;
            const timer = setTimeout(() => {
                script.onload = script.onerror = null;
                script.remove();
                reject(new Error('Mermaid load timed out'));
            }, 10000);
            script.onload = () => {
                clearTimeout(timer);
                if (typeof window.mermaid?.render === 'function') resolve(window.mermaid);
                else reject(new Error('Mermaid runtime unavailable'));
            };
            script.onerror = () => {
                clearTimeout(timer);
                script.remove();
                reject(new Error('Mermaid load failed'));
            };
            document.head.append(script);
        });
    }

    function configuration(theme) {
        const style = getComputedStyle(root);
        const color = name => style.getPropertyValue(name).trim();
        return {
            startOnLoad: false,
            securityLevel: 'strict',
            suppressErrorRendering: true,
            theme: 'base',
            look: 'classic',
            layout: 'dagre',
            fontFamily: getComputedStyle(document.body).fontFamily,
            flowchart: { useMaxWidth: false },
            sequence: { useMaxWidth: false },
            themeVariables: {
                darkMode: theme === 'dark',
                background: color('--surface'),
                primaryColor: color('--soft'),
                primaryTextColor: color('--text'),
                primaryBorderColor: color('--accent'),
                lineColor: color('--muted'),
                secondaryColor: color('--bg'),
                tertiaryColor: color('--surface')
            }
        };
    }

    function showFailure(diagram, message) {
        diagram.viewport.replaceChildren();
        diagram.viewport.hidden = true;
        diagram.figure.dataset.rendered = 'false';
        diagram.status.textContent = message;
        diagram.status.hidden = false;
        diagram.details.open = true;
    }

    async function initialize() {
        let mermaid;
        try {
            mermaid = await loadLibrary();
            await document.fonts.ready;
        } catch {
            for (const diagram of diagrams) {
                showFailure(diagram, '图表组件未能加载，已保留源码。');
            }
            return;
        }

        async function renderDiagrams() {
            if (rendering) return;
            rendering = true;
            try {
                let theme;
                do {
                    theme = requestedTheme;
                    const options = configuration(theme);
                    mermaid.initialize(options);
                    for (const diagram of diagrams) {
                        if (theme !== requestedTheme) break;
                        // A private host keeps parser failures and temporary SVGs out of the article.
                        const host = document.createElement('div');
                        host.style.cssText = 'position:absolute;left:-100000px;top:0;visibility:hidden;';
                        document.body.append(host);
                        try {
                            const id = 'rf-mermaid-' + (++renderSequence);
                            const { svg } = await mermaid.render(id, diagram.source, host);
                            if (theme !== requestedTheme) break;
                            // Mermaid's strict renderer sanitizes its SVG; the source stays in the code block.
                            diagram.viewport.innerHTML = svg;
                            diagram.viewport.style.backgroundColor = options.themeVariables.background;
                            diagram.viewport.hidden = false;
                            diagram.status.hidden = true;
                            if (diagram.figure.dataset.rendered !== 'true') diagram.details.open = false;
                            diagram.figure.dataset.rendered = 'true';
                        } catch (error) {
                            if (theme !== requestedTheme) break;
                            showFailure(diagram, '图表语法有误或暂不支持，已保留源码。');
                            console.warn('RazorFolio: Mermaid diagram could not be rendered.', error);
                        } finally {
                            host.remove();
                        }
                    }
                } while (theme !== requestedTheme);
            } finally {
                rendering = false;
            }
        }

        const observer = new MutationObserver(() => {
            requestedTheme = currentTheme();
            renderDiagrams();
        });
        observer.observe(root, { attributes: true, attributeFilter: ['data-theme'] });
        requestedTheme = currentTheme();
        await renderDiagrams();
    }

    initialize();
})();
