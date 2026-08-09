window.getElementCoordinates = (element) => {
	const rect = element.getBoundingClientRect();
	return {
		top: rect.top,
		left: rect.left,
		bottom: rect.bottom,
		right: rect.right,
		height: rect.height,
		width: rect.width
	};
};

// window.dropdownPositioner = {
//     activeTrackers: new Map(),

//     initPositioning: (btnElement, menuElement) => {
//         if (!btnElement || !menuElement) return;

//         const updatePosition = () => {
//             if (!document.body.contains(btnElement) || !document.body.contains(menuElement)) {
//                 window.dropdownPositioner.destroyPositioning(btnElement);
//                 return;
//             }

//             const rect = btnElement.getBoundingClientRect();
//             const windowHeight = window.innerHeight;
//             const menuHeight = 310;
//             const menuWidth = 210;

//             const spaceBelow = windowHeight - rect.bottom;
//             const left = rect.left - (menuWidth - rect.width);
//             let top = 0;

//             if (spaceBelow < menuHeight && rect.top > menuHeight) {
//                 top = rect.top - menuHeight - 5;
//                 menuElement.classList.add('drop-up');
//                 menuElement.classList.remove('drop-down');
//             } else {
//                 top = rect.bottom + 5;
//                 menuElement.classList.add('drop-down');
//                 menuElement.classList.remove('drop-up');
//             }

//             menuElement.style.position = 'fixed';
//             menuElement.style.top = `${top}px`;
//             menuElement.style.left = `${left}px`;
//         };

//         updatePosition();
//         const scrollHandler = () => {
//             window.requestAnimationFrame(updatePosition);
//         };

//         window.addEventListener('scroll', scrollHandler, true);

//         window.dropdownPositioner.activeTrackers.set(btnElement, scrollHandler);
//     },

//     destroyPositioning: (btnElement) => {
//         const handler = window.dropdownPositioner.activeTrackers.get(btnElement);
//         if (handler) {
//             window.removeEventListener('scroll', handler, true);
//             window.dropdownPositioner.activeTrackers.delete(btnElement);
//         }
//     }
// };

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
