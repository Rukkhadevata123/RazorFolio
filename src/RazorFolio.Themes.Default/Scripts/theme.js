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

    const headings = [...document.querySelectorAll('.article-body :is(h2, h3, h4, h5, h6)[id]')];
    const tocLinks = [...document.querySelectorAll('[data-toc] a')];
    if (!headings.length) return;

    let scheduled = false;
    function updateCurrentHeading() {
        scheduled = false;
        const offset = document.querySelector('.site-header').offsetHeight + 35;
        const current = headings.findLast(heading => heading.getBoundingClientRect().top <= offset) || headings[0];
        for (const link of tocLinks) {
            if (decodeURIComponent(link.hash.slice(1)) === current.id) {
                link.setAttribute('aria-current', 'location');
            } else {
                link.removeAttribute('aria-current');
            }
        }
    }
    addEventListener('scroll', () => {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(updateCurrentHeading);
    }, { passive: true });
    addEventListener('resize', updateCurrentHeading);
    updateCurrentHeading();
    for (const link of document.querySelectorAll('.mobile-toc a')) {
        link.addEventListener('click', () => {
            link.closest('details').open = false;
            const target = document.getElementById(decodeURIComponent(link.hash.slice(1)));
            requestAnimationFrame(() => {
                target?.setAttribute('tabindex', '-1');
                target?.focus({ preventScroll: true });
                target?.scrollIntoView();
            });
        });
    }
})();
