-- Script adaptado a la estructura real de la base de datos
-- Basado en la estructura actual: usuarios tiene 'apellido' y 'tipo_usuario'

USE sistema_rendiciones;

-- ===== PASO 1: Verificar estructura actual =====
SELECT '=== ESTRUCTURA ACTUAL ===' as info;
SHOW TABLES;
DESCRIBE usuarios;
DESCRIBE rendiciones;

-- ===== PASO 2: Verificar si necesitamos agregar columna 'rol' a usuarios =====
-- Primero verificar si existe la columna 'rol'
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'usuarios' 
     AND COLUMN_NAME = 'rol') = 0,
    'ALTER TABLE usuarios ADD COLUMN rol VARCHAR(20) NULL AFTER tipo_usuario',
    'SELECT "rol ya existe en usuarios" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ===== PASO 3: Actualizar la columna 'rol' basada en 'tipo_usuario' =====
-- Mapear tipo_usuario a rol (asumiendo que 1=empleado, 2=aprobador1, 3=aprobador2, etc.)
UPDATE usuarios SET rol = 'empleado' WHERE tipo_usuario = 1;
UPDATE usuarios SET rol = 'aprobador1' WHERE tipo_usuario = 2;
UPDATE usuarios SET rol = 'aprobador2' WHERE tipo_usuario = 3;
UPDATE usuarios SET rol = 'admin' WHERE tipo_usuario = 4;

-- ===== PASO 4: Verificar columnas faltantes en rendiciones =====
-- Solo agregar las columnas que realmente faltan

-- Verificar si aprobador_1_id existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'aprobador_1_id') = 0,
    'ALTER TABLE rendiciones ADD COLUMN aprobador_1_id INT NULL',
    'SELECT "aprobador_1_id ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si aprobador_2_id existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'aprobador_2_id') = 0,
    'ALTER TABLE rendiciones ADD COLUMN aprobador_2_id INT NULL',
    'SELECT "aprobador_2_id ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si fecha_aprobacion_1 existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'fecha_aprobacion_1') = 0,
    'ALTER TABLE rendiciones ADD COLUMN fecha_aprobacion_1 DATETIME NULL',
    'SELECT "fecha_aprobacion_1 ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si fecha_aprobacion_2 existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'fecha_aprobacion_2') = 0,
    'ALTER TABLE rendiciones ADD COLUMN fecha_aprobacion_2 DATETIME NULL',
    'SELECT "fecha_aprobacion_2 ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si fecha_pago existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'fecha_pago') = 0,
    'ALTER TABLE rendiciones ADD COLUMN fecha_pago DATETIME NULL',
    'SELECT "fecha_pago ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si comentarios_aprobador existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'comentarios_aprobador') = 0,
    'ALTER TABLE rendiciones ADD COLUMN comentarios_aprobador TEXT NULL',
    'SELECT "comentarios_aprobador ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si aprobado_por_1 existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'aprobado_por_1') = 0,
    'ALTER TABLE rendiciones ADD COLUMN aprobado_por_1 INT NULL',
    'SELECT "aprobado_por_1 ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si aprobado_por_2 existe
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'aprobado_por_2') = 0,
    'ALTER TABLE rendiciones ADD COLUMN aprobado_por_2 INT NULL',
    'SELECT "aprobado_por_2 ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si nombre existe en rendiciones
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'nombre') = 0,
    'ALTER TABLE rendiciones ADD COLUMN nombre VARCHAR(100) NULL',
    'SELECT "nombre ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si apellidos existe en rendiciones
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'apellidos') = 0,
    'ALTER TABLE rendiciones ADD COLUMN apellidos VARCHAR(100) NULL',
    'SELECT "apellidos ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si rut existe en rendiciones
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'rut') = 0,
    'ALTER TABLE rendiciones ADD COLUMN rut VARCHAR(20) NULL',
    'SELECT "rut ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si telefono existe en rendiciones
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'telefono') = 0,
    'ALTER TABLE rendiciones ADD COLUMN telefono VARCHAR(20) NULL',
    'SELECT "telefono ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si cargo existe en rendiciones
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'cargo') = 0,
    'ALTER TABLE rendiciones ADD COLUMN cargo VARCHAR(100) NULL',
    'SELECT "cargo ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si departamento existe en rendiciones
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'rendiciones' 
     AND COLUMN_NAME = 'departamento') = 0,
    'ALTER TABLE rendiciones ADD COLUMN departamento VARCHAR(100) NULL',
    'SELECT "departamento ya existe en rendiciones" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ===== PASO 5: Crear tablas si no existen =====

-- Crear tabla archivos_adjuntos
CREATE TABLE IF NOT EXISTS archivos_adjuntos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rendicion_id INT NOT NULL,
    nombre_archivo VARCHAR(255) NOT NULL,
    ruta_archivo VARCHAR(500) NOT NULL,
    tipo_archivo VARCHAR(50) NULL,
    tamano_archivo BIGINT NULL,
    fecha_subida DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Crear tabla notificaciones
CREATE TABLE IF NOT EXISTS notificaciones (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    rendicion_id INT NULL,
    mensaje TEXT NOT NULL,
    leido BOOLEAN DEFAULT FALSE,
    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    tipo_rol VARCHAR(20) NULL
);

-- ===== PASO 6: Verificar estructura final =====
SELECT '=== ESTRUCTURA FINAL ===' as info;
DESCRIBE usuarios;
DESCRIBE rendiciones;
DESCRIBE archivos_adjuntos;
DESCRIBE notificaciones;

-- ===== PASO 7: Verificar datos =====
SELECT '=== DATOS EXISTENTES ===' as info;
SELECT COUNT(*) as total_usuarios FROM usuarios;
SELECT COUNT(*) as total_rendiciones FROM rendiciones;
SELECT COUNT(*) as total_archivos FROM archivos_adjuntos;
SELECT COUNT(*) as total_notificaciones FROM notificaciones;

-- Verificar usuarios por tipo_usuario y rol
SELECT '=== USUARIOS POR TIPO ===' as info;
SELECT tipo_usuario, COUNT(*) as cantidad FROM usuarios GROUP BY tipo_usuario;

-- Si la columna rol existe, mostrar también por rol
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'usuarios' 
     AND COLUMN_NAME = 'rol') > 0,
    'SELECT rol, COUNT(*) as cantidad FROM usuarios GROUP BY rol',
    'SELECT "Columna rol no existe aún" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT '=== ACTUALIZACIÓN COMPLETADA ===' as mensaje; 