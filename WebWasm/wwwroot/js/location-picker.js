const loadLeaflet = () => {
return new Promise((resolve, reject) => {
		if (window.L) {
			resolve();
			return;
		}

		// Load CSS
		const link = document.createElement('link');
		link.rel = 'stylesheet';
		link.href = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css';
		document.head.appendChild(link);

		// Load JS
		const script = document.createElement('script');
		script.src = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.js';
		script.onload = () => resolve();
		script.onerror = () => reject(new Error('Failed to load Leaflet'));
		document.head.appendChild(script);
	});
};

export async function initLocationPicker(mapElement, initialLat, initialLng, dotNetHelper) {
	await loadLeaflet();

	const map = L.map(mapElement).setView([initialLat, initialLng], 13);

	L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
		attribution: '© OpenStreetMap contributors',
		maxZoom: 19
	}).addTo(map);

	const marker = L.marker([initialLat, initialLng], {
		draggable: true
	}).addTo(map);

	// Handle map clicks
	map.on('click', function (e) {
		const lat = e.latlng.lat;
		const lng = e.latlng.lng;
		marker.setLatLng([lat, lng]);
		dotNetHelper.invokeMethodAsync('OnMapClick', lat, lng);
	});

	// Handle marker drag
	marker.on('dragend', function (e) {
		const position = marker.getLatLng();
		dotNetHelper.invokeMethodAsync('OnMapClick', position.lat, position.lng);
	});

	return {
		updateMarker: function (lat, lng) {
			marker.setLatLng([lat, lng]);
			map.setView([lat, lng], map.getZoom());
		},
		dispose: function () {
			map.remove();
		}
	};
}
