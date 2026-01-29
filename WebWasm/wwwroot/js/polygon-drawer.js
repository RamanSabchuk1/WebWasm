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

export async function initDrawingMap(mapElement, initialPoints, dotnetHelper) {
	await loadLeaflet();

	const L = window.L;
	
	// Calculate bounds
	let bounds = null;
	let minLat = 90, maxLat = -90, minLng = 180, maxLng = -180;

	if (initialPoints.length > 0) {
		initialPoints.forEach(point => {
			minLat = Math.min(minLat, point.lat);
			maxLat = Math.max(maxLat, point.lat);
			minLng = Math.min(minLng, point.lng);
			maxLng = Math.max(maxLng, point.lng);
		});
		bounds = [[minLat, minLng], [maxLat, maxLng]];
	}

	// Create map
	const map = L.map(mapElement);
	
	if (bounds) {
		map.fitBounds(bounds);
	} else {
		map.setView([51.505, -0.09], 13); // Default center
	}

	// Add tile layer
	L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
		attribution: '© OpenStreetMap contributors',
		maxZoom: 19,
		minZoom: 2
	}).addTo(map);

	// State management
	const state = {
		points: [],
		markers: [],
		polyline: null,
		polygon: null,
		polygonLayer: null
	};

	// Initialize with existing points
	if (initialPoints.length > 0) {
		initialPoints.forEach((point, index) => {
			state.points.push({ lat: point.lat, lng: point.lng });
			addMarker(point.lat, point.lng, index);
		});
		updatePolygon();
	}

	function addMarker(lat, lng, index) {
		const marker = L.circleMarker([lat, lng], {
			radius: 6,
			fillColor: '#8B7355',
			color: '#6D5A44',
			weight: 2,
			opacity: 1,
			fillOpacity: 0.8
		}).addTo(map);

		// Add tooltip
		marker.bindTooltip(`${lat.toFixed(6)}, ${lng.toFixed(6)}`, {
			permanent: false,
			direction: 'top',
			offset: [0, -10]
		});

		// Make marker draggable
		let isDragging = false;
		marker.on('mousedown', () => {
			isDragging = true;
			map.dragging.disable();
		});

		map.on('mousemove', (e) => {
			if (isDragging && index < state.points.length) {
				state.points[index] = { lat: e.latlng.lat, lng: e.latlng.lng };
				marker.setLatLng(e.latlng);
				updatePolygon();
				marker.bindTooltip(`${e.latlng.lat.toFixed(6)}, ${e.latlng.lng.toFixed(6)}`);
				dotnetHelper.invokeMethodAsync('OnPointDrag', index, e.latlng.lat, e.latlng.lng);
			}
		});

		map.on('mouseup', () => {
			isDragging = false;
			map.dragging.enable();
		});

		state.markers.push(marker);
	}

	function updatePolygon() {
		// Clear existing polygon
		if (state.polygon) {
			map.removeLayer(state.polygon);
		}
		if (state.polyline) {
			map.removeLayer(state.polyline);
		}

		if (state.points.length < 2) return;

		// Draw polyline
		const polylinePoints = state.points.map(p => [p.lat, p.lng]);
		state.polyline = L.polyline(polylinePoints, {
			color: '#8B7355',
			weight: 2,
			opacity: 0.7,
			dashArray: '5, 5'
		}).addTo(map);

		// Draw polygon if 3+ points
		if (state.points.length >= 3) {
			state.polygon = L.polygon(polylinePoints, {
				color: '#8B7355',
				weight: 2,
				opacity: 0.8,
				fillOpacity: 0.2,
				fillColor: '#8B7355'
			}).addTo(map);

			// Fit map to polygon
			map.fitBounds(state.polygon.getBounds());
		}
	}

	// Map click handler
	map.on('click', (e) => {
		const lat = e.latlng.lat;
		const lng = e.latlng.lng;
		
		state.points.push({ lat, lng });
		addMarker(lat, lng, state.points.length - 1);
		updatePolygon();
		
		dotnetHelper.invokeMethodAsync('OnMapClick', lat, lng);
	});

	// Return map instance
	return {
		redrawPolygon: async (points) => {
			state.points = points.map(p => ({ lat: p.lat, lng: p.lng }));
			
			// Update markers
			state.markers.forEach(m => map.removeLayer(m));
			state.markers = [];
			
			state.points.forEach((point, index) => {
				addMarker(point.lat, point.lng, index);
			});
			
			updatePolygon();
		},
		dispose: () => {
			if (map) {
				map.off();
				map.remove();
			}
		}
	};
}
