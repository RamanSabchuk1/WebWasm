export function isInputFocused() {
    const tag = document.activeElement?.tagName ?? '';
    return ['INPUT', 'TEXTAREA', 'SELECT'].includes(tag);
}
