// Loads Leaflet from CDN once for all map modules (location-picker, polygon-drawer, triangle-map).
const base = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/';
let loading = null;

export function loadLeaflet() {
	// Waits for the stylesheet too: a map laid out before it gets wrong tile sizes.
	loading ??= Promise.all([
		add('link', { rel: 'stylesheet', href: base + 'leaflet.min.css' }),
		add('script', { src: base + 'leaflet.min.js' }),
	]).then(() => {
		// Leaflet guesses the marker image folder from its CSS and falls back to the site root (404) when it guesses
		// before the stylesheet is applied.
		L.Icon.Default.imagePath = base + 'images/';
	}, error => {
		loading = null;
		throw error;
	});
	return loading;
}

function add(tag, attributes) {
	return new Promise((resolve, reject) => {
		const element = Object.assign(document.createElement(tag), attributes);
		element.onload = resolve;
		element.onerror = () => reject(new Error('Failed to load Leaflet'));
		document.head.appendChild(element);
	});
}
