// Loads Leaflet from CDN once for all map modules (location-picker, polygon-drawer, triangle-map).
let loading = null;

export function loadLeaflet() {
	if (window.L) {
		return Promise.resolve();
	}

	loading ??= new Promise((resolve, reject) => {
		const link = document.createElement('link');
		link.rel = 'stylesheet';
		link.href = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css';
		document.head.appendChild(link);

		const script = document.createElement('script');
		script.src = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.js';
		script.onload = () => resolve();
		script.onerror = () => {
			loading = null;
			reject(new Error('Failed to load Leaflet'));
		};
		document.head.appendChild(script);
	});
	return loading;
}
