document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('login-form');
    const loginBtn = document.getElementById('login-btn');
    const loginInputs = document.querySelectorAll('.inputBox input');
    const errorMessage = document.getElementById('error-message');

    // Manejar el estado de los inputs
    loginInputs.forEach(input => {
        input.addEventListener('blur', function() {
            if (this.value.trim() !== '') {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
        });

        input.addEventListener('input', function() {
            if (this.value.trim() !== '') {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
        });

        // Verificar si ya tiene valor al cargar
        if (input.value.trim() !== '') {
            input.classList.add('has-value');
        }
    });

    // Manejar el envío del formulario
    loginForm.addEventListener('submit', function(e) {
        const email = document.getElementById('login-email').value.trim();
        const password = document.getElementById('login-password').value.trim();

        // Validaciones del lado del cliente
        if (!email || !password) {
            e.preventDefault();
            showError('Por favor, complete todos los campos');
            return;
        }

        // Validar formato de email básico
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(email)) {
            e.preventDefault();
            showError('Por favor, ingrese un email válido');
            return;
        }
    });

    // Función para mostrar errores
    function showError(message) {
        if (errorMessage) {
            errorMessage.textContent = message;
            errorMessage.style.display = 'block';
        } else {
            const newError = document.createElement('div');
            newError.id = 'error-message';
            newError.style.cssText = 'color: #ff0000; background-color: #ffeeee; padding: 10px; border-radius: 5px; margin-bottom: 15px; border-left: 4px solid #ff0000; font-size: 14px;';
            newError.textContent = message;
            
            const form = document.getElementById('login-form');
            const h2 = form.querySelector('h2');
            form.insertBefore(newError, h2.nextSibling);
        }
    }

    // Limpiar mensaje de error al escribir
    loginInputs.forEach(input => {
        input.addEventListener('input', function() {
            if (errorMessage && errorMessage.style.display !== 'none') {
                errorMessage.style.display = 'none';
            }
        });
    });

    // Permitir envío con Enter
    loginInputs.forEach(input => {
        input.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                loginForm.dispatchEvent(new Event('submit'));
            }
        });
    });
}); 