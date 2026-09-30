import { loadLeaflet } from './leaflet-loader.js';

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
