-- Script para verificar la estructura real de las tablas
-- Ejecutar en MySQL para ver los nombres correctos de las columnas

USE sistema_rendiciones;

-- 1. Verificar estructura de la tabla Usuarios
SELECT '=== ESTRUCTURA TABLA USUARIOS ===' AS Info;
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'sistema_rendiciones' 
AND TABLE_NAME = 'Usuarios'
ORDER BY ORDINAL_POSITION;

-- 2. Verificar estructura de la tabla Notificaciones
SELECT '=== ESTRUCTURA TABLA NOTIFICACIONES ===' AS Info;
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'sistema_rendiciones' 
AND TABLE_NAME = 'Notificaciones'
ORDER BY ORDINAL_POSITION;

-- 3. Verificar estructura de la tabla Rendiciones
SELECT '=== ESTRUCTURA TABLA RENDICIONES ===' AS Info;
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'sistema_rendiciones' 
AND TABLE_NAME = 'Rendiciones'
ORDER BY ORDINAL_POSITION;

-- 4. Ver algunos registros de ejemplo de Usuarios
SELECT '=== EJEMPLO DE REGISTROS USUARIOS ===' AS Info;
SELECT * FROM Usuarios LIMIT 3;

-- 5. Ver algunos registros de ejemplo de Notificaciones
SELECT '=== EJEMPLO DE REGISTROS NOTIFICACIONES ===' AS Info;
SELECT * FROM Notificaciones LIMIT 3;

-- 6. Ver algunos registros de ejemplo de Rendiciones
SELECT '=== EJEMPLO DE REGISTROS RENDICIONES ===' AS Info;
SELECT * FROM Rendiciones LIMIT 3; 