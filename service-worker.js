const CACHE_NAME = "mes-cache-v1";
const ASSETS_TO_CACHE = [
    "/",
    "/css/site.css",
    "/js/site.js",
    "/favicon.ico"
];

// Install SW
self.addEventListener("install", e => {
    e.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(ASSETS_TO_CACHE))
    );
    self.skipWaiting();
});

// Fetch cached assets
self.addEventListener("fetch", e => {
    e.respondWith(
        fetch(e.request)
            .catch(() => caches.match(e.request))
    );
});
