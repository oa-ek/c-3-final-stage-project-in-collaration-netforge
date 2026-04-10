let revenueChartInstance = null;
let statusChartInstance = null;
let currentCityMultiplier = 1.0;

$(document).ready(function() {
    const currentPath = window.location.pathname.toLowerCase();
    $('.sidebar-nav .nav-item').removeClass('active');
    $('.sidebar-nav .nav-item').each(function() {
        const itemPath = $(this).attr('href').toLowerCase();
        if (currentPath.includes(itemPath) && itemPath !== "/") {
            $(this).addClass('active');
        }
    });

    if ($('#revenueChart').length) {
        loadCharts();
    }

    $(document).on('change', '.service-check', function() {
        updateTotal();
    });

    $('#CityId').change(function() {
        const cityId = $(this).val();
        if (cityId && !$(this).prop('disabled')) {
            $.get('/Admin/Order/GetCityMultiplier/' + cityId, function(data) {
                currentCityMultiplier = parseFloat(data.multiplier) || 1.0;
                $('#cityMultiplierBadge').text('Тариф: x' + currentCityMultiplier).show();
                updateTotal();
            }).fail(function() {
                $.get('/Order/GetCityMultiplier/' + cityId, function(data) {
                    currentCityMultiplier = parseFloat(data.multiplier) || 1.0;
                    $('#cityMultiplierBadge').text('Тариф: x' + currentCityMultiplier).show();
                    updateTotal();
                });
            });
        }
    });

    $('#OrderStatusId').change(function() {
        const text = $(this).find('option:selected').text().toLowerCase();
        if (text.includes('скасован') || text.includes('відмінен')) {
            $('#cancellationReasonContainer').show();
        } else {
            $('#cancellationReasonContainer').hide();
            $('#CancellationReasonId').val('');
        }
    });
});

function loadCharts() {
    let start = $('#startDate').val();
    let end = $('#endDate').val();
    $.get(`/Admin/Dashboard/GetDashboardData?startDate=${start}&endDate=${end}`, function(data) {
        $('#valRevenue').text(data.kpi.totalRevenue.toLocaleString());
        $('#valOrders').text(data.kpi.totalOrders);
        $('#valAvg').text(data.kpi.averageCheck.toLocaleString());
        $('#valDrivers').text(data.kpi.activeDrivers);

        if (revenueChartInstance) revenueChartInstance.destroy();
        const ctxRev = document.getElementById('revenueChart').getContext('2d');
        revenueChartInstance = new Chart(ctxRev, {
            type: 'line',
            data: {
                labels: data.revenue.map(x => x.date),
                datasets: [{
                    label: 'Дохід ₴',
                    data: data.revenue.map(x => x.amount),
                    borderColor: '#eab308',
                    backgroundColor: 'rgba(234, 179, 8, 0.1)',
                    fill: true,
                    tension: 0.4
                }]
            },
            options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { grid: { color: 'rgba(255,255,255,0.05)' }, ticks: { color: '#94a3b8' } }, x: { grid: { color: 'rgba(255,255,255,0.05)' }, ticks: { color: '#94a3b8' } } } }
        });

        if (statusChartInstance) statusChartInstance.destroy();
        const ctxStat = document.getElementById('statusChart').getContext('2d');
        statusChartInstance = new Chart(ctxStat, {
            type: 'doughnut',
            data: {
                labels: data.statuses.map(x => x.status),
                datasets: [{
                    data: data.statuses.map(x => x.count),
                    backgroundColor: ['#eab308', '#3b82f6', '#10b981', '#ef4444'],
                    borderWidth: 0
                }]
            },
            options: { responsive: true, plugins: { legend: { labels: { color: '#f8fafc' } } } }
        });
    });
}

function exportReport(type) {
    let start = $('#startDate').val();
    let end = $('#endDate').val();
    window.location.href = (type === 'excel' ? '/Admin/Dashboard/ExportToExcel' : '/Admin/Dashboard/ExportToWord') + `?startDate=${start}&endDate=${end}`;
}

function searchClient(phone) {
    if (phone.length >= 10) {
        $.get('/Admin/Order/GetUserDetails?phone=' + phone, function(res) {
            if (res.exists) {
                $('#PassengerName').val(res.fullName);
                $('#UserId').val(res.id);
            }
        }).fail(function() {
            $.get('/Order/GetUserDetails?phone=' + phone, function(res) {
                if (res.exists) {
                    $('#PassengerName').val(res.fullName);
                    $('#UserId').val(res.id);
                }
            });
        });
    }
}

function calcDist() {
    if ($('#PickupAddress').val() && $('#DropoffAddress').val()) {
        const d = (Math.random() * 8 + 3).toFixed(1);
        $('#Distance').val(d.replace('.', ','));
        updateTotal();
    }
}

function updateTotal() {
    const d = parseFloat($('#Distance').val()?.replace(',', '.')) || 0;
    const b = parseFloat($('#ClientPriceBonus').val()?.replace(',', '.')) || 0;
    let servicesPrice = 0;
    $('.service-check:checked').each(function() {
        servicesPrice += parseFloat($(this).attr('data-price')) || 0;
    });
    const total = (d * 15 * currentCityMultiplier) + b + servicesPrice;
    $('#TotalPrice').val(total.toFixed(2).replace('.', ','));
}

function setupModalState(formId, btnId, viewOnly) {
    if (viewOnly) {
        $(btnId).hide();
        $(formId + ' input, ' + formId + ' select, ' + formId + ' textarea').prop('disabled', true);
    } else {
        $(btnId).show();
        $(formId + ' input, ' + formId + ' select, ' + formId + ' textarea').prop('disabled', false);
    }
}

function showBootstrapModal(modalId) {
    var modalEl = document.getElementById(modalId);
    if (modalEl) {
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) {
            modal = new bootstrap.Modal(modalEl);
        }
        modal.show();
    }
}

function openOrderModal(id, viewOnly) {
    $('#orderForm')[0].reset();
    $('#OrderId').val(id);
    $('.service-check').prop('checked', false);
    $('#cancellationReasonContainer').hide();
    setupModalState('#orderForm', '#saveBtn', viewOnly);

    if (id === 0) {
        $('#modalTitle').text('Нове замовлення');
        showBootstrapModal('orderModal');
    } else {
        $.get('/Admin/Order/GetOrderDetails/' + id, function(res) {
            populateOrderData(res, viewOnly);
        }).fail(function() {
            $.get('/Order/GetOrderDetails/' + id, function(res) {
                populateOrderData(res, viewOnly);
            });
        });
    }
}

function populateOrderData(res, viewOnly) {
    $('#PassengerPhone').val(res.passengerPhone);
    $('#PassengerName').val(res.passengerName);
    $('#DriverId').val(res.driverId);
    $('#OrderStatusId').val(res.orderStatusId).trigger('change');
    $('#VehicleClassId').val(res.vehicleClassId);
    $('#CityId').val(res.cityId);
    $('#PickupAddress').val(res.pickupAddress);
    $('#DropoffAddress').val(res.dropoffAddress);
    $('#Distance').val(res.distance);
    $('#PaymentMethodId').val(res.paymentMethodId);
    $('#PromoCodeId').val(res.promoCodeId);
    $('#CancellationReasonId').val(res.cancellationReasonId);
    $('#ClientPriceBonus').val(res.clientPriceBonus);
    $('#TotalPrice').val(res.totalPrice);
    if (res.selectedServiceIds) res.selectedServiceIds.forEach(sid => $('#srv_' + sid).prop('checked', true));
    $('#modalTitle').text(viewOnly ? 'Перегляд замовлення' : 'Редагування замовлення');
    showBootstrapModal('orderModal');
}

function openCityModal(id, viewOnly) {
    $('#cityForm')[0].reset();
    $('#c_Id').val(id);
    setupModalState('#cityForm', '#citySaveBtn', viewOnly);

    if (id === 0) {
        $('#cityModalTitle').text('Додати місто');
        $('#c_PriceMultiplier').val("1,0");
        showBootstrapModal('cityModal');
    } else {
        $.get('/Admin/Settings/GetCity/' + id, function(data) {
            $('#c_Name').val(data.name);
            $('#c_PriceMultiplier').val(data.priceMultiplier ? data.priceMultiplier.toString().replace('.', ',') : "1,0");
            $('#cityModalTitle').text(viewOnly ? 'Перегляд міста' : 'Редагування міста');
            showBootstrapModal('cityModal');
        }).fail(function() {
            $.get('/Settings/GetCity/' + id, function(data) {
                $('#c_Name').val(data.name);
                $('#c_PriceMultiplier').val(data.priceMultiplier ? data.priceMultiplier.toString().replace('.', ',') : "1,0");
                $('#cityModalTitle').text(viewOnly ? 'Перегляд міста' : 'Редагування міста');
                showBootstrapModal('cityModal');
            });
        });
    }
}

function openServiceModal(id, viewOnly) {
    $('#serviceForm')[0].reset();
    $('#s_Id').val(id);
    setupModalState('#serviceForm', '#serviceSaveBtn', viewOnly);

    if (id === 0) {
        $('#serviceModalTitle').text('Додати послугу');
        showBootstrapModal('serviceModal');
    } else {
        $.get('/Admin/Settings/GetService/' + id, function(data) {
            $('#s_Name').val(data.name);
            $('#s_Price').val(data.price ? data.price.toString().replace('.', ',') : "0,0");
            $('#s_IsPercentage').prop('checked', data.isPercentage);
            $('#serviceModalTitle').text(viewOnly ? 'Перегляд послуги' : 'Редагування послуги');
            showBootstrapModal('serviceModal');
        }).fail(function() {
            $.get('/Settings/GetService/' + id, function(data) {
                $('#s_Name').val(data.name);
                $('#s_Price').val(data.price ? data.price.toString().replace('.', ',') : "0,0");
                $('#s_IsPercentage').prop('checked', data.isPercentage);
                $('#serviceModalTitle').text(viewOnly ? 'Перегляд послуги' : 'Редагування послуги');
                showBootstrapModal('serviceModal');
            });
        });
    }
}

function openStatusModal(id, viewOnly) {
    $('#statusForm')[0].reset();
    $('#st_Id').val(id);
    setupModalState('#statusForm', '#statusSaveBtn', viewOnly);

    if (id === 0) {
        $('#statusModalTitle').text('Додати статус');
        showBootstrapModal('statusModal');
    } else {
        $.get('/Admin/Settings/GetStatus/' + id, function(data) {
            $('#st_Name').val(data.name);
            $('#statusModalTitle').text(viewOnly ? 'Перегляд статусу' : 'Редагування статусу');
            showBootstrapModal('statusModal');
        }).fail(function() {
            $.get('/Settings/GetStatus/' + id, function(data) {
                $('#st_Name').val(data.name);
                $('#statusModalTitle').text(viewOnly ? 'Перегляд статусу' : 'Редагування статусу');
                showBootstrapModal('statusModal');
            });
        });
    }
}

function openClientModal(id, viewOnly) {
    $('#clientForm')[0].reset();
    $('#u_Id').val(id);
    setupModalState('#clientForm', '#clientSaveBtn', viewOnly);

    if (id === 0) {
        $('#clientModalTitle').text('Новий клієнт');
        $('#u_Rating').val("5,0");
        showBootstrapModal('clientModal');
    } else {
        $.get('/Admin/People/GetUser/' + id, function(data) {
            $('#u_FirstName').val(data.firstName);
            $('#u_LastName').val(data.lastName);
            $('#u_PhoneNumber').val(data.phoneNumber);
            $('#u_Email').val(data.email);
            $('#u_AvatarPath').val(data.avatarPath);
            $('#u_Rating').val(data.rating ? data.rating.toString().replace('.', ',') : "5,0");
            $('#u_PrefersSilentRide').prop('checked', data.prefersSilentRide);
            $('#clientModalTitle').text(viewOnly ? 'Перегляд клієнта' : 'Редагування клієнта');
            showBootstrapModal('clientModal');
        }).fail(function() {
            $.get('/People/GetUser/' + id, function(data) {
                $('#u_FirstName').val(data.firstName);
                $('#u_LastName').val(data.lastName);
                $('#u_PhoneNumber').val(data.phoneNumber);
                $('#u_Email').val(data.email);
                $('#u_AvatarPath').val(data.avatarPath);
                $('#u_Rating').val(data.rating ? data.rating.toString().replace('.', ',') : "5,0");
                $('#u_PrefersSilentRide').prop('checked', data.prefersSilentRide);
                $('#clientModalTitle').text(viewOnly ? 'Перегляд клієнта' : 'Редагування клієнта');
                showBootstrapModal('clientModal');
            });
        });
    }
}

function openDriverModal(id, viewOnly) {
    $('#driverForm')[0].reset();
    $('#d_DriverId').val(id);
    $('#d_UserId').val(0);
    setupModalState('#driverForm', '#driverSaveBtn', viewOnly);

    if (id === 0) {
        $('#driverModalTitle').text('Найняти водія');
        $('#d_CommissionRate').val("10,0");
        showBootstrapModal('driverModal');
    } else {
        $.get('/Admin/People/GetDriverDetails/' + id, function(data) {
            populateDriverData(data, viewOnly);
        }).fail(function() {
            $.get('/People/GetDriverDetails/' + id, function(data) {
                populateDriverData(data, viewOnly);
            });
        });
    }
}

function populateDriverData(data, viewOnly) {
    $('#d_DriverId').val(data.driverId);
    $('#d_UserId').val(data.userId);
    $('#d_AvatarPath').val(data.avatarPath);
    $('#d_FirstName').val(data.firstName);
    $('#d_LastName').val(data.lastName);
    $('#d_PhoneNumber').val(data.phoneNumber);
    $('#d_Email').val(data.email);
    $('#d_Patronymic').val(data.patronymic);
    $('#d_TaxId').val(data.taxId);
    $('#d_Iban').val(data.iban);
    if (data.dateOfBirth) $('#d_DateOfBirth').val(data.dateOfBirth.split('T')[0]);
    $('#d_CommissionRate').val(data.commissionRate ? data.commissionRate.toString().replace('.', ',') : "10,0");
    $('#d_IsVerified').prop('checked', data.isVerified);
    $('#driverModalTitle').text(viewOnly ? 'Перегляд водія' : 'Редагування водія');
    showBootstrapModal('driverModal');
}

function openVehicleModal(id, viewOnly) {
    $('#vehicleForm')[0].reset();
    $('#v_Id').val(id);
    $('.v-class-check, .v-srv-check').prop('checked', false);
    $('#v_PhotosPreview').empty();
    setupModalState('#vehicleForm', '#vehicleSaveBtn', viewOnly);

    if (id === 0) {
        $('#vehicleModalTitle').text('Додати автомобіль');
        showBootstrapModal('vehicleModal');
    } else {
        $.get('/Admin/Fleet/GetVehicleDetails/' + id, function(data) {
            populateVehicleData(data, viewOnly);
        }).fail(function() {
            $.get('/Fleet/GetVehicleDetails/' + id, function(data) {
                populateVehicleData(data, viewOnly);
            });
        });
    }
}

function populateVehicleData(data, viewOnly) {
    $('#v_DriverId').val(data.driverId);
    $('#v_Brand').val(data.brand);
    $('#v_Model').val(data.model);
    $('#v_Year').val(data.year);
    $('#v_Color').val(data.color);
    $('#v_LicensePlate').val(data.licensePlate);
    $('#v_PassengerSeats').val(data.passengerSeats);
    if (data.insuranceExpiryDate) $('#v_InsuranceExpiryDate').val(data.insuranceExpiryDate.split('T')[0]);
    if (data.selectedClassIds) data.selectedClassIds.forEach(cid => $('#cls_' + cid).prop('checked', true));
    if (data.selectedServiceIds) data.selectedServiceIds.forEach(sid => $('#vsrv_' + sid).prop('checked', true));

    if (data.photos && data.photos.length > 0) {
        data.photos.forEach(p => {
            $('#v_PhotosPreview').append(`<img src="${p}" style="width: 50px; height: 50px; object-fit: cover; border-radius: 5px;">`);
        });
    }

    $('#vehicleModalTitle').text(viewOnly ? 'Перегляд авто' : 'Редагування авто');
    showBootstrapModal('vehicleModal');
}

function openPromoModal(id, viewOnly) {
    $('#promoForm')[0].reset();
    $('#p_Id').val(id);
    setupModalState('#promoForm', '#promoSaveBtn', viewOnly);

    if (id === 0) {
        $('#promoModalTitle').text('Створити промокод');
        $('#p_DiscountPercentage').val("10,0");
        showBootstrapModal('promoModal');
    } else {
        $.get('/Admin/Marketing/GetPromoCode/' + id, function(data) {
            $('#p_Code').val(data.code);
            $('#p_DiscountPercentage').val(data.discountPercentage ? data.discountPercentage.toString().replace('.', ',') : "0,0");
            if (data.expiryDate) $('#p_ExpiryDate').val(data.expiryDate.split('T')[0]);
            $('#p_MaxUses').val(data.maxUses);
            $('#promoModalTitle').text(viewOnly ? 'Перегляд промокоду' : 'Редагування промокоду');
            showBootstrapModal('promoModal');
        }).fail(function() {
            $.get('/Marketing/GetPromoCode/' + id, function(data) {
                $('#p_Code').val(data.code);
                $('#p_DiscountPercentage').val(data.discountPercentage ? data.discountPercentage.toString().replace('.', ',') : "0,0");
                if (data.expiryDate) $('#p_ExpiryDate').val(data.expiryDate.split('T')[0]);
                $('#p_MaxUses').val(data.maxUses);
                $('#promoModalTitle').text(viewOnly ? 'Перегляд промокоду' : 'Редагування промокоду');
                showBootstrapModal('promoModal');
            });
        });
    }
}

function openNewsModal(id, viewOnly) {
    $('#newsForm')[0].reset();
    $('#n_Id').val(id);
    setupModalState('#newsForm', '#newsSaveBtn', viewOnly);

    if (id === 0) {
        $('#newsModalTitle').text('Додати новину');
        showBootstrapModal('newsModal');
    } else {
        $.get('/Admin/Marketing/GetNewsItem/' + id, function(data) {
            $('#n_Title').val(data.title);
            $('#n_ImagePath').val(data.imagePath);
            $('#n_Description').val(data.description);
            $('#newsModalTitle').text(viewOnly ? 'Перегляд новини' : 'Редагування новини');
            showBootstrapModal('newsModal');
        }).fail(function() {
            $.get('/Marketing/GetNewsItem/' + id, function(data) {
                $('#n_Title').val(data.title);
                $('#n_ImagePath').val(data.imagePath);
                $('#n_Description').val(data.description);
                $('#newsModalTitle').text(viewOnly ? 'Перегляд новини' : 'Редагування новини');
                showBootstrapModal('newsModal');
            });
        });
    }
}

function verifyDriver(id, approve) {
    const actionText = approve ? 'Підтвердити документи водія?' : 'Відхилити заявку водія?';
    if (confirm(actionText)) {
        $.post('/Admin/People/VerifyDriver', { id: id, approve: approve }, function() {
            location.reload();
        }).fail(function() {
            $.post('/People/VerifyDriver', { id: id, approve: approve }, function() {
                location.reload();
            });
        });
    }
}

function deleteReview(id) {
    if (confirm('Видалити цей відгук?')) {
        $.post('/Admin/People/DeleteReview', { id: id }, function() {
            location.reload();
        }).fail(function() {
            $.post('/People/DeleteReview', { id: id }, function() {
                location.reload();
            });
        });
    }
}

function unblockUser(id) {
    if (confirm('Розблокувати користувача?')) {
        $.post('/Admin/People/RemoveFromBlacklist', { id: id }, function() {
            location.reload();
        }).fail(function() {
            $.post('/People/RemoveFromBlacklist', { id: id }, function() {
                location.reload();
            });
        });
    }
}

function openVehicleClassModal(id, viewOnly) {
    $('#vehicleClassForm')[0].reset();
    $('#vc_Id').val(id);
    setupModalState('#vehicleClassForm', '#vehicleClassSaveBtn', viewOnly);

    if (id === 0) {
        $('#vehicleClassModalTitle').text('Створити клас авто');
        showBootstrapModal('vehicleClassModal');
    } else {
        $.get('/Admin/Settings/GetVehicleClass/' + id, function(data) {
            $('#vc_Name').val(data.name);
            $('#vc_Description').val(data.description);
            $('#vc_BasePrice').val(data.basePrice ? data.basePrice.toString().replace('.', ',') : "0,0");
            $('#vc_PricePerKm').val(data.pricePerKm ? data.pricePerKm.toString().replace('.', ',') : "0,0");
            $('#vc_PricePerKmOutsideCity').val(data.pricePerKmOutsideCity ? data.pricePerKmOutsideCity.toString().replace('.', ',') : "0,0");
            $('#vc_PricePerMinuteWaiting').val(data.pricePerMinuteWaiting ? data.pricePerMinuteWaiting.toString().replace('.', ',') : "0,0");
            $('#vc_CancellationFee').val(data.cancellationFee ? data.cancellationFee.toString().replace('.', ',') : "0,0");
            $('#vehicleClassModalTitle').text(viewOnly ? 'Перегляд класу' : 'Редагування класу');
            showBootstrapModal('vehicleClassModal');
        }).fail(function() {
            $.get('/Settings/GetVehicleClass/' + id, function(data) {
                $('#vc_Name').val(data.name);
                $('#vc_Description').val(data.description);
                $('#vc_BasePrice').val(data.basePrice ? data.basePrice.toString().replace('.', ',') : "0,0");
                $('#vc_PricePerKm').val(data.pricePerKm ? data.pricePerKm.toString().replace('.', ',') : "0,0");
                $('#vc_PricePerKmOutsideCity').val(data.pricePerKmOutsideCity ? data.pricePerKmOutsideCity.toString().replace('.', ',') : "0,0");
                $('#vc_PricePerMinuteWaiting').val(data.pricePerMinuteWaiting ? data.pricePerMinuteWaiting.toString().replace('.', ',') : "0,0");
                $('#vc_CancellationFee').val(data.cancellationFee ? data.cancellationFee.toString().replace('.', ',') : "0,0");
                $('#vehicleClassModalTitle').text(viewOnly ? 'Перегляд класу' : 'Редагування класу');
                showBootstrapModal('vehicleClassModal');
            });
        });
    }
}

function openPaymentMethodModal(id, viewOnly) {
    $('#paymentMethodForm')[0].reset();
    $('#pm_Id').val(id);
    setupModalState('#paymentMethodForm', '#paymentMethodSaveBtn', viewOnly);

    if (id === 0) {
        $('#paymentMethodModalTitle').text('Додати метод оплати');
        showBootstrapModal('paymentMethodModal');
    } else {
        $.get('/Admin/Settings/GetPaymentMethod/' + id, function(data) {
            $('#pm_Name').val(data.name);
            $('#paymentMethodModalTitle').text(viewOnly ? 'Перегляд' : 'Редагування');
            showBootstrapModal('paymentMethodModal');
        }).fail(function() {
            $.get('/Settings/GetPaymentMethod/' + id, function(data) {
                $('#pm_Name').val(data.name);
                $('#paymentMethodModalTitle').text(viewOnly ? 'Перегляд' : 'Редагування');
                showBootstrapModal('paymentMethodModal');
            });
        });
    }
}

function openCancellationReasonModal(id, viewOnly) {
    $('#cancellationReasonForm')[0].reset();
    $('#cr_Id').val(id);
    setupModalState('#cancellationReasonForm', '#cancellationReasonSaveBtn', viewOnly);

    if (id === 0) {
        $('#cancellationReasonModalTitle').text('Додати причину');
        showBootstrapModal('cancellationReasonModal');
    } else {
        $.get('/Admin/Settings/GetCancellationReason/' + id, function(data) {
            $('#cr_Name').val(data.name);
            $('#cancellationReasonModalTitle').text(viewOnly ? 'Перегляд' : 'Редагування');
            showBootstrapModal('cancellationReasonModal');
        }).fail(function() {
            $.get('/Settings/GetCancellationReason/' + id, function(data) {
                $('#cr_Name').val(data.name);
                $('#cancellationReasonModalTitle').text(viewOnly ? 'Перегляд' : 'Редагування');
                showBootstrapModal('cancellationReasonModal');
            });
        });
    }
}