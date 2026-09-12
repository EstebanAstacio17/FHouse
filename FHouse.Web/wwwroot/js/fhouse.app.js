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

document.addEventListener('DOMContentLoaded', () => {
    inicializarTema();

    // Re-initialize icons and dynamic bindings on htmx swap
    document.body.addEventListener('htmx:afterSwap', function (evt) {
        if (window.lucide) {
            window.lucide.createIcons();
        }
    });

    // Handle toast messages on trigger
    document.body.addEventListener('transaccionCreada', function () {
        mostrarToast('Transacción registrada exitosamente.', 'success');
        const modal = document.querySelector('[x-data]');
        if (modal && modal.__x) {
            modal.__x.$data.openModalTransaccion = false;
        }
    });

    document.body.addEventListener('fuenteCreada', function () {
        mostrarToast('Fuente de ingreso creada con éxito.', 'success');
    });

    document.body.addEventListener('cuentaCreada', function () {
        mostrarToast('Cuenta bancaria agregada.', 'success');
    });

    document.body.addEventListener('transferenciaExitosa', function () {
        mostrarToast('Transferencia completada correctamente.', 'success');
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
