-- Script para crear usuarios de prueba
-- Ejecutar este script en la base de datos MySQL

-- Deshabilitar modo seguro temporalmente
SET SQL_SAFE_UPDATES = 0;

-- Crear Camila Flores (Aprobador 1)
INSERT IGNORE INTO usuarios (nombre, nombre_completo, rut, email, password_hash, rol, cargo, departamento, activo, fecha_creacion) 
VALUES (
    'Camila Flores',
    'Camila Flores',
    '12.345.678-9',
    'camila.flores@primar.cl',
    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', -- password: password
    'aprobador1',
    'Supervisora de Finanzas',
    'Finanzas',
    1,
    NOW()
);

-- Crear Don Juan (Aprobador 2)
INSERT IGNORE INTO usuarios (nombre, nombre_completo, rut, email, password_hash, rol, cargo, departamento, activo, fecha_creacion) 
VALUES (
    'Don Juan',
    'Juan Pérez',
    '98.765.432-1',
    'don.juan@primar.cl',
    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', -- password: password
    'aprobador2',
    'Gerente General',
    'Dirección',
    1,
    NOW()
);

-- Crear un empleado de prueba
INSERT IGNORE INTO usuarios (nombre, nombre_completo, rut, email, password_hash, rol, cargo, departamento, activo, fecha_creacion) 
VALUES (
    'Catalina Núñez',
    'Catalina Núñez',
    '11.111.111-1',
    'catalina.nunez@primar.cl',
    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', -- password: password
    'empleado',
    'Analista',
    'Operaciones',
    1,
    NOW()
);

-- Habilitar modo seguro nuevamente
SET SQL_SAFE_UPDATES = 1;

-- Verificar usuarios creados
SELECT id, nombre, email, rol, cargo, departamento FROM usuarios ORDER BY id; 