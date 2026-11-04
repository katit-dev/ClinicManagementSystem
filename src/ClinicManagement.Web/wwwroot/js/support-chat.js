export function scrollToLatest(element) { if (element) element.scrollTop = element.scrollHeight; }
export function isReadingLatest(element) {
    if (!element || document.visibilityState !== 'visible' || !document.hasFocus()) return false;
    const last = element.querySelector('[data-message-sequence]:last-of-type');
    if (!last) return false;
    const item = last.getBoundingClientRect(), box = element.getBoundingClientRect();
    const top = window.visualViewport?.offsetTop || 0;
    const bottom = top + (window.visualViewport?.height || window.innerHeight);
    return item.bottom <= Math.min(box.bottom, bottom) + 2 && item.bottom > Math.max(box.top, top)
        && box.top < bottom && box.bottom > top;
}

const widgets = new WeakMap();
const threads = new WeakMap();
const nearLatest = element => element.scrollHeight - element.clientHeight - element.scrollTop <= 32;

// Remember the position before Blazor replaces/appends message DOM nodes.
export function updateThread(element, force = false) {
    if (!element) return false;
    let state = threads.get(element);
    if (!state) {
        state = { atBottom: true, height: element.scrollHeight, top: element.scrollTop, first: null, last: 0, seen: 0 };
        state.scroll = () => {
            state.atBottom = nearLatest(element);
            state.height = element.scrollHeight; state.top = element.scrollTop;
            if (state.atBottom) state.seen = state.last;
        };
        element.addEventListener('scroll', state.scroll);
        threads.set(element, state);
    }
    const items = element.querySelectorAll('[data-message-sequence]');
    if (!items.length) return false;
    const first = Number(items[0].dataset.messageSequence), last = Number(items[items.length - 1].dataset.messageSequence);
    if (force || state.first === null || last > state.last && state.atBottom) {
        scrollToLatest(element); state.seen = last;
    } else if (first < state.first) {
        element.scrollTop = state.top + element.scrollHeight - state.height;
    }
    state.first = first; state.last = last; state.atBottom = nearLatest(element);
    state.height = element.scrollHeight; state.top = element.scrollTop;
    if (state.atBottom) state.seen = last;
    return last > state.seen && !state.atBottom;
}

export function disposeThread(element) {
    const state = threads.get(element);
    if (state) { element.removeEventListener('scroll', state.scroll); threads.delete(element); }
}

// The visual viewport shrinks when a mobile keyboard opens; the layout viewport may not.
export function mountChat(element) {
    if (!element || widgets.has(element)) return;
    const viewport = window.visualViewport;
    const mobile = window.matchMedia('(max-width: 720px), (max-height: 500px) and (pointer: coarse)');
    let baseline = viewport?.height || window.innerHeight;
    let lastWidth = document.documentElement.clientWidth;
    let frame = 0, locked = false, savedBody, savedScroll;
    const previousFocus = document.activeElement;
    const editable = node => node instanceof HTMLElement && node.matches('textarea,input,select,[contenteditable="true"]');
    function lockBody() {
        if (locked) return;
        locked = true;
        savedScroll = window.scrollY;
        savedBody = ['position', 'top', 'left', 'right', 'width', 'overflow'].map(key => [key, document.body.style[key]]);
        Object.assign(document.body.style, { position: 'fixed', top: `-${savedScroll}px`, left: '0', right: '0', width: '100%', overflow: 'hidden' });
    }
    function unlockBody() {
        if (!locked) return;
        locked = false;
        for (const [key, value] of savedBody) document.body.style[key] = value;
        window.scrollTo({ top: savedScroll, behavior: 'instant' });
    }
    function update() {
        frame = 0;
        if (!element.isConnected) { unmountChat(element); return; }
        // Lock first: changing body positioning can change Safari's viewport measurements.
        if (mobile.matches) { element.setAttribute('aria-modal', 'true'); lockBody(); }
        else { element.removeAttribute('aria-modal'); unlockBody(); }
        const layoutHeight = document.documentElement.clientHeight || window.innerHeight;
        const height = Math.max(0, Math.min(viewport?.height || window.innerHeight, window.innerHeight, layoutHeight));
        const offset = Math.min(Math.max(0, viewport?.offsetTop || 0), Math.max(0, layoutHeight - height));
        const focused = element.contains(document.activeElement) && editable(document.activeElement);
        const width = document.documentElement.clientWidth;
        if (!focused || Math.abs(width - lastWidth) > 80) baseline = height;
        lastWidth = width;
        baseline = Math.max(baseline, height);
        const keyboard = mobile.matches && focused && baseline - height > 120;
        element.classList.toggle('support-keyboard-open', keyboard);
        const desired = Math.min(720, height * .82);
        const sheetHeight = Math.max(0, Math.min(height - 16, keyboard ? height - 16 : desired));
        // Use one coordinate system (top + height). Fixed bottom and innerHeight can
        // refer to different bounds while a mobile browser toolbar is expanded.
        element.style.setProperty('--support-sheet-top', `${offset + height - sheetHeight - 8}px`);
        element.style.setProperty('--support-sheet-height', `${sheetHeight}px`);
        element.classList.add('support-viewport-ready');
        for (const input of element.querySelectorAll('.support-composer textarea')) {
            input.style.height = 'auto';
            const style = getComputedStyle(input);
            input.style.height = `${Math.min(parseFloat(style.maxHeight), Math.max(parseFloat(style.minHeight), input.scrollHeight + 2))}px`;
        }
    }
    function schedule() { if (!frame) frame = requestAnimationFrame(update); }
    function keydown(event) {
        if (event.key !== 'Tab' || !mobile.matches) return;
        const root = element.querySelector('[role="alertdialog"]') || element;
        const controls = [...root.querySelectorAll('button,a[href],input,select,textarea,[tabindex="0"]')]
            .filter(node => !node.disabled && node.getClientRects().length && getComputedStyle(node).visibility !== 'hidden');
        if (!controls.length) return;
        const first = controls[0], last = controls.at(-1);
        if (!root.contains(document.activeElement) || event.shiftKey && document.activeElement === first || !event.shiftKey && document.activeElement === last) {
            event.preventDefault(); (event.shiftKey ? last : first).focus({ preventScroll: true });
        }
    }
    const observer = new MutationObserver(schedule);
    observer.observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-support-view'] });
    window.addEventListener('resize', schedule);
    window.addEventListener('orientationchange', schedule);
    viewport?.addEventListener('resize', schedule);
    viewport?.addEventListener('scroll', schedule);
    element.addEventListener('focusin', schedule);
    element.addEventListener('focusout', schedule);
    element.addEventListener('input', schedule);
    element.addEventListener('keydown', keydown);
    widgets.set(element, () => {
        observer.disconnect(); cancelAnimationFrame(frame);
        window.removeEventListener('resize', schedule);
        window.removeEventListener('orientationchange', schedule);
        viewport?.removeEventListener('resize', schedule);
        viewport?.removeEventListener('scroll', schedule);
        element.removeEventListener('focusin', schedule);
        element.removeEventListener('focusout', schedule);
        element.removeEventListener('input', schedule);
        element.removeEventListener('keydown', keydown);
        unlockBody();
        const trigger = previousFocus?.isConnected ? previousFocus : document.querySelector('.support-fab');
        trigger?.focus({ preventScroll: true });
    });
    update();
    element.querySelector('.support-minimize')?.focus({ preventScroll: true });
}

export function unmountChat(element) {
    const dispose = widgets.get(element);
    if (dispose) { widgets.delete(element); dispose(); }
}
