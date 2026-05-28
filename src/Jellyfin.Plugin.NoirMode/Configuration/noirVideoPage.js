(() => {
    const styleId = 'noirModeMenuStyles';
    const menuItemClass = 'noirModeMenuItem';
    const dialogId = 'noirModePresetDialog';
    const commandId = 'noirmode';
    let presetsPromise;
    let pendingMenuItem;

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
            return routeMatch[1].replace(/-/g, '');
        }

        return new URLSearchParams(window.location.search).get('id');
    };

    const getMenuItemId = target => {
        const menuButton = target.closest('.btnMoreCommands, [data-action="menu"]');
        if (!menuButton) {
            return null;
        }

        const selectedSource = document.querySelector('.itemDetailPage .selectSource');
        if (menuButton.classList.contains('btnMoreCommands') && selectedSource && selectedSource.value) {
            return selectedSource.value;
        }

        const itemElement = menuButton.closest('[data-id]');
        return itemElement ? itemElement.getAttribute('data-id') : getRouteItemId();
    };

    const getPresets = () => {
        presetsPromise = presetsPromise || api('NoirMode/presets').catch(error => {
            presetsPromise = undefined;
            throw error;
        });
        return presetsPromise;
    };

    const getCurrentUserId = () => {
        if (ApiClient.getCurrentUserId) {
            return ApiClient.getCurrentUserId();
        }

        return null;
    };

    const getItem = itemId => {
        const userId = getCurrentUserId();
        if (ApiClient.getItem && userId) {
            return ApiClient.getItem(userId, itemId);
        }

        if (!userId) {
            return Promise.reject(new Error('No Jellyfin user is available for Noir Mode item lookup.'));
        }

        return api(`Users/${encodeURIComponent(userId)}/Items/${encodeURIComponent(itemId)}`);
    };

    const isSupportedVideoItem = item => {
        const itemType = read(item, 'type', '');
        const mediaType = read(item, 'mediaType', '');
        const isFolder = read(item, 'isFolder', false);
        return !isFolder && (
            itemType === 'Movie'
            || itemType === 'Episode'
            || itemType === 'Video'
            || mediaType === 'Video'
        );
    };

    const ensureStyles = () => {
        if (document.getElementById(styleId)) {
            return;
        }

        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
            .${menuItemClass} .material-icons {
                color: inherit;
            }

            .noirModeDialogBackdrop {
                align-items: center;
                background: rgba(0, 0, 0, .58);
                bottom: 0;
                display: flex;
                justify-content: center;
                left: 0;
                position: fixed;
                right: 0;
                top: 0;
                z-index: 99999;
            }

            .noirModeDialog {
                background: #202020;
                box-shadow: 0 1em 3em rgba(0, 0, 0, .45);
                color: inherit;
                max-height: min(80vh, 34em);
                max-width: min(92vw, 28em);
                min-width: min(92vw, 20em);
                overflow: hidden;
            }

            .noirModeDialog .actionSheetScroller {
                max-height: 23em;
                overflow-y: auto;
            }

            .noirModeDialogStatus {
                min-height: 1.5em;
                padding: .6em 1.25em 1em;
            }
        `;
        document.head.appendChild(style);
    };

    const closeNoirDialog = () => {
        const dialog = document.getElementById(dialogId);
        if (dialog) {
            dialog.remove();
        }
    };

    const createActionButton = (id, icon, name, selected) => {
        const button = document.createElement('button');
        button.setAttribute('is', 'emby-button');
        button.type = 'button';
        button.className = 'listItem listItem-button actionSheetMenuItem';
        button.dataset.id = id;

        const iconElement = document.createElement('span');
        iconElement.className = `actionsheetMenuItemIcon listItemIcon listItemIcon-transparent material-icons ${selected ? 'check' : icon}`;
        iconElement.setAttribute('aria-hidden', 'true');

        const body = document.createElement('div');
        body.className = 'listItemBody actionsheetListItemBody';

        const text = document.createElement('div');
        text.className = 'listItemBodyText actionSheetItemText';
        text.textContent = name;

        body.appendChild(text);
        button.append(iconElement, body);
        return button;
    };

    const setChildren = (element, children) => {
        while (element.firstChild) {
            element.removeChild(element.firstChild);
        }

        for (const child of children) {
            element.appendChild(child);
        }
    };

    const saveSelection = async (itemId, presetId, status) => {
        status.textContent = 'Saving...';

        if (presetId === 'off') {
            await api(`NoirMode/items/${encodeURIComponent(itemId)}/override`, {
                type: 'PUT',
                data: JSON.stringify({ itemId, mode: 1, presetId: null })
            });
            return;
        }

        await api(`NoirMode/items/${encodeURIComponent(itemId)}/override`, {
            type: 'PUT',
            data: JSON.stringify({ itemId, mode: 2, presetId })
        });
    };

    const showNoirModeDialog = async itemId => {
        closeNoirDialog();
        ensureStyles();

        const backdrop = document.createElement('div');
        backdrop.id = dialogId;
        backdrop.className = 'noirModeDialogBackdrop';

        const dialog = document.createElement('div');
        dialog.className = 'noirModeDialog actionSheet actionsheet-not-fullscreen';
        dialog.setAttribute('role', 'dialog');
        dialog.setAttribute('aria-modal', 'true');
        dialog.setAttribute('aria-label', 'Noir Mode');

        const content = document.createElement('div');
        content.className = 'actionSheetContent';

        const title = document.createElement('h1');
        title.className = 'actionSheetTitle';
        title.textContent = 'Noir Mode';

        const scroller = document.createElement('div');
        scroller.className = 'actionSheetScroller scrollY';

        const status = document.createElement('div');
        status.className = 'fieldDescription noirModeDialogStatus';
        status.setAttribute('aria-live', 'polite');
        status.textContent = 'Loading...';

        content.append(title, scroller, status);
        dialog.appendChild(content);
        backdrop.appendChild(dialog);
        document.body.appendChild(backdrop);

        backdrop.addEventListener('click', event => {
            if (event.target === backdrop) {
                closeNoirDialog();
            }
        });

        try {
            const [presets, override] = await Promise.all([
                getPresets(),
                api(`NoirMode/items/${encodeURIComponent(itemId)}/override`)
            ]);
            const selectedPreset = read(override, 'presetId', '');
            const selectedValue = isPresetMode(read(override, 'mode', 0)) && selectedPreset ? selectedPreset : 'off';
            const presetItems = asArray(presets);
            const buttons = [
                createActionButton('off', 'filter_b_and_w', 'Off', selectedValue === 'off'),
                ...presetItems.map(preset => {
                    const id = read(preset, 'id', '');
                    return createActionButton(id, 'filter_b_and_w', read(preset, 'label', id), selectedValue === id);
                })
            ];

            setChildren(scroller, buttons);
            status.textContent = '';

            for (const button of buttons) {
                button.addEventListener('click', async () => {
                    scroller.querySelectorAll('button').forEach(menuButton => {
                        menuButton.disabled = true;
                    });

                    try {
                        await saveSelection(itemId, button.dataset.id, status);
                        status.textContent = 'Saved';
                        window.setTimeout(closeNoirDialog, 350);
                    } catch (error) {
                        console.error('Noir Mode selection save failed', error);
                        status.textContent = 'Save failed';
                        scroller.querySelectorAll('button').forEach(menuButton => {
                            menuButton.disabled = false;
                        });
                    }
                });
            }
        } catch (error) {
            console.error('Noir Mode menu failed to load', error);
            status.textContent = 'Noir Mode could not load for this item.';
        }
    };

    const createNoirMenuItem = itemId => {
        const button = createActionButton(commandId, 'filter_b_and_w', 'Noir Mode', false);
        button.classList.add(menuItemClass);
        button.addEventListener('click', () => {
            window.setTimeout(() => showNoirModeDialog(itemId), 50);
        });
        return button;
    };

    const injectNoirMenuItem = () => {
        if (!pendingMenuItem || pendingMenuItem.expiresAt < Date.now() || !pendingMenuItem.item) {
            return;
        }

        const actionSheet = document.querySelector('.actionSheet');
        const scroller = actionSheet ? actionSheet.querySelector('.actionSheetScroller') : null;
        if (!scroller || scroller.querySelector(`.${menuItemClass}`)) {
            return;
        }

        const firstDivider = scroller.querySelector('.actionsheetDivider');
        const menuItem = createNoirMenuItem(pendingMenuItem.itemId);
        if (firstDivider) {
            firstDivider.insertAdjacentElement('afterend', menuItem);
        } else {
            scroller.appendChild(menuItem);
        }
    };

    const handleMoreMenuClick = event => {
        const itemId = getMenuItemId(event.target);
        if (!itemId || !window.ApiClient) {
            return;
        }

        pendingMenuItem = {
            itemId,
            expiresAt: Date.now() + 5000,
            item: null
        };

        getItem(itemId)
            .then(item => {
                if (!pendingMenuItem || pendingMenuItem.itemId !== itemId || !isSupportedVideoItem(item)) {
                    return;
                }

                pendingMenuItem.item = item;
                injectNoirMenuItem();
            })
            .catch(error => {
                console.debug('Noir Mode More menu item skipped', error);
            });
    };

    ensureStyles();

    document.addEventListener('click', handleMoreMenuClick, true);

    new MutationObserver(injectNoirMenuItem).observe(document.body, {
        childList: true,
        subtree: true
    });
})();
