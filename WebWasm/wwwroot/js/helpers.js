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
