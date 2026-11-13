// Development service worker: no offline cache, only push. Published version: service-worker.published.js.
importScripts('./js/notification-store.js', './js/push-sw.js');

self.addEventListener('fetch', () => { });
