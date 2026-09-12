// F House (FH) Client Application Script

// Theme Management (Light / Dark Mode)
function inicializarTema() {
    const temaGuardado = localStorage.getItem('fh-theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    aplicarTema(temaGuardado);
}

function alternarTema() {
    const temaActual = document.documentElement.classList.contains('dark') ? 'dark' : 'light';
    const nuevoTema = temaActual === 'dark' ? 'light' : 'dark';
    aplicarTema(nuevoTema);
    localStorage.setItem('fh-theme', nuevoTema);
}

function aplicarTema(tema) {
    if (tema === 'dark') {
        document.documentElement.classList.add('dark');
        document.documentElement.classList.remove('light');
    } else {
        document.documentElement.classList.add('light');
        document.documentElement.classList.remove('dark');
    }

    const iconoTema = document.getElementById('icono-tema');
    if (iconoTema) {
        iconoTema.setAttribute('data-lucide', tema === 'dark' ? 'sun' : 'moon');
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }
}

// Modal Global Helpers
window.abrirModalTransaccion = function () {
    const modal = document.getElementById('modal-nueva-transaccion');
    if (modal) {
        modal.style.display = 'flex';
    }
    
    const camposContainer = document.getElementById('modal-transaccion-campos-container');
    if (camposContainer && window.htmx) {
        htmx.trigger(camposContainer, 'openModalTransaccion');
    }
    if (window.lucide) {
        window.lucide.createIcons();
    }
};

window.cerrarModalTransaccion = function () {
    const modal = document.getElementById('modal-nueva-transaccion');
    if (modal) {
        modal.style.display = 'none';
    }
    const spinner = document.getElementById('loading-spinner');
    if (spinner) {
        spinner.classList.add('hidden');
    }
};

window.seleccionarTipoTransaccion = function (tipo) {
    const input = document.getElementById('modal-input-tipo');
    if (input) input.value = tipo;

    const btnEgreso = document.getElementById('modal-btn-tipo-egreso');
    const btnIngreso = document.getElementById('modal-btn-tipo-ingreso');

    if (tipo === '2') {
        if (btnEgreso) btnEgreso.className = "py-2.5 rounded-xl text-xs flex items-center justify-center gap-2 transition-all bg-[#ff3b30]/15 text-[#ff3b30] dark:text-[#ff453a] shadow-sm font-bold";
        if (btnIngreso) btnIngreso.className = "py-2.5 rounded-xl text-xs flex items-center justify-center gap-2 transition-all text-subtitle hover:text-title font-medium";
    } else {
        if (btnIngreso) btnIngreso.className = "py-2.5 rounded-xl text-xs flex items-center justify-center gap-2 transition-all bg-[#34c759]/15 text-[#34c759] dark:text-[#30d158] shadow-sm font-bold";
        if (btnEgreso) btnEgreso.className = "py-2.5 rounded-xl text-xs flex items-center justify-center gap-2 transition-all text-subtitle hover:text-title font-medium";
    }

    if (window.lucide) {
        window.lucide.createIcons();
    }
};

document.addEventListener('DOMContentLoaded', () => {
    inicializarTema();

    // Re-initialize icons and dynamic bindings on htmx swap
    document.body.addEventListener('htmx:afterSwap', function (evt) {
        if (window.lucide) {
            window.lucide.createIcons();
        }
    });

    // Handle toast messages and auto-close modals on trigger
    // NOTE: Data refresh (KPI cards, transaction table) is handled declaratively via htmx
    // attributes on #dashboard-live-content and #transacciones-tbody — NO manual reload needed.
    document.body.addEventListener('transaccionCreada', function () {
        mostrarToast('Movimiento registrado exitosamente.', 'success');
        cerrarModalTransaccion();

        const form = document.getElementById('form-nueva-transaccion');
        if (form) {
            form.reset();
            // Reset tipo selector to default (Egreso)
            seleccionarTipoTransaccion('2');
        }
    });

    document.body.addEventListener('transaccionAnulada', function () {
        mostrarToast('Transacción anulada correctamente.', 'success');
    });

    document.body.addEventListener('fuenteCreada', function () {
        mostrarToast('Fuente de ingreso creada con éxito.', 'success');
        cerrarModalTransaccion();
        if (window.lucide) window.lucide.createIcons();
    });

    document.body.addEventListener('cuentaCreada', function () {
        mostrarToast('Cuenta bancaria agregada exitosamente.', 'success');
        cerrarModalTransaccion();
        if (window.lucide) window.lucide.createIcons();
    });

    document.body.addEventListener('transferenciaExitosa', function () {
        mostrarToast('Transferencia completada correctamente.', 'success');
        cerrarModalTransaccion();
        if (window.lucide) window.lucide.createIcons();
    });

    document.body.addEventListener('categoriaCreada', function () {
        mostrarToast('Categoría creada exitosamente.', 'success');
        cerrarModalTransaccion();
        if (window.lucide) window.lucide.createIcons();
    });

    if (window.lucide) {
        window.lucide.createIcons();
    }
});

function mostrarToast(mensaje, tipo = 'info') {
    const toast = document.createElement('div');
    const colorBg = tipo === 'success' ? 'bg-emerald-500/20 border-emerald-500/30 text-emerald-400' : 'bg-rose-500/20 border-rose-500/30 text-rose-400';
    toast.className = `fixed bottom-6 right-6 z-50 px-5 py-3.5 rounded-full border backdrop-blur-xl flex items-center gap-3 text-sm font-medium shadow-2xl transition-all duration-300 transform translate-y-4 opacity-0 ${colorBg}`;
    toast.innerHTML = `
        <span class="w-2 h-2 rounded-full ${tipo === 'success' ? 'bg-emerald-400' : 'bg-rose-400'} animate-pulse"></span>
        <span>${mensaje}</span>
    `;

    document.body.appendChild(toast);

    requestAnimationFrame(() => {
        toast.classList.remove('translate-y-4', 'opacity-0');
    });

    setTimeout(() => {
        toast.classList.add('translate-y-4', 'opacity-0');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// Auto-Logout por Inactividad (5 minutos)
function inicializarAutoLogout(minutos) {
    minutos = minutos || 5;
    const tiempoInactividadMs = minutos * 60 * 1000;
    let temporizadorInactividad;

    function resetearTemporizador() {
        if (temporizadorInactividad) {
            clearTimeout(temporizadorInactividad);
        }
        temporizadorInactividad = setTimeout(function () {
            // Limpieza y redirección automática por inactividad
            window.location.href = '/Account/Logout';
        }, tiempoInactividadMs);
    }

    const eventos = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'click'];
    for (var i = 0; i < eventos.length; i++) {
        window.addEventListener(eventos[i], resetearTemporizador, { passive: true });
    }

    resetearTemporizador();
}
