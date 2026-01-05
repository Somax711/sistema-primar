-- Script para configurar a Camila como administradora del sistema
-- Ejecutar este script en la base de datos MySQL

USE rendiciones_primar;

-- Verificar si Camila existe y actualizar su rol si es necesario
UPDATE usuarios 
SET rol = 'aprobador1',
    cargo = 'Supervisora de Finanzas',
    departamento = 'Finanzas',
    activo = 1
WHERE email = 'camila.flores@primar.cl';

-- Si Camila no existe, crearla
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

-- Verificar que la actualización fue exitosa
SELECT id, nombre, email, rol, cargo, departamento, activo 
FROM usuarios 
WHERE email = 'camila.flores@primar.cl';

-- Mostrar todos los usuarios activos
SELECT id, nombre, email, rol, cargo, departamento, activo 
FROM usuarios 
WHERE activo = 1 
ORDER BY nombre; 