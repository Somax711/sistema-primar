document.addEventListener('DOMContentLoaded', function() {
    const forms = {
        email: document.getElementById('email-form'),
        code: document.getElementById('code-form'),
        password: document.getElementById('password-form')
    };
    
    const steps = {
        1: document.getElementById('step1'),
        2: document.getElementById('step2'),
        3: document.getElementById('step3')
    };

    let currentEmail = '';
    let currentCode = '';

    // Función para mostrar errores
    function showError(message, formId = 'email-form') {
        const form = document.getElementById(formId);
        let errorDiv = form.querySelector('.error-message');
        
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'error-message';
            errorDiv.style.cssText = 'color: #ff0000; background-color: #ffeeee; padding: 10px; border-radius: 5px; margin-bottom: 15px; border-left: 4px solid #ff0000; font-size: 14px;';
            form.insertBefore(errorDiv, form.querySelector('h2').nextSibling);
        }
        
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
    }

    // Función para limpiar errores
    function clearError(formId = 'email-form') {
        const form = document.getElementById(formId);
        const errorDiv = form.querySelector('.error-message');
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
    }

    // Manejar inputs del código de verificación
    const codeInputs = document.querySelectorAll('.verification-code input');
    codeInputs.forEach((input, index) => {
        input.addEventListener('input', function(e) {
            // Solo permitir números
            e.target.value = e.target.value.replace(/[^0-9]/g, '');
            
            if (e.target.value.length === 1) {
                if (index < codeInputs.length - 1) {
                    codeInputs[index + 1].focus();
                }
            }
        });

        input.addEventListener('keydown', function(e) {
            if (e.key === 'Backspace' && !e.target.value && index > 0) {
                codeInputs[index - 1].focus();
            }
        });
    });

    // Función para mostrar paso
    function showStep(step) {
        Object.values(forms).forEach(form => form.style.display = 'none');
        Object.values(steps).forEach(s => s.classList.remove('active', 'completed'));
        
        const formKeys = Object.keys(forms);
        const currentFormKey = formKeys[step - 1];
        forms[currentFormKey].style.display = 'block';
        
        steps[step].classList.add('active');
        for (let i = 1; i < step; i++) {
            steps[i].classList.add('completed');
        }
    }

    // Paso 1: Enviar código de recuperación
    forms.email.addEventListener('submit', async function(e) {
        e.preventDefault();
        clearError('email-form');
        
        const email = document.getElementById('recovery-email').value.trim();
        
        if (!email) {
            showError('Por favor, ingresa tu correo electrónico', 'email-form');
            return;
        }

        const button = this.querySelector('button[type="submit"]');
        const originalText = button.textContent;
        button.disabled = true;
        button.textContent = 'Enviando...';

        try {
            const response = await fetch('/Account/EnviarCodigoRecuperacion', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `email=${encodeURIComponent(email)}`
            });

            const result = await response.json();

            if (result.success) {
                currentEmail = email;
                showStep(2);
                startResendCountdown();
            } else {
                showError(result.message || 'Ocurrió un error al enviar el código', 'email-form');
            }
        } catch (error) {
            showError('Error de conexión. Por favor, intenta nuevamente.', 'email-form');
        } finally {
            button.disabled = false;
            button.textContent = originalText;
        }
    });

    // Paso 2: Verificar código
    forms.code.addEventListener('submit', async function(e) {
        e.preventDefault();
        clearError('code-form');
        
        const code = Array.from(codeInputs).map(input => input.value).join('');
        
        if (code.length !== 6) {
            showError('Por favor, ingresa el código completo de 6 dígitos', 'code-form');
            return;
        }

        const button = this.querySelector('button[type="submit"]');
        const originalText = button.textContent;
        button.disabled = true;
        button.textContent = 'Verificando...';

        try {
            const response = await fetch('/Account/VerificarCodigo', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `email=${encodeURIComponent(currentEmail)}&codigo=${encodeURIComponent(code)}`
            });

            const result = await response.json();

            if (result.success) {
                currentCode = code;
                showStep(3);
            } else {
                showError(result.message || 'Código inválido', 'code-form');
                // Limpiar inputs del código
                codeInputs.forEach(input => input.value = '');
                codeInputs[0].focus();
            }
        } catch (error) {
            showError('Error de conexión. Por favor, intenta nuevamente.', 'code-form');
        } finally {
            button.disabled = false;
            button.textContent = originalText;
        }
    });

    // Paso 3: Cambiar contraseña
    forms.password.addEventListener('submit', async function(e) {
        e.preventDefault();
        clearError('password-form');
        
        const newPassword = document.getElementById('new-password').value;
        const confirmPassword = document.getElementById('confirm-password').value;
        
        if (!newPassword || !confirmPassword) {
            showError('Por favor, completa todos los campos', 'password-form');
            return;
        }
        
        if (newPassword !== confirmPassword) {
            showError('Las contraseñas no coinciden', 'password-form');
            return;
        }

        if (newPassword.length < 6) {
            showError('La contraseña debe tener al menos 6 caracteres', 'password-form');
            return;
        }

        const button = this.querySelector('button[type="submit"]');
        const originalText = button.textContent;
        button.disabled = true;
        button.textContent = 'Cambiando...';

        try {
            const response = await fetch('/Account/CambiarContrasena', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `email=${encodeURIComponent(currentEmail)}&codigo=${encodeURIComponent(currentCode)}&nuevaContrasena=${encodeURIComponent(newPassword)}`
            });

            const result = await response.json();

            if (result.success) {
                alert('Contraseña cambiada exitosamente. Serás redirigido al inicio de sesión.');
                window.location.href = '/Account/Login';
            } else {
                showError(result.message || 'Error al cambiar la contraseña', 'password-form');
            }
        } catch (error) {
            showError('Error de conexión. Por favor, intenta nuevamente.', 'password-form');
        } finally {
            button.disabled = false;
            button.textContent = originalText;
        }
    });

    // Manejar reenvío de código
    let countdown = 60;
    let countdownInterval;
    const countdownElement = document.getElementById('countdown');
    const resendLink = document.getElementById('resend-code');
    
    function startResendCountdown() {
        countdown = 60;
        resendLink.style.display = 'none';
        countdownElement.style.display = 'block';
        
        countdownInterval = setInterval(() => {
            countdownElement.textContent = `Podrás solicitar un nuevo código en ${countdown} segundos`;
            countdown--;
            
            if (countdown < 0) {
                clearInterval(countdownInterval);
                resendLink.style.display = 'inline';
                countdownElement.style.display = 'none';
            }
        }, 1000);
    }
    
    resendLink.addEventListener('click', async function(e) {
        e.preventDefault();
        
        if (countdown > 0) return;
        
        const originalText = this.textContent;
        this.textContent = 'Enviando...';
        this.style.pointerEvents = 'none';

        try {
            const response = await fetch('/Account/EnviarCodigoRecuperacion', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `email=${encodeURIComponent(currentEmail)}`
            });

            const result = await response.json();

            if (result.success) {
                startResendCountdown();
            } else {
                showError(result.message || 'Error al reenviar el código', 'code-form');
            }
        } catch (error) {
            showError('Error de conexión. Por favor, intenta nuevamente.', 'code-form');
        } finally {
            this.textContent = originalText;
            this.style.pointerEvents = 'auto';
        }
    });

    // Limpiar errores al escribir
    document.getElementById('recovery-email').addEventListener('input', function() {
        clearError('email-form');
    });

    codeInputs.forEach(input => {
        input.addEventListener('input', function() {
            clearError('code-form');
        });
    });

    document.getElementById('new-password').addEventListener('input', function() {
        clearError('password-form');
    });

    document.getElementById('confirm-password').addEventListener('input', function() {
        clearError('password-form');
    });
}); 