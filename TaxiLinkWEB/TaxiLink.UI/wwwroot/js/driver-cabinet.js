let selectedRating = 5;

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

            var map = L.map('map', {
                zoomControl: !isUnverified,
                dragging: !isUnverified,
                scrollWheelZoom: !isUnverified
            }).setView([50.4501, 30.5234], 13);

            L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
                attribution: '&copy; OpenStreetMap'
            }).addTo(map);

            if (isWorkPage) {
                const p = document.getElementById('pickup-addr').innerText.replace(/"/g, '').trim();
                const d = document.getElementById('dropoff-addr').innerText.replace(/"/g, '').trim();
                setupRouteOnMap(map, p, d);
            }
            else if (!isUnverified) {
                var carIcon = L.divIcon({
                    className: 'custom-car-marker',
                    html: '<div style="width: 20px; height: 20px; background: #FFCC00; border-radius: 50%; border: 3px solid #1e293b; box-shadow: 0 0 15px rgba(255, 204, 0, 0.8);"></div>',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10]
                });
                L.marker([50.4501, 30.5234], { icon: carIcon }).addTo(map);
            }
            map.invalidateSize();
        }, 300);
    }
});

async function setupRouteOnMap(map, pickup, dropoff) {
    const geocode = async (addr) => {
        try {
            let res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(addr + ", Київ")}`);
            let data = await res.json();
            return data.length > 0 ? L.latLng(data[0].lat, data[0].lon) : null;
        } catch (e) { return null; }
    };

    const pCoords = await geocode(pickup);
    const dCoords = await geocode(dropoff);

    if (pCoords && dCoords) {
        L.Routing.control({
            waypoints: [pCoords, dCoords],
            lineOptions: { styles: [{ color: '#facc15', weight: 6, opacity: 0.8 }] },
            show: false,
            addWaypoints: false,
            createMarker: () => null 
        }).on('routesfound', function (e) {
            const summary = e.routes[0].summary;
            const timeInMinutes = Math.round((summary.totalDistance / 1000) * 3 + 2); 

            const timeEl = document.getElementById('arrival-time');
            if (timeEl) timeEl.innerText = `~ ${timeInMinutes} хв`;

            map.flyTo(pCoords, 15, { animate: true, duration: 1.5 });
        }).addTo(map);

        L.marker(pCoords, { icon: L.divIcon({ className: '', html: '<div style="background:#10b981; width:16px; height:16px; border-radius:50%; border:2px solid white;"></div>' }) }).addTo(map);
        L.marker(dCoords, { icon: L.divIcon({ className: '', html: '<div style="background:#ef4444; width:16px; height:16px; border-radius:50%; border:2px solid white;"></div>' }) }).addTo(map);
    }
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
    const reason = document.getElementById('cancelReasonId').value;
    fetch(`/Driver/Dashboard/CancelOrder?orderId=${id}&reasonId=${reason}`, { method: 'POST' })
        .then(() => window.location.href = '/Driver/Dashboard/Index');
}

function submitFinish(id) {
    fetch(`/Driver/Dashboard/FinishOrder?orderId=${id}&rating=${selectedRating}`, { method: 'POST' })
        .then(() => window.location.href = '/Driver/Dashboard/Wallet');
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
    statusText.innerText = isChecked ? "Працюю" : "Перерва";

    fetch('/Driver/Dashboard/ToggleWorkingMode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
        body: JSON.stringify({ status: isChecked })
    }).then(res => res.json()).then(data => {
        if (!data.success) {
            checkbox.checked = !isChecked;
            statusText.innerText = checkbox.checked ? "Працюю" : "Перерва";
        }
    }).catch(() => {
        checkbox.checked = !isChecked;
        statusText.innerText = checkbox.checked ? "Працюю" : "Перерва";
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
    btns[0].className = 'fop-btn ' + (isActive ? 'active-yes' : '');
    btns[1].className = 'fop-btn ' + (!isActive ? 'active-no' : '');
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