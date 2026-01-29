// Load Leaflet from CDN
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

export async function initMap(mapElement, triangles, color) {
	await loadLeaflet();

	const L = window.L;

	// Calculate bounds from all triangle points
	let minLat = 90, maxLat = -90, minLng = 180, maxLng = -180;

	triangles.forEach(triangle => {
		[triangle.point1, triangle.point2, triangle.point3].forEach(point => {
			minLat = Math.min(minLat, point.lat);
			maxLat = Math.max(maxLat, point.lat);
			minLng = Math.min(minLng, point.lng);
			maxLng = Math.max(maxLng, point.lng);
		});
	});

	// Create map
	const map = L.map(mapElement).fitBounds([
		[minLat, minLng],
		[maxLat, maxLng]
	]);

	// Add tile layer
	L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
		attribution: '© OpenStreetMap contributors',
		maxZoom: 19
	}).addTo(map);

	// Draw triangles
	triangles.forEach(triangle => {
		const polygon = L.polygon([
			[triangle.point1.lat, triangle.point1.lng],
			[triangle.point2.lat, triangle.point2.lng],
			[triangle.point3.lat, triangle.point3.lng]
		], {
			color: color,
			weight: 2,
			opacity: 0.8,
			fillOpacity: 0.3,
			fillColor: color
		}).addTo(map);

		// Add popup on hover
		polygon.bindPopup('Triangle', { closeButton: false });
		polygon.on('mouseover', () => polygon.openPopup());
		polygon.on('mouseout', () => polygon.closePopup());
	});

	// Return map instance
	return {
		dispose: () => {
			if (map) {
				map.off();
				map.remove();
			}
		}
	};
}
