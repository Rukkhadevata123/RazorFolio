(() => {
    function httpsUrl(value) {
        try {
            const url = new URL(value);
            return url.protocol === 'https:' && !url.username && !url.password ? url.href : null;
        } catch {
            return null;
        }
    }

    function loadWallpaper(url) {
        return new Promise((resolve, reject) => {
            const image = new Image();
            const timer = setTimeout(() => {
                image.onload = image.onerror = null;
                image.src = '';
                reject(new Error('Wallpaper timed out'));
            }, 8000);
            image.onload = () => { clearTimeout(timer); resolve(image); };
            image.onerror = () => { clearTimeout(timer); reject(new Error('Wallpaper unavailable')); };
            image.src = url;
        });
    }

    async function initializeBackground() {
        const hero = document.querySelector('.hero');
        if (!hero) return;

        const fallback = hero.querySelector('.hero-background');
        if (fallback) {
            const markLoaded = () => { if (fallback.naturalWidth) hero.classList.add('has-background'); };
            fallback.addEventListener('load', markLoaded, { once: true });
            fallback.addEventListener('error', () => { fallback.remove(); }, { once: true });
            markLoaded();
        }
        if (hero.dataset.backgroundMode !== 'bing') return;

        try {
            const response = await fetch('https://bing-wallpaper.vuejs.press/api/wallpaper', {
                signal: AbortSignal.timeout(5000), credentials: 'omit', referrerPolicy: 'no-referrer'
            });
            if (!response.ok) return;
            const data = await response.json();
            if (!Array.isArray(data)) return;
            const wallpapers = data.filter(item => item && httpsUrl(item.url));
            if (!wallpapers.length) return;
            const selected = wallpapers[Math.floor(Math.random() * wallpapers.length)];
            const image = await loadWallpaper(httpsUrl(selected.url));
            image.className = 'hero-background';
            image.alt = '';
            fallback?.remove();
            hero.prepend(image);
            hero.classList.add('has-background');

            const credit = hero.querySelector('.hero-credit');
            const language = document.documentElement.lang.toLowerCase().split('-')[0];
            const locale = selected.locales?.[language] || selected.locales?.en;
            const description = [locale?.title, locale?.copyright].filter(value => typeof value === 'string').join(' · ');
            if (description) {
                const source = httpsUrl(locale?.backstage);
                if (source) {
                    const link = document.createElement('a');
                    link.href = source;
                    link.textContent = description;
                    credit.append(link);
                } else {
                    credit.textContent = description;
                }
                credit.hidden = false;
            }
        } catch {
            // Keep the configured image or solid background when the remote provider fails.
        }
    }
    initializeBackground();

    const root = document.documentElement;
    const header = document.querySelector('.site-header');
    new ResizeObserver(() => {
        root.style.setProperty('--header-height', header.offsetHeight + 'px');
    }).observe(header);

    const toggle = document.querySelector('[data-theme-toggle]');
    let preference;
    try { preference = localStorage.getItem('razorfolio-color-mode'); } catch { }
    if (preference !== 'light' && preference !== 'dark') preference = null;
    const systemTheme = matchMedia('(prefers-color-scheme: dark)');
    root.dataset.theme = preference || (systemTheme.matches ? 'dark' : 'light');

    const updateToggle = () => {
        toggle.textContent = root.dataset.theme === 'dark' ? '浅色' : '深色';
        toggle.setAttribute('aria-label', '切换至' + toggle.textContent + '模式');
    };
    updateToggle();
    toggle.hidden = false;
    systemTheme.addEventListener('change', () => {
        if (preference) return;
        root.dataset.theme = systemTheme.matches ? 'dark' : 'light';
        updateToggle();
    });
    toggle.addEventListener('click', () => {
        root.dataset.theme = root.dataset.theme === 'dark' ? 'light' : 'dark';
        preference = root.dataset.theme;
        try { localStorage.setItem('razorfolio-color-mode', preference); } catch { }
        updateToggle();
    });

    const menus = [...document.querySelectorAll('.mobile-navigation, .mobile-sidebar')];
    for (const menu of menus) {
        menu.addEventListener('keydown', event => {
            if (event.key !== 'Escape' || !menu.open) return;
            event.preventDefault();
            menu.open = false;
            menu.querySelector('summary').focus({ preventScroll: true });
        });
        menu.addEventListener('toggle', () => {
            if (!menu.open) return;
            for (const other of document.querySelectorAll('.mobile-navigation, .mobile-sidebar, .mobile-toc')) {
                if (other !== menu) other.open = false;
            }
        });
        menu.addEventListener('click', event => {
            if (event.target.closest('a')) menu.open = false;
        });
    }
    document.addEventListener('click', event => {
        for (const menu of menus) {
            if (menu.open && !menu.contains(event.target)) menu.open = false;
        }
    });

    // Only overflowing content needs an extra keyboard stop.
    const scrollRegions = [...document.querySelectorAll('.article-body pre, .article-body table')];
    const managedRegions = scrollRegions.filter(region => !region.hasAttribute('tabindex'));
    const regionObserver = new ResizeObserver(() => {
        for (const region of managedRegions) {
            if (region.scrollWidth > region.clientWidth + 1) {
                region.tabIndex = 0;
            } else {
                region.removeAttribute('tabindex');
            }
        }
    });
    for (const region of managedRegions) regionObserver.observe(region);

    const articleBody = document.querySelector('.article-body');
    const headings = [...document.querySelectorAll('.article-body :is(h2, h3, h4, h5, h6)[id]')];
    const tocLinks = [...document.querySelectorAll('[data-toc] a')];
    if (!headings.length || !tocLinks.length) return;

    const grid = document.querySelector('.article-grid');
    const mobileToc = document.querySelector('.mobile-toc');
    const summary = mobileToc?.querySelector('summary');
    const currentLabel = mobileToc?.querySelector('.toc-current');
    const desktopToc = document.querySelector('.page-toc nav');
    const linksByHeading = new Map();
    for (const link of tocLinks) {
        const id = decodeURIComponent(link.hash.slice(1));
        if (!linksByHeading.has(id)) linksByHeading.set(id, []);
        linksByHeading.get(id).push(link);
    }

    let scheduled = false;
    let currentHeading;
    function revealCurrentLink(container) {
        if (!container || !container.clientHeight) return;
        const link = container.querySelector('[aria-current="location"]');
        if (!link) return;

        const bounds = container.getBoundingClientRect();
        const item = link.getBoundingClientRect();
        if (item.top < bounds.top + 12) {
            container.scrollTop -= bounds.top + 12 - item.top;
        } else if (item.bottom > bounds.bottom - 12) {
            container.scrollTop += item.bottom - bounds.bottom + 12;
        }
    }

    function updateCurrentHeading() {
        scheduled = false;
        const mobileHeight = summary?.getBoundingClientRect().height || 0;
        const offset = header.offsetHeight + mobileHeight + 24;
        grid.style.setProperty('--reading-offset', offset + 'px');

        let current = headings.findLast(heading => heading.getBoundingClientRect().top <= offset + 2) || headings[0];
        // A short final section may never reach the top of the viewport.
        const bodyBounds = articleBody.getBoundingClientRect();
        if (bodyBounds.top < offset && bodyBounds.bottom <= innerHeight - 24) current = headings.at(-1);
        if (current === currentHeading) return;
        currentHeading = current;

        for (const link of tocLinks) link.removeAttribute('aria-current');
        const currentLinks = linksByHeading.get(current.id) || [];
        for (const link of currentLinks) link.setAttribute('aria-current', 'location');
        if (currentLabel) currentLabel.textContent = currentLinks[0]?.textContent || '';
        // Leave the directory under the reader's control while they interact with it.
        if (!desktopToc?.matches(':hover, :focus-within')) revealCurrentLink(desktopToc);
    }

    function scheduleHeadingUpdate() {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(updateCurrentHeading);
    }

    addEventListener('scroll', scheduleHeadingUpdate, { passive: true });
    addEventListener('resize', scheduleHeadingUpdate);
    const readingObserver = new ResizeObserver(scheduleHeadingUpdate);
    readingObserver.observe(header);
    readingObserver.observe(articleBody);
    if (summary) readingObserver.observe(summary);
    updateCurrentHeading();

    mobileToc?.addEventListener('toggle', () => {
        if (mobileToc.open) revealCurrentLink(mobileToc.querySelector('nav'));
    });
    mobileToc?.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || !mobileToc.open) return;
        event.preventDefault();
        mobileToc.open = false;
        summary.focus({ preventScroll: true });
    });
    document.addEventListener('click', event => {
        if (mobileToc?.open && !mobileToc.contains(event.target)) mobileToc.open = false;
    });

    for (const link of tocLinks) {
        link.addEventListener('click', event => {
            if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            const target = document.getElementById(decodeURIComponent(link.hash.slice(1)));
            if (!target) return;
            if (mobileToc) mobileToc.open = false;
            // Keep native fragment history; focus follows keyboard navigation on both layouts.
            requestAnimationFrame(() => {
                updateCurrentHeading();
                if (!target.hasAttribute('tabindex')) target.tabIndex = -1;
                target.focus({ preventScroll: true });
                target.scrollIntoView({ behavior: 'auto', block: 'start' });
            });
        });
    }
})();
