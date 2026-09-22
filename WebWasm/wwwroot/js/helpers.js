window.getElementCoordinates = (element) => {
	if (!element) return null;
	const rect = element.getBoundingClientRect();
	return {
		top: rect.top,
		left: rect.left,
		bottom: rect.bottom,
		right: rect.right,
		height: rect.height,
		width: rect.width,
		windowHeight: window.innerHeight
	};
};

window.getElementHeight = (element) => {
	if (!element) return null;
	return element.getBoundingClientRect().height;
};

// S5: Enter в модалке = submit. Но Enter внутри textarea — это перевод строки,
// а внутри <select> и у нативных кнопок/ссылок — их собственное действие.
// Blazor KeyboardEventArgs не отдаёт target, поэтому спрашиваем DOM напрямую.
window.isEnterSubmitBlocked = () => {
	const el = document.activeElement;
	if (!el) return false;
	const tag = el.tagName ? el.tagName.toLowerCase() : '';
	if (tag === 'textarea' || tag === 'select' || tag === 'button' || tag === 'a') return true;
	if (el.isContentEditable) return true;
	// Открытый список автокомплита/datalist сам обрабатывает Enter.
	return el.getAttribute && el.getAttribute('aria-expanded') === 'true';
};

window.clickOutside = {
	register: function (element, dotnetHelper) {
		element.clickOutsideHandler = function (event) {
			if (!element.contains(event.target)) {
				dotnetHelper.invokeMethodAsync('InvokeClickOutside');
			}
		};
		document.addEventListener('click', element.clickOutsideHandler);
	},
	unregister: function (element) {
		if (element.clickOutsideHandler) {
			document.removeEventListener('click', element.clickOutsideHandler);
			delete element.clickOutsideHandler;
		}
	}
};
