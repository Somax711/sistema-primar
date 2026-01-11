// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function() {
    
    // Auto-hide alerts después de 5 segundos
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => {
                alert.remove();
            }, 300);
        }, 5000);
    });
    
    // Smooth scroll para enlaces internos
    const internalLinks = document.querySelectorAll('a[href^="#"]');
    internalLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ 
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
    
    // Confirmación para acciones importantes
    const confirmButtons = document.querySelectorAll('[data-confirm]');
    confirmButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const message = this.getAttribute('data-confirm');
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });
    
    // Manejo de subida de archivos
    const fileInput = document.getElementById('archivos');
    if (fileInput) {
        fileInput.addEventListener('change', handleFileSelection);
        
        // Drag and drop
        const fileLabel = document.querySelector('.file-label');
        if (fileLabel) {
            fileLabel.addEventListener('dragover', handleDragOver);
            fileLabel.addEventListener('drop', handleFileDrop);
        }
    }
    
    // Actualizar badge de notificaciones
    updateNotificationBadge();
    
    // Inicializar tooltips si están disponibles
    initializeTooltips();

    document.querySelectorAll('input[name="Rut"], input[name="rut"], input.rut').forEach(function(input) {
        input.addEventListener('input', function() { formatearRutInput(input); });
    });
    document.querySelectorAll('input[name="Telefono"], input[name="telefono"], input.telefono').forEach(function(input) {
        input.addEventListener('input', function() { formatearTelefonoInput(input); });
    });
});

// Manejo de selección de archivos
function handleFileSelection(e) {
    const files = e.target.files;
    displaySelectedFiles(files);
    validateFiles(files);
}

// Manejo de drag over
function handleDragOver(e) {
    e.preventDefault();
    e.stopPropagation();
    this.classList.add('drag-over');
}

// Manejo de drop
function handleFileDrop(e) {
    e.preventDefault();
    e.stopPropagation();
    this.classList.remove('drag-over');
    
    const files = e.dataTransfer.files;
    const fileInput = document.getElementById('archivos');
    
    if (fileInput) {
        fileInput.files = files;
        displaySelectedFiles(files);
        validateFiles(files);
    }
}

// Mostrar archivos seleccionados
function displaySelectedFiles(files) {
    const fileList = document.getElementById('file-list');
    if (!fileList) return;
    
    fileList.innerHTML = '';
    
    Array.from(files).forEach(file => {
        const fileItem = document.createElement('div');
        fileItem.className = 'file-item';
        fileItem.innerHTML = `
            <i class="fas ${getFileIcon(file.type)}"></i>
            <span class="file-name">${file.name}</span>
            <span class="file-size">(${formatFileSize(file.size)})</span>
        `;
        fileList.appendChild(fileItem);
    });
}

// Obtener icono según tipo de archivo
function getFileIcon(fileType) {
    if (fileType.includes('pdf')) return 'fa-file-pdf';
    if (fileType.includes('image')) return 'fa-file-image';
    if (fileType.includes('excel') || fileType.includes('spreadsheet')) return 'fa-file-excel';
    if (fileType.includes('word')) return 'fa-file-word';
    return 'fa-file';
}

// Formatear tamaño de archivo
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Validar archivos
function validateFiles(files) {
    const allowedTypes = [
        'application/pdf',
        'image/jpeg',
        'image/png',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'application/vnd.ms-excel',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ];
    
    const maxSize = 10 * 1024 * 1024; // 10MB
    let hasErrors = false;
    
    Array.from(files).forEach(file => {
        if (!allowedTypes.includes(file.type)) {
            showAlert(`Tipo de archivo no permitido: ${file.name}`, 'error');
            hasErrors = true;
        }
        
        if (file.size > maxSize) {
            showAlert(`Archivo muy grande: ${file.name}. Máximo 10MB`, 'error');
            hasErrors = true;
        }
    });
    
    if (hasErrors) {
        const fileInput = document.getElementById('archivos');
        if (fileInput) fileInput.value = '';
        document.getElementById('file-list').innerHTML = '';
    }
    
    return !hasErrors;
}

// Mostrar alertas dinámicas
function showAlert(message, type = 'info') {
    const alertContainer = document.querySelector('.container');
    if (!alertContainer) return;
    
    const alert = document.createElement('div');
    alert.className = `alert alert-${type === 'error' ? 'error' : type}`;
    alert.innerHTML = `
        <i class="fas ${type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle'}"></i>
        ${message}
    `;
    
    alertContainer.insertBefore(alert, alertContainer.firstChild);
    
    // Auto-remove después de 5 segundos
    setTimeout(() => {
        alert.style.opacity = '0';
        setTimeout(() => alert.remove(), 300);
    }, 5000);
}

// Actualizar badge de notificaciones
function updateNotificationBadge() {
    const badge = document.querySelector('.badge');
    if (badge && badge.textContent === '0') {
        badge.style.display = 'none';
    }
}

// Función para formatear montos
function formatCurrency(amount) {
    return new Intl.NumberFormat('es-CL', {
        style: 'currency',
        currency: 'CLP'
    }).format(amount);
}

// Inicializar tooltips
function initializeTooltips() {
    const tooltipElements = document.querySelectorAll('[data-tooltip]');
    tooltipElements.forEach(element => {
        element.addEventListener('mouseenter', showTooltip);
        element.addEventListener('mouseleave', hideTooltip);
    });
}

// Mostrar tooltip
function showTooltip(e) {
    const text = this.getAttribute('data-tooltip');
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = text;
    document.body.appendChild(tooltip);
    
    const rect = this.getBoundingClientRect();
    tooltip.style.left = rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2) + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 5 + 'px';
}

// Ocultar tooltip
function hideTooltip() {
    const tooltip = document.querySelector('.tooltip');
    if (tooltip) {
        tooltip.remove();
    }
}

// Confirmar acciones importantes
function confirmAction(message, callback) {
    if (confirm(message)) {
        if (typeof callback === 'function') {
            callback();
        }
        return true;
    }
    return false;
}

// Copiar texto al portapapeles
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showAlert('Copiado al portapapeles', 'success');
    }).catch(err => {
        console.error('Error al copiar: ', err);
        showAlert('Error al copiar al portapapeles', 'error');
    });
}

// Loading spinner
function showLoading(element) {
    if (element) {
        element.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Cargando...';
        element.disabled = true;
    }
}

function hideLoading(element, originalText) {
    if (element) {
        element.innerHTML = originalText;
        element.disabled = false;
    }
}

// Validación de formularios en tiempo real
function setupFormValidation() {
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        const inputs = form.querySelectorAll('input[required], textarea[required]');
        inputs.forEach(input => {
            input.addEventListener('blur', validateField);
            input.addEventListener('input', clearFieldError);
        });
    });
}

function validateField(e) {
    const field = e.target;
    const value = field.value.trim();
    
    if (!value && field.hasAttribute('required')) {
        showFieldError(field, 'Este campo es requerido');
        return false;
    }
    
    if (field.type === 'email' && value && !isValidEmail(value)) {
        showFieldError(field, 'Ingresa un email válido');
        return false;
    }
    
    clearFieldError(field);
    return true;
}

function showFieldError(field, message) {
    clearFieldError(field);
    
    const errorElement = document.createElement('span');
    errorElement.className = 'field-error text-danger';
    errorElement.textContent = message;
    
    field.parentNode.appendChild(errorElement);
    field.classList.add('error');
}

function clearFieldError(field) {
    const errorElement = field.parentNode.querySelector('.field-error');
    if (errorElement) {
        errorElement.remove();
    }
    field.classList.remove('error');
}

function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// Búsqueda en tiempo real
function setupSearch() {
    const searchInput = document.getElementById('search');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(performSearch, 300));
    }
}

function performSearch(e) {
    const query = e.target.value.toLowerCase();
    const items = document.querySelectorAll('.searchable-item');
    
    items.forEach(item => {
        const text = item.textContent.toLowerCase();
        if (text.includes(query)) {
            item.style.display = 'block';
        } else {
            item.style.display = 'none';
        }
    });
}

// Debounce function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Animaciones
function animateElement(element, animationClass) {
    element.classList.add(animationClass);
    element.addEventListener('animationend', () => {
        element.classList.remove(animationClass);
    }, { once: true });
}

// Manejo de errores globales
window.addEventListener('error', function(e) {
    console.error('Error:', e.error);
    // En producción, enviar errores a servicio de logging
});

// Funciones de utilidad
const Utils = {
    formatDate: function(date) {
        return new Date(date).toLocaleDateString('es-CL');
    },
    
    formatDateTime: function(date) {
        return new Date(date).toLocaleString('es-CL');
    },
    
    truncateText: function(text, length = 100) {
        return text.length > length ? text.substring(0, length) + '...' : text;
    },
    
    slugify: function(text) {
        return text.toLowerCase()
            .replace(/[^\w ]+/g, '')
            .replace(/ +/g, '-');
    }
};

// Exportar funciones globales
window.PrimarSystem = {
    showAlert,
    confirmAction,
    copyToClipboard,
    showLoading,
    hideLoading,
    formatCurrency,
    Utils
};

function formatearRutInput(input) {
    let rut = input.value.replace(/\./g, '').replace(/-/g, '').replace(/[^0-9kK]/g, '');
    if (rut.length < 2) { input.value = rut; return; }
    let cuerpo = rut.slice(0, -1);
    let dv = rut.slice(-1);
    let cuerpoFormateado = '';
    let contador = 0;
    for (let i = cuerpo.length - 1; i >= 0; i--) {
        cuerpoFormateado = cuerpo[i] + cuerpoFormateado;
        contador++;
        if (contador === 3 && i !== 0) {
            cuerpoFormateado = '.' + cuerpoFormateado;
            contador = 0;
        }
    }
    input.value = `${cuerpoFormateado}-${dv}`;
}

function formatearTelefonoInput(input) {
    let tel = input.value.replace(/[^0-9+]/g, '');
    // Chile
    if (tel.startsWith('+569') && tel.length >= 12) {
        input.value = `+56 9 ${tel.slice(4,8)} ${tel.slice(8,12)}`.trim();
        return;
    }
    if (tel.startsWith('9') && tel.length >= 9) {
        input.value = `9 ${tel.slice(1,5)} ${tel.slice(5,9)}`.trim();
        return;
    }
    // Argentina
    if (tel.startsWith('+54911') && tel.length >= 13) {
        input.value = `+54 9 11 ${tel.slice(6,10)} ${tel.slice(10,14)}`.trim();
        return;
    }
    if (tel.startsWith('11') && tel.length >= 10) {
        input.value = `11 ${tel.slice(2,6)} ${tel.slice(6,10)}`.trim();
        return;
    }
    // Perú
    if (tel.startsWith('+519') && tel.length >= 12) {
        input.value = `+51 9 ${tel.slice(4,8)} ${tel.slice(8,12)}`.trim();
        return;
    }
    if (tel.startsWith('9') && tel.length >= 9) {
        input.value = `9 ${tel.slice(1,5)} ${tel.slice(5,9)}`.trim();
        return;
    }
    input.value = tel;
}