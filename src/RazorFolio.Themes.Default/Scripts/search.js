(() => {
    const dialog = document.querySelector('#rf-search');
    const opener = document.querySelector('[data-search-open]');
    if (!dialog || !opener || typeof dialog.showModal !== 'function') return;

    const form = dialog.querySelector('form');
    const input = dialog.querySelector('input');
    const status = dialog.querySelector('[data-search-status]');
    const retry = dialog.querySelector('[data-search-retry]');
    const results = dialog.querySelector('.search-results');
    const indexUrl = new URL(dialog.dataset.indexUrl, document.baseURI);
    const siteRoot = new URL('.', indexUrl);
    const resultLimit = 20;
    let indexPromise;
    let timer;
    let requestId = 0;
    let composing = false;

    const normalize = value => value.normalize('NFKC').toLowerCase();

    function prepareDocument(document, order) {
        if (!document || typeof document.title !== 'string' || typeof document.text !== 'string'
            || typeof document.url !== 'string' || !Array.isArray(document.tags)
            || !document.tags.every(tag => typeof tag === 'string')) {
            throw new Error('Invalid search document');
        }

        const url = new URL(document.url, indexUrl);
        if (!['http:', 'https:'].includes(url.protocol) || url.origin !== siteRoot.origin
            || !url.pathname.startsWith(siteRoot.pathname) || url.username || url.password
            || !url.pathname.toLowerCase().endsWith('.html')) {
            throw new Error('Invalid search URL');
        }

        return {
            ...document, url: url.href, order,
            searchTitle: normalize(document.title),
            searchTags: document.tags.map(normalize),
            searchText: normalize(document.text)
        };
    }

    function loadIndex() {
        if (!indexPromise) {
            indexPromise = (async () => {
                const response = await fetch(indexUrl, { signal: AbortSignal.timeout(8000) });
                if (!response.ok) throw new Error('Search index unavailable');
                const index = await response.json();
                if (index.version !== 1 || !Array.isArray(index.documents)) {
                    throw new Error('Unsupported search index');
                }
                return index.documents.map(prepareDocument);
            })().catch(error => {
                indexPromise = null;
                throw error;
            });
        }
        return indexPromise;
    }

    function findMatches(documents, terms) {
        const matches = [];
        for (const document of documents) {
            let score = 0;
            let matched = true;
            for (const term of terms) {
                if (document.searchTitle.includes(term)) score += 8;
                else if (document.searchTags.some(tag => tag.includes(term))) score += 4;
                else if (document.searchText.includes(term)) score += 1;
                else {
                    matched = false;
                    break;
                }
            }
            if (matched) matches.push({ document, score });
        }
        return matches.sort((left, right) => right.score - left.score || left.document.order - right.document.order);
    }

    function excerpt(text, terms) {
        if (!text) return '此页面暂无正文。';
        const lowerText = text.toLowerCase();
        const positions = terms.map(term => lowerText.indexOf(term)).filter(position => position >= 0);
        const match = positions.length ? Math.min(...positions) : 0;
        const characters = Array.from(text);
        const start = Math.max(0, Array.from(text.slice(0, match)).length - 35);
        const end = start + 160;
        return (start ? '…' : '') + characters.slice(start, end).join('') + (end < characters.length ? '…' : '');
    }

    function showResults(matches, terms) {
        const fragment = document.createDocumentFragment();
        for (const { document: match } of matches.slice(0, resultLimit)) {
            const item = document.createElement('li');
            const heading = document.createElement('h3');
            const link = document.createElement('a');
            link.href = match.url;
            link.textContent = match.title || '未命名页面';
            heading.append(link);
            item.append(heading);
            if (match.tags.length) {
                const tags = document.createElement('p');
                tags.className = 'search-result-tags';
                tags.textContent = match.tags.join(' · ');
                item.append(tags);
            }
            const summary = document.createElement('p');
            summary.className = 'search-result-excerpt';
            summary.textContent = excerpt(match.text, terms);
            item.append(summary);
            fragment.append(item);
        }
        results.replaceChildren(fragment);
        if (matches.length === 0) {
            status.textContent = '没有找到匹配的文章或页面，请换个关键词。';
        } else if (matches.length > resultLimit) {
            status.textContent = `找到 ${matches.length} 项结果，显示前 ${resultLimit} 项，请缩小关键词范围。`;
        } else {
            status.textContent = `找到 ${matches.length} 项结果。`;
        }
    }

    async function search(query, currentRequest) {
        try {
            const documents = await loadIndex();
            if (currentRequest !== requestId || !dialog.open) return;
            const terms = [...new Set(query.split(/\s+/u))];
            showResults(findMatches(documents, terms), terms);
        } catch {
            if (currentRequest !== requestId || !dialog.open) return;
            status.textContent = '搜索暂时无法加载，请稍后重试。';
            retry.hidden = false;
        } finally {
            if (currentRequest === requestId) results.setAttribute('aria-busy', 'false');
        }
    }

    function queueSearch(delay = 120) {
        const currentRequest = ++requestId;
        clearTimeout(timer);
        retry.hidden = true;
        results.replaceChildren();
        const query = normalize(input.value).trim();
        results.setAttribute('aria-busy', query ? 'true' : 'false');
        if (!query) {
            status.textContent = '输入关键词开始搜索。';
            return;
        }

        status.textContent = '正在搜索…';
        timer = setTimeout(() => search(query, currentRequest), delay);
    }

    opener.hidden = false;
    opener.addEventListener('click', () => {
        dialog.showModal();
        input.focus();
        queueSearch();
    });
    dialog.querySelector('[data-search-close]').addEventListener('click', () => dialog.close());
    let startedOutside = false;
    const outside = event => {
        const bounds = dialog.getBoundingClientRect();
        return event.clientX < bounds.left || event.clientX > bounds.right
            || event.clientY < bounds.top || event.clientY > bounds.bottom;
    };
    dialog.addEventListener('pointerdown', event => { startedOutside = outside(event); });
    dialog.addEventListener('click', event => {
        if (startedOutside && outside(event)) dialog.close();
    });
    dialog.addEventListener('keydown', event => {
        // Search inputs otherwise consume Escape to clear their value before closing the dialog.
        if (event.key === 'Escape' && !composing && !event.isComposing) {
            event.preventDefault();
            dialog.close();
        } else if (event.key === 'Tab') {
            const controls = [...dialog.querySelectorAll('button, input, a[href]')]
                .filter(control => !control.hidden && !control.disabled);
            const first = controls[0];
            const last = controls[controls.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        }
    });
    dialog.addEventListener('cancel', event => {
        if (composing) event.preventDefault();
    });
    dialog.addEventListener('close', () => {
        ++requestId;
        clearTimeout(timer);
        composing = false;
        results.setAttribute('aria-busy', 'false');
        opener.focus({ preventScroll: true });
    });
    input.addEventListener('compositionstart', () => {
        composing = true;
        ++requestId;
        clearTimeout(timer);
    });
    input.addEventListener('compositionend', () => {
        composing = false;
        queueSearch();
    });
    input.addEventListener('input', event => {
        if (!composing && !event.isComposing) queueSearch();
    });
    form.addEventListener('submit', event => {
        event.preventDefault();
        if (!composing) queueSearch(0);
    });
    retry.addEventListener('click', () => queueSearch(0));
    input.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown' && !composing && results.firstElementChild) {
            event.preventDefault();
            results.querySelector('a').focus();
        }
    });
    results.addEventListener('keydown', event => {
        if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
        const links = [...results.querySelectorAll('a')];
        const position = links.indexOf(document.activeElement);
        if (position < 0) return;
        event.preventDefault();
        if (event.key === 'ArrowUp' && position === 0) input.focus();
        else links[position + (event.key === 'ArrowDown' ? 1 : -1)]?.focus();
    });
})();
