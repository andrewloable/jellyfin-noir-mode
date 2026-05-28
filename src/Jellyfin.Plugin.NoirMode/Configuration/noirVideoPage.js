(() => {
    const containerId = 'noirModeVideoPageSelection';
    const selectId = 'noirModeVideoPageSelect';
    const styleId = 'noirModeVideoPageStyles';
    let presetsPromise;
    let lastItemId;
    let renderTimer;
    let renderVersion = 0;
    let selectorPausedUntil = 0;

    const api = (path, options) => ApiClient.ajax(Object.assign({
        url: ApiClient.getUrl(path),
        type: 'GET',
        dataType: 'json',
        contentType: 'application/json'
    }, options));

    const read = (value, camelName, fallback) => {
        if (!value) {
            return fallback;
        }

        if (Object.prototype.hasOwnProperty.call(value, camelName)) {
            return value[camelName];
        }

        const pascalName = camelName.charAt(0).toUpperCase() + camelName.slice(1);
        if (Object.prototype.hasOwnProperty.call(value, pascalName)) {
            return value[pascalName];
        }

        return fallback;
    };

    const asArray = value => {
        if (Array.isArray(value)) {
            return value;
        }

        if (typeof value === 'string' && value) {
            try {
                const parsed = JSON.parse(value);
                return Array.isArray(parsed) ? parsed : [];
            } catch (error) {
                console.debug('Noir Mode response was not a JSON array', error);
                return [];
            }
        }

        return [];
    };

    const isPresetMode = mode => {
        if (mode === 2) {
            return true;
        }

        return typeof mode === 'string' && mode.toLowerCase() === 'preset';
    };

    const getRouteItemId = () => {
        const hash = window.location.hash || '';
        const hashQueryIndex = hash.indexOf('?');
        if (hashQueryIndex >= 0) {
            const value = new URLSearchParams(hash.substring(hashQueryIndex + 1)).get('id');
            if (value) {
                return value;
            }
        }

        const routeMatch = hash.match(/(?:details|itemdetails|video|movies?|episodes?)[^a-f0-9-]*([a-f0-9]{32}|[a-f0-9-]{36})/i);
        if (routeMatch) {
            return routeMatch[1].replaceAll('-', '');
        }

        return new URLSearchParams(window.location.search).get('id');
    };

    const isMediaLabel = element => {
        const text = element.textContent ? element.textContent.trim() : '';
        return text === 'Video' || text === 'Audio' || text === 'Subtitles';
    };

    const findModernMediaAnchor = page => {
        const labels = Array.from(page.querySelectorAll('label, span, div, p'))
            .filter(element => element.children.length === 0 && isMediaLabel(element));
        const subtitleLabel = labels.find(element => element.textContent.trim() === 'Subtitles');
        const label = subtitleLabel || labels[labels.length - 1];

        if (!label) {
            return null;
        }

        return label.closest('[class*="MuiGrid"], [class*="item"], [class*="row"]')
            || label.parentElement;
    };

    const findInsertionAnchor = page => {
        const subtitleContainer = page.querySelector('.selectSubtitlesContainer');
        if (subtitleContainer && !subtitleContainer.classList.contains('hide')) {
            return subtitleContainer;
        }

        return page.querySelector('.trackSelections')
            || page.querySelector('.selectAudioContainer')
            || page.querySelector('.selectVideoContainer')
            || findModernMediaAnchor(page);
    };

    const ensureStyles = () => {
        if (document.getElementById(styleId)) {
            return;
        }

        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
            #${containerId} {
                display: grid;
                grid-template-columns: 6em minmax(12em, 32em);
                align-items: center;
                column-gap: 2em;
                margin: .35em 0;
                max-width: 48em;
            }

            #${containerId} .selectLabel {
                margin: 0;
            }

            #${containerId} .noirModeNativeSelect {
                appearance: auto;
                -webkit-appearance: menulist;
                background: #2b2b2b;
                border: 1px solid rgba(255, 255, 255, .15);
                border-radius: 3px;
                color: inherit;
                min-height: 2.25em;
                padding: .35em .6em;
                width: 100%;
            }

            #${containerId} .noirModeStatus {
                grid-column: 2;
                min-height: 1.25em;
            }
        `;
        document.head.appendChild(style);
    };

    const getPresets = () => {
        presetsPromise = presetsPromise || api('NoirMode/presets').catch(error => {
            presetsPromise = undefined;
            throw error;
        });
        return presetsPromise;
    };

    const createContainer = () => {
        const container = document.createElement('div');
        container.id = containerId;
        container.className = 'noirModeSelectionContainer';

        const label = document.createElement('label');
        label.className = 'selectLabel';
        label.htmlFor = selectId;
        label.textContent = 'Noir Mode';

        const select = document.createElement('select');
        select.id = selectId;
        select.className = 'noirModeNativeSelect';

        const status = document.createElement('div');
        status.className = 'fieldDescription noirModeStatus';
        status.setAttribute('aria-live', 'polite');

        container.append(label, select, status);
        return container;
    };

    const ensureContainer = (page, anchor) => {
        ensureStyles();

        let container = document.getElementById(containerId);
        if (!container) {
            container = createContainer();
        }

        if (anchor && !anchor.classList.contains('trackSelections')) {
            anchor.insertAdjacentElement('afterend', container);
            return container;
        }

        if (anchor) {
            anchor.insertAdjacentElement('afterend', container);
            return container;
        }

        const detailContent = page.querySelector('.detailPageContent') || page.querySelector('.mainDetailButtons') || page;
        detailContent.appendChild(container);
        return container;
    };

    const setOptions = (select, presets, override) => {
        const selectedPreset = read(override, 'presetId', '');
        const selectedValue = isPresetMode(read(override, 'mode', 0)) && selectedPreset ? selectedPreset : 'off';
        const currentValues = Array.from(select.options).map(option => `${option.value}:${option.textContent}`).join('|');
        const presetItems = asArray(presets);
        const nextValues = [`off:Off`, ...presetItems.map(preset => `${read(preset, 'id', '')}:${read(preset, 'label', '')}`)].join('|');

        if (currentValues === nextValues) {
            select.value = selectedValue;
            return;
        }

        select.replaceChildren();

        const off = document.createElement('option');
        off.value = 'off';
        off.textContent = 'Off';
        select.appendChild(off);

        for (const preset of presetItems) {
            const option = document.createElement('option');
            option.value = read(preset, 'id', '');
            option.textContent = read(preset, 'label', '');
            select.appendChild(option);
        }

        select.value = selectedValue;
    };

    const saveSelection = async (itemId, select, status) => {
        select.disabled = true;
        status.textContent = 'Saving...';

        try {
            if (select.value === 'off') {
                await api(`NoirMode/items/${encodeURIComponent(itemId)}/override`, {
                    type: 'PUT',
                    data: JSON.stringify({ itemId, mode: 1, presetId: null })
                });
            } else {
                await api(`NoirMode/items/${encodeURIComponent(itemId)}/override`, {
                    type: 'PUT',
                    data: JSON.stringify({ itemId, mode: 2, presetId: select.value })
                });
            }

            status.textContent = 'Saved';
        } catch (error) {
            console.error('Noir Mode selection save failed', error);
            status.textContent = 'Save failed';
        } finally {
            select.disabled = false;
        }
    };

    const pauseSelectorRefresh = () => {
        selectorPausedUntil = Date.now() + 3000;
    };

    const isSelectorInteractionActive = () => {
        const select = document.getElementById(selectId);
        return !!select && (document.activeElement === select || Date.now() < selectorPausedUntil);
    };

    const stopSelectEvent = event => {
        pauseSelectorRefresh();
        event.stopPropagation();
    };

    const removeContainer = () => {
        const container = document.getElementById(containerId);
        if (container) {
            container.remove();
        }
    };

    const render = async () => {
        const version = ++renderVersion;
        const itemId = getRouteItemId();
        if (!itemId || !window.ApiClient) {
            removeContainer();
            lastItemId = undefined;
            return;
        }

        if (isSelectorInteractionActive()) {
            return;
        }

        const page = document.querySelector('.itemDetailPage') || document.body;
        if (!page) {
            return;
        }

        try {
            const anchor = findInsertionAnchor(page);
            if (!anchor) {
                removeContainer();
                lastItemId = undefined;
                return;
            }

            const [presets, override] = await Promise.all([
                getPresets(),
                api(`NoirMode/items/${encodeURIComponent(itemId)}/override`)
            ]);
            if (version !== renderVersion) {
                return;
            }

            const container = ensureContainer(page, anchor);
            const select = container.querySelector('select');
            const status = container.querySelector('.noirModeStatus');

            setOptions(select, presets, override);
            status.textContent = '';
            select.onpointerdown = stopSelectEvent;
            select.onmousedown = stopSelectEvent;
            select.ontouchstart = stopSelectEvent;
            select.onclick = stopSelectEvent;
            select.onfocus = pauseSelectorRefresh;
            select.onblur = () => {
                selectorPausedUntil = Date.now() + 150;
                scheduleRender();
            };

            if (lastItemId !== itemId) {
                select.onchange = () => {
                    pauseSelectorRefresh();
                    saveSelection(itemId, select, status);
                };
                lastItemId = itemId;
            }
        } catch (error) {
            console.debug('Noir Mode video page selection skipped', error);
            if (!lastItemId || lastItemId !== itemId) {
                removeContainer();
                lastItemId = undefined;
            }
        }
    };

    const scheduleRender = () => {
        window.clearTimeout(renderTimer);
        renderTimer = window.setTimeout(render, isSelectorInteractionActive() ? 750 : 150);
    };

    window.addEventListener('hashchange', scheduleRender);
    window.addEventListener('popstate', scheduleRender);
    document.addEventListener('viewshow', scheduleRender);
    document.addEventListener('pageshow', scheduleRender);

    new MutationObserver(mutations => {
        if (mutations.every(mutation => mutation.target.closest && mutation.target.closest(`#${containerId}`))) {
            return;
        }

        scheduleRender();
    }).observe(document.body, {
        childList: true,
        subtree: true
    });

    scheduleRender();
})();
