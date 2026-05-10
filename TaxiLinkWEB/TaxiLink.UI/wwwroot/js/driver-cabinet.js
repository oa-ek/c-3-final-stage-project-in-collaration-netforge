let selectedRating = 5;
let carMarker = null;
let routePolyline = null;
let animationInterval = null;
let routePoints = [];

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

document.addEventListener("DOMContentLoaded", function () {
    const avatarBtn = document.getElementById('avatarUploadBtn');
    const avatarInput = document.getElementById('avatarInput');
    const avatarPreview = document.getElementById('avatarPreview');

    if (avatarBtn && avatarInput) {
        avatarBtn.style.cursor = "pointer";
        avatarBtn.onclick = function (e) {
            e.preventDefault();
            avatarInput.click();
        };
        avatarInput.onchange = function () {
            if (this.files && this.files[0]) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    avatarPreview.src = e.target.result;
                };
                reader.readAsDataURL(this.files[0]);
            }
        };
    }

    const stars = document.querySelectorAll('#rating-stars i');
    stars.forEach(star => {
        star.onclick = function () {
            selectedRating = this.dataset.val;
            stars.forEach((s, idx) => {
                s.className = idx < selectedRating ? 'fa-solid fa-star' : 'fa-regular fa-star';
            });
        };
    });

    const mapElement = document.getElementById('map');
    if (mapElement) {
        setTimeout(() => {
            const isUnverified = document.getElementById('unverified-overlay') !== null;
            const isWorkPage = document.querySelector('.work-wrapper') !== null;

            window.driverMap = L.map('map', {
                zoomControl: !isUnverified,
                dragging: !isUnverified,
                scrollWheelZoom: !isUnverified,
                fadeAnimation: false,
            }).setView([50.4501, 30.5234], 13);

            L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
                attribution: '&copy; OpenStreetMap'
            }).addTo(window.driverMap);

            if (isWorkPage) {
                const p = document.getElementById('pickup-addr').innerText.replace(/"/g, '').trim();
                const d = document.getElementById('dropoff-addr').innerText.replace(/"/g, '').trim();

                const cancelBtn = document.querySelector('button[data-bs-target="#cancelModal"]');
                if (cancelBtn) {
                    const orderIdInput = document.getElementById('currentOrderId');
                    const extId = orderIdInput ? orderIdInput.value : null;
                    if (extId) {
                        setupRouteOnMap(window.driverMap, p, d, extId);
                    }
                }
            }
            else if (!isUnverified) {
                var carIcon = L.divIcon({
                    className: 'custom-car-marker-container-static',
                    html: carSvg,
                    iconSize: [26, 52],
                    iconAnchor: [13, 26]
                });
                L.marker([50.4501, 30.5234], { icon: carIcon }).addTo(window.driverMap);

                fetch('/Driver/Dashboard/GetCurrentWeather')
                    .then(res => res.json())
                    .then(wData => {
                        if (wData.success) {
                            const wWidget = document.getElementById('index-weather-widget');
                            if (wWidget) {
                                let icon = "☀️";
                                if (wData.condition.includes("Дощ") || wData.condition.includes("Мряка")) icon = "🌧️";
                                if (wData.condition.includes("Сніг")) icon = "❄️";
                                if (wData.condition.includes("Гроза")) icon = "⚡";

                                document.getElementById('index-weather-icon').innerText = icon;
                                document.getElementById('index-weather-text').innerText = wData.condition;
                                document.getElementById('index-weather-coef').innerText = `Коефіцієнт: x${wData.multiplier.toFixed(1)}`;
                                wWidget.style.display = "flex";
                            }
                        }
                    });
            }
            window.driverMap.invalidateSize();
        }, 300);
    }

    const sidebarToggleBtn = document.getElementById('sidebar-toggle-btn');
    if (sidebarToggleBtn) {
        sidebarToggleBtn.onclick = function () {
            const sidebar = document.querySelector('.driver-info-sidebar');
            sidebar.classList.toggle('collapsed');

            const icon = this.querySelector('i');
            if (sidebar.classList.contains('collapsed')) {
                icon.classList.remove('fa-chevron-left');
                icon.classList.add('fa-chevron-right');
            } else {
                icon.classList.remove('fa-chevron-right');
                icon.classList.add('fa-chevron-left');
            }

            if (window.driverMap) {
                setTimeout(() => {
                    window.driverMap.invalidateSize();
                }, 400);
            }
        };
    }

    const startBtn = document.getElementById('btn-start');
    if (startBtn) {
        startBtn.addEventListener('click', function (e) {
            e.preventDefault();
            const currentOrderId = this.getAttribute('data-order-id') || document.getElementById('currentOrderId').value;
            startRideAction(currentOrderId);
        });
    }
});

async function setupRouteOnMap(map, pickup, dropoff, orderId) {
    try {
        const response = await fetch(`/Driver/Dashboard/GetRouteData?orderId=${orderId}`);
        const data = await response.json();

        if (data.success) {
            document.getElementById('arrival-time').innerText = `~ ${data.duration} хв`;
            const usdEl = document.getElementById('usd-price');
            if (usdEl) usdEl.innerText = `(≈ $${data.priceUsd})`;

            const weatherAlert = document.getElementById('weather-alert');
            if (weatherAlert && data.weatherCondition) {
                let icon = "☀️";
                let colorClass = "text-success";
                let prefix = "Сприятливі умови:";

                if (data.weatherCondition.includes("Дощ") || data.weatherCondition.includes("Мряка")) {
                    icon = "🌧️";
                    colorClass = "text-warning";
                    prefix = "Ускладнені умови:";
                } else if (data.weatherCondition.includes("Сніг")) {
                    icon = "❄️";
                    colorClass = "text-info";
                    prefix = "Ускладнені умови:";
                } else if (data.weatherCondition.includes("Гроза")) {
                    icon = "⚡";
                    colorClass = "text-danger";
                    prefix = "Складні умови:";
                }

                weatherAlert.className = `small fw-bold mt-1 ${colorClass}`;
                weatherAlert.innerText = `${icon} ${prefix} ${data.weatherCondition} (Коеф: x${data.weatherMultiplier.toFixed(1)})`;
                weatherAlert.style.display = "block";
            } else if (weatherAlert) {
                weatherAlert.style.display = "none";
            }

            routePoints = data.routeCoordinates.map(coord => [coord[1], coord[0]]);
            const pCoords = routePoints[0];
            const dCoords = routePoints[routePoints.length - 1];

            routePolyline = L.polyline(routePoints, { color: '#facc15', weight: 6, opacity: 0.8 }).addTo(map);

            map.fitBounds(routePolyline.getBounds(), { paddingLeft: [380, 50], padding: [50, 50] });

            L.marker(pCoords, { icon: L.divIcon({ className: '', html: '<div style="background:#10b981; width:16px; height:16px; border-radius:50%; border:2px solid white;"></div>' }) }).addTo(map);
            L.marker(dCoords, { icon: L.divIcon({ className: '', html: '<div style="background:#ef4444; width:16px; height:16px; border-radius:50%; border:2px solid white;"></div>' }) }).addTo(map);

            let initialAngle = 0;
            if (routePoints.length > 1) {
                let dy = routePoints[1][0] - routePoints[0][0];
                let dx = routePoints[1][1] - routePoints[0][1];
                initialAngle = Math.atan2(dx, dy) * 180 / Math.PI;
            }

            var carIcon = L.divIcon({
                className: 'custom-car-marker-container',
                html: carSvg,
                iconSize: [32, 64],
                iconAnchor: [16, 32]
            });
            carMarker = L.marker(pCoords, { icon: carIcon, zIndexOffset: 1000 }).addTo(map);

            let img = carMarker.getElement().querySelector('svg');
            if (img) img.style.transform = `rotate(${initialAngle}deg)`;

            const statusPill = document.querySelector('.top-status-pill');
            if (statusPill && statusPill.innerText.includes('Виконується')) {
                const startBtn = document.getElementById('btn-start');
                if (startBtn) startBtn.style.display = 'none';
                startCarAnimation();
            }
        } else {
            document.getElementById('arrival-time').innerText = "Час невідомий";
        }
    } catch (e) { console.error("Помилка маршруту:", e); }
}

function startRideAction(id) {
    fetch(`/Driver/Dashboard/StartRide?orderId=${id}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                const startBtn = document.getElementById('btn-start');
                if (startBtn) startBtn.style.display = 'none';

                const statusPill = document.querySelector('.top-status-pill');
                if (statusPill) statusPill.innerText = 'Виконується (в русі)';

                startCarAnimation();
            }
        });
}

function startCarAnimation() {
    if (!carMarker || routePoints.length < 2) return;

    let totalDist = 0;
    let segments = [];
    for (let i = 0; i < routePoints.length - 1; i++) {
        let d = window.driverMap.distance(routePoints[i], routePoints[i + 1]);
        totalDist += d;
        segments.push({ p1: routePoints[i], p2: routePoints[i + 1], dist: d });
    }

    let startTime = null;
    const duration = 25000;

    function animate(timestamp) {
        if (!startTime) startTime = timestamp;
        let progress = (timestamp - startTime) / duration;
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

            window.driverMap.setView(currentPos, window.driverMap.getZoom(), { animate: false });
        }

        if (progress < 1) {
            animationInterval = requestAnimationFrame(animate);
        } else {
            const statusPill = document.querySelector('.top-status-pill');
            if (statusPill) statusPill.innerText = 'Прибули на місце';

            const cancelBtn = document.getElementById('btn-cancel');
            if (cancelBtn) cancelBtn.style.display = 'none';

            const finishBtn = document.getElementById('btn-finish');
            if (finishBtn) finishBtn.style.display = 'block';

            const sidebar = document.querySelector('.driver-info-sidebar');
            if (sidebar && sidebar.classList.contains('collapsed')) {
                sidebar.classList.remove('collapsed');
                const icon = document.getElementById('sidebar-toggle-btn').querySelector('i');
                if (icon) {
                    icon.classList.remove('fa-chevron-right');
                    icon.classList.add('fa-chevron-left');
                }
                setTimeout(() => window.driverMap.invalidateSize(), 400);
            }
        }
    }

    animationInterval = requestAnimationFrame(animate);
}

function updateRideStatus(action, id) {
    fetch(`/Driver/Dashboard/${action}?orderId=${id}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                if (action === 'CompleteRide') {
                    window.location.href = '/Driver/Dashboard/Wallet';
                } else {
                    location.reload();
                }
            }
        });
}

function submitCancel(id) {
    if (animationInterval) cancelAnimationFrame(animationInterval);

    const reason = document.getElementById('cancelReasonId').value;
    fetch(`/Driver/Dashboard/CancelOrder?orderId=${id}&reasonId=${reason}`, { method: 'POST' })
        .then(() => window.location.href = '/Driver/Dashboard/Index');
}

function submitFinish(id) {
    fetch(`/Driver/Dashboard/FinishOrder?orderId=${id}&rating=${selectedRating}`, { method: 'POST' })
        .then(() => window.location.href = '/Driver/Dashboard/Wallet');
}

function acceptOrder(id) {
    fetch('/Driver/Dashboard/AcceptOrder?orderId=' + id, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                window.location.href = '/Driver/Dashboard/Work';
            } else {
                alert(data.message);
            }
        });
}

function switchTab(tabId) {
    const navLinks = document.querySelectorAll('.nav-tabs-custom .nav-link');
    navLinks.forEach(link => link.classList.remove('active'));
    if (event && event.currentTarget) event.currentTarget.classList.add('active');

    const panes = document.querySelectorAll('.tab-pane');
    panes.forEach(pane => {
        pane.classList.remove('d-block');
        pane.classList.add('d-none');
    });

    const targetPane = document.getElementById('tab-' + tabId);
    if (targetPane) {
        targetPane.classList.remove('d-none');
        targetPane.classList.add('d-block');
    }
}

function toggleStatus(checkbox) {
    const statusText = document.getElementById('status-text');
    const isChecked = checkbox.checked;
    if (statusText) statusText.innerText = isChecked ? "Працюю" : "Перерва";

    fetch('/Driver/Dashboard/ToggleWorkingMode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
        body: JSON.stringify({ status: isChecked })
    }).then(res => res.json()).then(data => {
        if (!data.success) {
            checkbox.checked = !isChecked;
            if (statusText) statusText.innerText = checkbox.checked ? "Працюю" : "Перерва";
        }
    }).catch(() => {
        checkbox.checked = !isChecked;
        if (statusText) statusText.innerText = checkbox.checked ? "Працюю" : "Перерва";
    });
}

function toggleService(btn, id) {
    btn.classList.toggle('selected');
    const icon = btn.querySelector('i');
    if (btn.classList.contains('selected')) {
        icon.className = 'fa-solid fa-xmark';
        const input = document.createElement('input');
        input.type = 'hidden'; input.name = 'SelectedServiceIds'; input.value = id; input.id = 'srv-input-' + id;
        document.getElementById('services-inputs').appendChild(input);
    } else {
        icon.className = 'fa-solid fa-plus';
        const el = document.getElementById('srv-input-' + id);
        if (el) el.remove();
    }
}

function toggleVehicleClass(btn, id) {
    btn.classList.toggle('selected');
    const icon = btn.querySelector('i');
    if (btn.classList.contains('selected')) {
        icon.className = 'fa-solid fa-xmark';
        const input = document.createElement('input');
        input.type = 'hidden'; input.name = 'SelectedVehicleClassIds'; input.value = id; input.id = 'vclass-input-' + id;
        document.getElementById('vclass-inputs').appendChild(input);
    } else {
        icon.className = 'fa-solid fa-plus';
        const el = document.getElementById('vclass-input-' + id);
        if (el) el.remove();
    }
}

function setFop(isActive) {
    document.getElementById('IsFopActiveHidden').value = isActive;
    const btns = document.querySelectorAll('.fop-btn');
    if (btns.length >= 2) {
        btns[0].className = 'fop-btn ' + (isActive ? 'active-yes' : '');
        btns[1].className = 'fop-btn ' + (!isActive ? 'active-no' : '');
    }
}

function checkWithdrawal(balance) {
    if (balance <= 0) alert("На вашому рахунку недостатньо коштів для виведення. Мінімальна сума: 1.00 ₴");
    else alert("Запит на виведення " + balance.toFixed(2) + " ₴ відправлено в обробку. Очікуйте зарахування протягом 24 годин.");
}

function processRefill() {
    const amountInput = document.getElementById('refillAmount');
    const amount = parseFloat(amountInput.value);
    if (!amount || amount <= 0) { alert("Будь ласка, введіть суму для поповнення."); return; }

    const btn = document.getElementById('confirmRefillBtn');
    btn.disabled = true; btn.innerText = "Обробка...";

    fetch('/Driver/Dashboard/RefillWallet', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'amount=' + amount
    }).then(res => res.json()).then(data => {
        if (data.success) {
            document.getElementById('current-balance-display').innerText = data.newBalance + " ₴";
            bootstrap.Modal.getInstance(document.getElementById('refillModal')).hide();
            alert("Оплата успішна! Баланс поповнено.");
            location.reload();
        } else {
            alert("Помилка поповнення.");
            btn.disabled = false; btn.innerText = "Підтвердити оплату";
        }
    }).catch(() => {
        btn.disabled = false; btn.innerText = "Підтвердити оплату";
    });
}

function processWithdrawal() {
    if (!confirm("Ви впевнені, що хочете вивести всі кошти на вашу основну карту?")) return;
    fetch('/Driver/Dashboard/WithdrawWallet', { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                document.getElementById('current-balance-display').innerText = "0,00 ₴";
                alert("Кошти успішно виведені! Зарахування очікуйте протягом дня.");
                location.reload();
            } else alert(data.message || "Помилка при виведенні коштів.");
        });
}