const carSvg = `
<svg viewBox="0 0 100 200" width="32" height="64" xmlns="http://www.w3.org/2000/svg" style="filter: drop-shadow(0px 0px 15px rgba(250, 204, 21, 0.7)); transition: transform 0.1s linear;">
  <rect x="10" y="20" width="80" height="160" rx="25" fill="#FFCC00" />
  <rect x="25" y="10" width="50" height="180" rx="15" fill="#FFCC00" />
  <path d="M30 50 Q 50 40 70 50 L 75 100 Q 50 90 25 100 Z" fill="#1e293b" />
  <rect x="20" y="105" width="10" height="50" rx="5" fill="#1e293b" />
  <rect x="70" y="105" width="10" height="50" rx="5" fill="#1e293b" />
  <path d="M30 160 Q 50 170 70 160 L 72 150 Q 50 155 28 150 Z" fill="#1e293b" />
  <rect x="5" y="70" width="10" height="20" rx="5" fill="#FFCC00" />
  <rect x="85" y="70" width="10" height="20" rx="5" fill="#FFCC00" />
  <rect x="35" y="30" width="10" height="10" fill="black" />
  <rect x="45" y="30" width="10" height="10" fill="white" />
  <rect x="55" y="30" width="10" height="10" fill="black" />
  <rect x="35" y="40" width="10" height="10" fill="white" />
  <rect x="45" y="40" width="10" height="10" fill="black" />
  <rect x="55" y="40" width="10" height="10" fill="white" />
  <rect x="25" y="175" width="15" height="10" rx="3" fill="#ef4444" />
  <rect x="60" y="175" width="15" height="10" rx="3" fill="#ef4444" />
</svg>
`;

document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebar-overlay');

    function toggleSidebar() {
        if (sidebar && overlay) {
            sidebar.classList.toggle('closed');
            overlay.classList.toggle('d-none');
        }
    }

    document.getElementById('menu-toggle')?.addEventListener('click', toggleSidebar);
    document.getElementById('menu-close')?.addEventListener('click', toggleSidebar);
    overlay?.addEventListener('click', toggleSidebar);

    const mapElement = document.getElementById('map');
    if (!mapElement) return;

    let map = L.map('map', { zoomControl: false }).setView([50.45, 30.52], 13);
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png').addTo(map);

    let markerA = null, markerB = null, routeLine = null, carMarker = null;
    let orderState = { distance: 0, classId: null, basePrice: 0, perKm: 0, routeCoords: [], durationMin: 0 };

    let cityMultiplierElement = document.getElementById('cityMultiplier');
    let usdRateElement = document.getElementById('usdRate');
    const multiplier = cityMultiplierElement ? parseFloat(cityMultiplierElement.value) : 1.0;
    const usdRate = usdRateElement ? parseFloat(usdRateElement.value) : 40.0;

    let currentOrderId = null;
    let currentStatusId = 1;
    let pollInterval = null;
    let isAnimationStarted = false;

    map.on('click', async function (e) {
        if (!markerA || (markerA && markerB)) {
            if (markerA) map.removeLayer(markerA);
            if (markerB) map.removeLayer(markerB);
            if (routeLine) map.removeLayer(routeLine);
            if (carMarker) map.removeLayer(carMarker);
            markerB = null;

            markerA = L.marker(e.latlng, { icon: createDot('#10b981') }).addTo(map);
            document.getElementById('pickup').value = await getAddress(e.latlng.lat, e.latlng.lng);
        } else if (!markerB) {
            markerB = L.marker(e.latlng, { icon: createDot('#ef4444') }).addTo(map);
            let dropoffStr = await getAddress(e.latlng.lat, e.latlng.lng);
            document.getElementById('dropoff').value = dropoffStr;

            document.getElementById('ui-pickup-text').innerText = document.getElementById('pickup').value;
            document.getElementById('ui-dropoff-text').innerText = dropoffStr;
            document.getElementById('search-pickup').innerText = document.getElementById('pickup').value;
            document.getElementById('search-dropoff').innerText = dropoffStr;

            drawRouteAndShowPanel();
        }
    });

    function createDot(color) {
        return L.divIcon({ className: '', html: `<div style="background:${color}; width:16px; height:16px; border-radius:50%; box-shadow: 0 0 10px ${color};"></div>` });
    }

    async function getAddress(lat, lng) {
        try {
            let res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`);
            let data = await res.json();
            return data.address.road ? `${data.address.road} ${data.address.house_number || ''}`.trim() : data.display_name.split(',')[0];
        } catch { return `${lat.toFixed(4)}, ${lng.toFixed(4)}`; }
    }

    async function drawRouteAndShowPanel() {
        let p1 = markerA.getLatLng(), p2 = markerB.getLatLng();
        let res = await fetch(`/Client/Dashboard/GetRouteFromCoords?startLat=${p1.lat}&startLon=${p1.lng}&endLat=${p2.lat}&endLon=${p2.lng}`);
        let data = await res.json();

        if (data.success) {
            orderState.distance = data.distance;
            orderState.routeCoords = data.coordinates.map(c => [c[1], c[0]]);
            orderState.durationMin = data.duration || Math.ceil(data.distance * 2);

            routeLine = L.polyline(orderState.routeCoords, { color: '#facc15', weight: 4 }).addTo(map);
            map.fitBounds(routeLine.getBounds(), { padding: [50, 50] });

            document.getElementById('panel-search').classList.add('d-none');
            document.getElementById('panel-order').classList.remove('d-none');

            let firstClass = document.querySelector('.class-card');
            if (firstClass) firstClass.click();
        }
    }

    document.querySelectorAll('.class-card').forEach(card => {
        card.addEventListener('click', () => {
            document.querySelectorAll('.class-card').forEach(c => c.classList.remove('active'));
            card.classList.add('active');

            orderState.classId = card.getAttribute('data-id');
            orderState.basePrice = parseFloat(card.getAttribute('data-base').replace(',', '.'));
            orderState.perKm = parseFloat(card.getAttribute('data-perkm').replace(',', '.'));
            document.getElementById('ui-class-name').innerText = card.querySelector('.fw-bold').innerText;
            calculateTotal();
        });
    });

    document.querySelectorAll('.srv-checkbox').forEach(cb => cb.addEventListener('change', calculateTotal));

    function calculateTotal() {
        if (!orderState.classId) return;
        let price = (orderState.basePrice + (orderState.distance * orderState.perKm)) * multiplier;
        document.querySelectorAll('.srv-checkbox:checked').forEach(cb => price += parseFloat(cb.getAttribute('data-price')));

        let finalPrice = Math.round(price);
        let usdPrice = (finalPrice / usdRate).toFixed(2);

        document.getElementById('ui-total-price').innerText = finalPrice;
        document.getElementById('search-price').innerText = finalPrice;
        document.getElementById('search-price-usd').innerText = usdPrice;
        document.getElementById('review-price').innerText = finalPrice;
    }

    document.getElementById('btn-options')?.addEventListener('click', () => {
        document.getElementById('panel-order').classList.add('d-none');
        document.getElementById('panel-options').classList.remove('d-none');
    });

    document.getElementById('btn-apply-options')?.addEventListener('click', () => {
        document.getElementById('panel-options').classList.add('d-none');
        document.getElementById('panel-order').classList.remove('d-none');
    });

    document.getElementById('btn-close-options')?.addEventListener('click', () => {
        document.getElementById('panel-options').classList.add('d-none');
        document.getElementById('panel-order').classList.remove('d-none');
    });

    document.getElementById('btn-back')?.addEventListener('click', () => {
        document.getElementById('panel-order').classList.add('d-none');
        document.getElementById('panel-search').classList.remove('d-none');
    });

    document.getElementById('btn-cancel-search')?.addEventListener('click', async () => {
        clearInterval(pollInterval);
        if (currentOrderId) {
            await fetch(`/Client/Dashboard/CancelOrder?orderId=${currentOrderId}`, { method: 'POST' });
        }
        document.getElementById('panel-searching').classList.add('d-none');
        document.getElementById('panel-order').classList.remove('d-none');
    });

    document.getElementById('btn-order')?.addEventListener('click', async () => {
        let srvs = Array.from(document.querySelectorAll('.srv-checkbox:checked')).map(cb => parseInt(cb.value));
        let payload = {
            Pickup: document.getElementById('pickup').value,
            Dropoff: document.getElementById('dropoff').value,
            Distance: orderState.distance,
            VehicleClassId: parseInt(orderState.classId),
            Comment: document.getElementById('order-comment').value || "",

            FinalPrice: parseFloat(document.getElementById('ui-total-price').innerText),
            SelectedServices: srvs
        };

        let res = await fetch('/Client/Dashboard/CreateOrder', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
        let data = await res.json();

        if (data.success) {
            currentOrderId = data.orderId;
            currentStatusId = 1;
            document.getElementById('panel-order').classList.add('d-none');
            document.getElementById('panel-searching').classList.remove('d-none');

            pollInterval = setInterval(checkStatus, 3000);
        }
    });

    async function checkStatus() {
        if (!currentOrderId) return;
        let res = await fetch(`/Client/Dashboard/CheckOrderStatus?orderId=${currentOrderId}`);
        let data = await res.json();

        if (data.success) {
            if (data.statusId == 2 && currentStatusId !== 2) {
                currentStatusId = 2;
                document.getElementById('panel-searching').classList.add('d-none');
                document.getElementById('panel-active-ride').classList.remove('d-none');

                document.getElementById('ui-driver-name').innerText = data.driverName;
                document.getElementById('ui-driver-rating').innerText = data.driverRating;
                document.getElementById('ui-car-brand').innerText = data.carBrand + " " + data.carModel;
                document.getElementById('ui-car-color').innerText = data.carColor;
                document.getElementById('ui-car-plate').innerText = data.carPlate;
                document.getElementById('ui-status-text').innerText = "Очікуйте водія";
                document.getElementById('ui-eta').innerText = "~";

                let avatarContainer = document.getElementById('ui-driver-avatar-container');
                if (data.driverAvatar) {
                    avatarContainer.innerHTML = `<img src="${data.driverAvatar}" class="w-100 h-100" style="object-fit:cover;" />`;
                } else {
                    avatarContainer.innerHTML = `<i class="fa-solid fa-user-astronaut"></i>`;
                }
            }
            else if (data.statusId == 3 && currentStatusId !== 3) {
                currentStatusId = 3;
                document.getElementById('ui-status-text').innerText = "Виконується (в русі)";
                if (!isAnimationStarted) startRideSimulation();
            }
            else if ((data.statusId == 4 || data.statusId == 5) && currentStatusId !== 4) {
                currentStatusId = 4;
                clearInterval(pollInterval);
                document.getElementById('panel-active-ride').classList.add('d-none');
                document.getElementById('panel-review').classList.remove('d-none');
            }
        }
    }

    function startRideSimulation() {
        isAnimationStarted = true;

        var carIcon = L.divIcon({
            className: 'custom-car-marker-container',
            html: carSvg,
            iconSize: [32, 64],
            iconAnchor: [16, 32]
        });

        carMarker = L.marker(orderState.routeCoords[0], { icon: carIcon, zIndexOffset: 1000 }).addTo(map);

        let totalDist = 0;
        let segments = [];
        for (let i = 0; i < orderState.routeCoords.length - 1; i++) {
            let d = map.distance(orderState.routeCoords[i], orderState.routeCoords[i + 1]);
            totalDist += d;
            segments.push({ p1: orderState.routeCoords[i], p2: orderState.routeCoords[i + 1], dist: d });
        }

        let startTime = null;
        let durationMs = 25000;

        function animate(timestamp) {
            if (!startTime) startTime = timestamp;
            let progress = (timestamp - startTime) / durationMs;
            if (progress > 1) progress = 1;

            let targetDist = progress * totalDist;
            let currentDist = 0;
            let currentPos = null;
            let angle = 0;

            for (let i = 0; i < segments.length; i++) {
                if (currentDist + segments[i].dist >= targetDist || i === segments.length - 1) {
                    let segmentProgress = segments[i].dist > 0 ? (targetDist - currentDist) / segments[i].dist : 1;
                    if (segmentProgress > 1) segmentProgress = 1;

                    let lat = segments[i].p1[0] + (segments[i].p2[0] - segments[i].p1[0]) * segmentProgress;
                    let lng = segments[i].p1[1] + (segments[i].p2[1] - segments[i].p1[1]) * segmentProgress;
                    currentPos = [lat, lng];

                    let dy = segments[i].p2[0] - segments[i].p1[0];
                    let dx = segments[i].p2[1] - segments[i].p1[1];
                    angle = Math.atan2(dx, dy) * 180 / Math.PI;
                    break;
                }
                currentDist += segments[i].dist;
            }

            if (currentPos) {
                carMarker.setLatLng(currentPos);
                let img = carMarker.getElement().querySelector('svg');
                if (img) img.style.transform = `rotate(${angle}deg)`;

                map.setView(currentPos, map.getZoom(), { animate: false });
            }

            let remainingMin = Math.ceil(orderState.durationMin * (1 - progress));
            document.getElementById('ui-eta').innerText = remainingMin > 0 ? `${remainingMin} хв` : "Прибули!";

            if (progress < 1) requestAnimationFrame(animate);
        }
        requestAnimationFrame(animate);
    }

    document.querySelectorAll('.star-icon').forEach(star => {
        star.addEventListener('click', function () {
            let val = parseInt(this.getAttribute('data-val'));
            document.querySelectorAll('.star-icon').forEach((s, idx) => {
                if (idx < val) s.classList.add('active');
                else s.classList.remove('active');
            });
        });
    });
});