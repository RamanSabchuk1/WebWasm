// IndexedDB history of push notifications, shared by the page and both service workers.
self.notificationStore = {
	open: function () {
		return new Promise((resolve, reject) => {
			const req = indexedDB.open('webwasm-db', 1);
			req.onupgradeneeded = (e) => {
				const db = e.target.result;
				if (!db.objectStoreNames.contains('notifications')) {
					db.createObjectStore('notifications', { keyPath: 'id', autoIncrement: true });
				}
			};
			req.onsuccess = () => resolve(req.result);
			req.onerror = () => reject(req.error);
		});
	},

	getAll: async function () {
		try {
			const db = await this.open();
			return await new Promise((resolve) => {
				const req = db.transaction(['notifications'], 'readonly').objectStore('notifications').getAll();
				req.onsuccess = () => resolve(req.result.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp)));
				req.onerror = () => resolve([]);
			});
		} catch {
			return [];
		}
	},

	// Keeps the last 40 once there are more than 50.
	save: async function (payload) {
		try {
			const db = await this.open();
			const store = db.transaction('notifications', 'readwrite').objectStore('notifications');
			store.add({
				title: payload.notification?.title || payload.data?.title || 'Notification',
				body: payload.notification?.body || payload.data?.body || '',
				data: payload.data || null,
				timestamp: new Date().toISOString(),
				isRead: false
			});

			const countReq = store.count();
			countReq.onsuccess = () => {
				if (countReq.result > 50) {
					const keysReq = store.getAllKeys();
					keysReq.onsuccess = () => {
						const keys = keysReq.result;
						for (let i = 0; i < keys.length - 40; i++) {
							store.delete(keys[i]);
						}
					};
				}
			};
		} catch (e) {
			console.error('Save notification failed', e);
		}
	}
};
