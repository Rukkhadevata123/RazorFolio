(() => {
    const article = document.querySelector('.article-body');
    if (!article || typeof HTMLDialogElement === 'undefined') return;

    let dialog;
    let opener;
    let zoomed = false;
    let originalWidth = 0;

    function setZoom(value) {
        zoomed = value;
        dialog.dataset.zoomed = String(value);
        const button = dialog.querySelector('[data-image-zoom]');
        button.textContent = value ? '适应窗口' : '原始尺寸';
        button.setAttribute('aria-pressed', String(value));
        const image = dialog.querySelector('img');
        image.style.width = value ? originalWidth + 'px' : '';
        const viewport = dialog.querySelector('.image-viewport');
        viewport.scrollLeft = Math.max(0, (viewport.scrollWidth - viewport.clientWidth) / 2);
        viewport.scrollTop = Math.max(0, (viewport.scrollHeight - viewport.clientHeight) / 2);
    }

    function createDialog() {
        const element = document.createElement('dialog');
        element.className = 'image-dialog';
        element.setAttribute('aria-labelledby', 'rf-image-title');
        element.innerHTML = `
            <div class="image-toolbar">
                <h2 id="rf-image-title">图片预览</h2>
                <button type="button" data-image-close autofocus>关闭</button>
            </div>
            <div class="image-actions">
                <button type="button" data-image-zoom aria-pressed="false">原始尺寸</button>
                <a data-image-original target="_blank" rel="noopener noreferrer">打开原图（新标签页）</a>
            </div>
            <p class="image-status" role="status"></p>
            <div class="image-viewport" tabindex="0" role="region" aria-label="图片，可用方向键滚动">
                <img alt="" draggable="false">
            </div>
            <p class="image-caption"></p>`;
        element.querySelector('[data-image-close]').addEventListener('click', () => element.close());
        element.querySelector('[data-image-zoom]').addEventListener('click', () => setZoom(!zoomed));
        element.addEventListener('close', () => opener?.focus({ preventScroll: true }));

        let startedOutside = false;
        const outside = event => {
            const bounds = element.getBoundingClientRect();
            return event.clientX < bounds.left || event.clientX > bounds.right
                || event.clientY < bounds.top || event.clientY > bounds.bottom;
        };
        element.addEventListener('pointerdown', event => { startedOutside = outside(event); });
        element.addEventListener('click', event => {
            if (startedOutside && outside(event)) element.close();
        });
        document.body.append(element);
        return element;
    }

    function openImage(image, button) {
        dialog ||= createDialog();
        opener = button;
        const preview = dialog.querySelector('img');
        const status = dialog.querySelector('.image-status');
        const zoom = dialog.querySelector('[data-image-zoom]');
        const source = image.currentSrc || image.src;
        originalWidth = image.naturalWidth;
        preview.alt = image.alt;
        preview.hidden = false;
        zoom.disabled = true;
        status.textContent = '正在加载图片…';
        dialog.querySelector('.image-caption').textContent = image.alt;
        dialog.querySelector('[data-image-original]').href = source;
        preview.onload = () => {
            originalWidth = preview.naturalWidth;
            zoom.disabled = false;
            status.textContent = '';
        };
        preview.onerror = () => {
            preview.hidden = true;
            zoom.disabled = true;
            status.textContent = '图片未能加载，可打开原图重试。';
        };
        setZoom(false);
        preview.src = source;
        dialog.showModal();
    }

    for (const image of article.querySelectorAll('img')) {
        function enhance() {
            // Keep authored links, interactive content and decorative icons unchanged.
            if (!image.naturalWidth || !image.alt.trim() || image.hasAttribute('data-no-preview')
                || image.closest('a, button, summary')
                || (image.naturalWidth < 160 && image.naturalHeight < 160)) return;
            const target = image.parentElement.tagName === 'PICTURE' ? image.parentElement : image;
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'image-open';
            button.setAttribute('aria-label', '查看大图：' + image.alt);
            button.setAttribute('aria-haspopup', 'dialog');
            const hint = document.createElement('span');
            hint.className = 'image-hint';
            hint.textContent = '查看大图';
            hint.setAttribute('aria-hidden', 'true');
            target.before(button);
            button.append(target, hint);
            button.addEventListener('click', () => openImage(image, button));
        }
        image.addEventListener('load', enhance);
        if (image.complete) enhance();
    }
})();
