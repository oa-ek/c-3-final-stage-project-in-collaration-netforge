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
                var startIcon = L.divIcon({ className: '', html: '<div style="width: 25px; height: 25px; background: rgba(16, 185, 129, 0.2); border-radius: 50%; display: flex; align-items: center; justify-content: center;"><div style="width: 10px; height: 10px; background: #10b981; border-radius: 50%;"></div></div>', iconSize: [25, 25] });
                var endIcon = L.divIcon({ className: '', html: '<div style="width: 25px; height: 25px; background: rgba(239, 68, 68, 0.2); border-radius: 50%; display: flex; align-items: center; justify-content: center;"><div style="width: 10px; height: 10px; background: #ef4444; border-radius: 50%;"></div></div>', iconSize: [25, 25] });
                L.marker([50.455, 30.520], { icon: startIcon }).addTo(map);
                L.marker([50.440, 30.530], { icon: endIcon }).addTo(map);
            } else if (!isUnverified) {
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

function switchTab(tabId) {
    const navLinks = document.querySelectorAll('.nav-tabs-custom .nav-link');
    navLinks.forEach(link => link.classList.remove('active'));

    if (event && event.currentTarget) {
        event.currentTarget.classList.add('active');
    }

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
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'status=' + isChecked
    }).then(res => res.json()).then(data => {
        if (!data.success) {
            checkbox.checked = !isChecked;
            statusText.innerText = checkbox.checked ? "Працюю" : "Перерва";
        }
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