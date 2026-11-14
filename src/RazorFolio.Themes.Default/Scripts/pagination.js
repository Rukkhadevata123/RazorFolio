(() => {
    const feed = document.querySelector('[data-home-feed]');
    if (!feed || !feed.querySelector('.pagination') || !['http:', 'https:'].includes(location.protocol)) return;

    const content = feed.querySelector('[data-page-content]');
    const status = feed.querySelector('[data-pagination-status]');
    const articles = document.querySelector('#rf-articles');
    let renderedUrl = new URL(location.href);
    let requestId = 0;
    let controller;
    let animation;
    let scrollTimer;

    const pageKey = url => url.origin + url.pathname + url.search;

    function saveReadingPosition() {
        if (pageKey(new URL(location.href)) !== pageKey(renderedUrl)
            || content.getAttribute('aria-busy') === 'true') return;
        history.replaceState({
            ...history.state,
            rfPaginationScroll: scrollY,
            rfPaginationPage: Number(content.dataset.page)
        }, '');
    }
    addEventListener('scroll', () => {
        clearTimeout(scrollTimer);
        scrollTimer = setTimeout(saveReadingPosition, 150);
    }, { passive: true });

    function resolveLinks(root, baseUrl, preserveFragments = false) {
        for (const element of root.querySelectorAll('a[href], link[href], img[src]')) {
            const attribute = element.tagName === 'IMG' ? 'src' : 'href';
            const value = element.getAttribute(attribute);
            if (preserveFragments && value.startsWith('#')) continue;
            element.setAttribute(attribute, new URL(value, baseUrl).href);
        }
    }

    // Retained navigation and images must keep their meaning when history moves into page/.
    resolveLinks(document, document.baseURI, true);

    function updateCanonical(url) {
        const existing = document.querySelector('link[rel="canonical"]');
        if (!url) {
            existing?.remove();
            return;
        }
        const canonical = existing || document.createElement('link');
        canonical.rel = 'canonical';
        canonical.href = url;
        if (!existing) document.head.append(canonical);
    }

    function showError(url) {
        status.dataset.error = 'true';
        status.textContent = '文章暂时加载失败。';
        const link = document.createElement('a');
        link.href = url.href;
        link.textContent = '直接打开此页';
        status.append(' ', link);
    }

    async function showPage(url, { fromHistory = false, savedScroll = null, expectedPage = null } = {}) {
        const currentRequest = ++requestId;
        controller?.abort();
        controller = new AbortController();
        const activeController = controller;
        const timeout = setTimeout(() => activeController.abort(), 8000);
        content.setAttribute('aria-busy', 'true');
        delete status.dataset.error;
        status.textContent = '正在加载文章…';

        try {
            const response = await fetch(url.href, { signal: activeController.signal });
            if (!response.ok || new URL(response.url).origin !== location.origin) {
                throw new Error('Page unavailable');
            }
            const nextDocument = new DOMParser().parseFromString(await response.text(), 'text/html');
            const nextContent = nextDocument.querySelector('[data-home-feed] [data-page-content]');
            if (!nextContent || !/^[1-9]\d*$/.test(nextContent.dataset.page)) {
                throw new Error('Not an article list page');
            }
            if (expectedPage !== null && nextContent.dataset.page !== String(expectedPage)) {
                throw new Error('Unexpected article list page');
            }
            resolveLinks(nextContent, response.url);
            if (currentRequest !== requestId) return;

            if (!fromHistory) {
                history.replaceState({
                    ...history.state,
                    rfPaginationScroll: scrollY,
                    rfPaginationPage: Number(content.dataset.page)
                }, '');
                history.pushState({ rfPaginationScroll: null, rfPaginationPage: Number(nextContent.dataset.page) }, '', url.href);
            }
            content.replaceChildren(...nextContent.childNodes);
            content.dataset.page = nextContent.dataset.page;
            animation?.cancel();
            if (matchMedia('(prefers-reduced-motion: no-preference)').matches && content.animate) {
                animation = content.animate([{ opacity: .5 }, { opacity: 1 }], { duration: 160 });
            }
            document.title = nextDocument.title;
            const canonical = nextDocument.querySelector('link[rel="canonical"]')?.getAttribute('href');
            updateCanonical(canonical ? new URL(canonical, response.url).href : null);
            renderedUrl = url;
            status.textContent = `已显示第 ${content.dataset.page} 页。`;
            if (!fromHistory) feed.focus({ preventScroll: true });

            if (fromHistory && Number.isFinite(savedScroll)) {
                scrollTo({ top: savedScroll, behavior: 'instant' });
            } else {
                articles.scrollIntoView({ behavior: fromHistory ? 'instant' : 'auto', block: 'start' });
            }
        } catch {
            if (currentRequest !== requestId) return;
            if (fromHistory || pageKey(new URL(location.href)) !== pageKey(renderedUrl)) {
                // A history traversal has already changed the address; load its full document.
                location.reload();
            } else {
                showError(url);
            }
        } finally {
            clearTimeout(timeout);
            if (currentRequest === requestId) content.setAttribute('aria-busy', 'false');
        }
    }

    feed.addEventListener('click', event => {
        const link = event.target.closest('a');
        if (!link?.closest('.pagination') || event.defaultPrevented || event.button !== 0
            || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey
            || link.hasAttribute('download') || (link.target && link.target !== '_self')) return;

        const url = new URL(link.href);
        if (url.origin !== location.origin || !/^[1-9]\d*$/.test(link.dataset.page)) return;
        event.preventDefault();
        showPage(url, { expectedPage: Number(link.dataset.page) });
    });

    function cancelPendingPage() {
        clearTimeout(scrollTimer);
        ++requestId;
        controller?.abort();
        content.setAttribute('aria-busy', 'false');
        status.textContent = '';
        delete status.dataset.error;
    }

    addEventListener('pagehide', () => {
        saveReadingPosition();
        cancelPendingPage();
    });
    addEventListener('popstate', event => {
        cancelPendingPage();
        const url = new URL(location.href);
        if (pageKey(url) !== pageKey(renderedUrl)) {
            showPage(url, {
                fromHistory: true,
                savedScroll: event.state?.rfPaginationScroll,
                expectedPage: event.state?.rfPaginationPage
            });
        }
    });
})();
