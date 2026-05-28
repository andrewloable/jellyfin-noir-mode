(() => {
    const containerId = 'noirModeVideoPageSelection';
    const selectId = 'noirModeVideoPageSelect';
    const styleId = 'noirModeVideoPageStyles';
    let presetsPromise;
    let lastItemId;
    let renderTimer;
    let renderVersion = 0;

    const api = (path, options) => ApiClient.ajax(Object.assign({
        url: ApiClient.getUrl(path),
        type: 'GET',
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

    const hasVideoControls = page => Boolean(
        page.querySelector('.trackSelections')
        || page.querySelector('.selectVideoContainer')
        || page.querySelector('.selectAudioContainer')
        || page.querySelector('.selectSubtitlesContainer'));

    const ensureStyles = () => {
        if (document.getElementById(styleId)) {
            return;
        }

        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
            #${containerId} {
                margin-top: 1em;
                max-width: 32em;
            }

            #${containerId}.noirModeInlineSelection {
                margin-top: 0;
            }

            #${containerId} .noirModeStatus {
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
        container.className = 'selectContainer noirModeSelectionContainer';

        const label = document.createElement('label');
        label.className = 'selectLabel';
        label.htmlFor = selectId;
        label.textContent = 'Noir Mode';

        const select = document.createElement('select');
        select.id = selectId;
        select.className = 'emby-select-withcolor emby-select';

        const status = document.createElement('div');
        status.className = 'fieldDescription noirModeStatus';
        status.setAttribute('aria-live', 'polite');

        container.append(label, select, status);
        return container;
    };

    const ensureContainer = page => {
        ensureStyles();

        let container = document.getElementById(containerId);
        if (!container) {
            container = createContainer();
        }

        const trackSelections = page.querySelector('.trackSelections');
        const subtitleContainer = page.querySelector('.selectSubtitlesContainer');
        if (trackSelections && subtitleContainer && !trackSelections.classList.contains('hide')) {
            container.classList.add('noirModeInlineSelection');
            subtitleContainer.insertAdjacentElement('afterend', container);
            return container;
        }

        container.classList.remove('noirModeInlineSelection');
        if (trackSelections) {
            trackSelections.insertAdjacentElement('afterend', container);
            return container;
        }

        const detailContent = page.querySelector('.detailPageContent') || page.querySelector('.mainDetailButtons') || page;
        detailContent.appendChild(container);
        return container;
    };

    const setOptions = (select, presets, override) => {
        const selectedPreset = read(override, 'presetId', '');
        const selectedValue = read(override, 'mode', 0) === 2 && selectedPreset ? selectedPreset : 'off';
        const currentValues = Array.from(select.options).map(option => `${option.value}:${option.textContent}`).join('|');
        const nextValues = [`off:Off`, ...presets.map(preset => `${read(preset, 'id', '')}:${read(preset, 'label', '')}`)].join('|');

        if (currentValues === nextValues) {
            select.value = selectedValue;
            return;
        }

        select.replaceChildren();

        const off = document.createElement('option');
        off.value = 'off';
        off.textContent = 'Off';
        select.appendChild(off);

        for (const preset of presets) {
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

        const page = document.querySelector('.itemDetailPage') || document.body;
        if (!page) {
            return;
        }

        try {
            if (!hasVideoControls(page)) {
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

            const container = ensureContainer(page);
            const select = container.querySelector('select');
            const status = container.querySelector('.noirModeStatus');

            setOptions(select, presets, override);
            status.textContent = '';

            if (lastItemId !== itemId) {
                select.onchange = () => saveSelection(itemId, select, status);
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
        renderTimer = window.setTimeout(render, 150);
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
