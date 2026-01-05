-- Script para actualizar la base de datos con los nuevos campos de información personal
-- Ejecutar este script en MySQL Workbench o en la línea de comandos de MySQL

USE rendiciones_primar;

-- Verificar si las columnas ya existen para evitar errores
SET @sql = '';

-- Agregar nombre_completo si no existe
SELECT COUNT(*) INTO @exists FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'rendiciones_primar' AND TABLE_NAME = 'usuarios' AND COLUMN_NAME = 'nombre_completo';
IF @exists = 0 THEN
    SET @sql = CONCAT(@sql, 'ALTER TABLE usuarios ADD COLUMN nombre_completo VARCHAR(100) NOT NULL DEFAULT "" AFTER nombre;');
END IF;

-- Agregar rut si no existe
SELECT COUNT(*) INTO @exists FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'rendiciones_primar' AND TABLE_NAME = 'usuarios' AND COLUMN_NAME = 'rut';
IF @exists = 0 THEN
    SET @sql = CONCAT(@sql, 'ALTER TABLE usuarios ADD COLUMN rut VARCHAR(20) NOT NULL DEFAULT "" AFTER nombre_completo;');
END IF;

-- Agregar telefono si no existe
SELECT COUNT(*) INTO @exists FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'rendiciones_primar' AND TABLE_NAME = 'usuarios' AND COLUMN_NAME = 'telefono';
IF @exists = 0 THEN
    SET @sql = CONCAT(@sql, 'ALTER TABLE usuarios ADD COLUMN telefono VARCHAR(20) NULL AFTER email;');
END IF;

-- Agregar cargo si no existe
SELECT COUNT(*) INTO @exists FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'rendiciones_primar' AND TABLE_NAME = 'usuarios' AND COLUMN_NAME = 'cargo';
IF @exists = 0 THEN
    SET @sql = CONCAT(@sql, 'ALTER TABLE usuarios ADD COLUMN cargo VARCHAR(100) NULL AFTER rol;');
END IF;

-- Agregar departamento si no existe
SELECT COUNT(*) INTO @exists FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'rendiciones_primar' AND TABLE_NAME = 'usuarios' AND COLUMN_NAME = 'departamento';
IF @exists = 0 THEN
    SET @sql = CONCAT(@sql, 'ALTER TABLE usuarios ADD COLUMN departamento VARCHAR(100) NULL AFTER cargo;');
END IF;

-- Ejecutar las alteraciones si hay algo que agregar
IF LENGTH(@sql) > 0 THEN
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    SELECT 'Columnas agregadas exitosamente' AS Resultado;
ELSE
    SELECT 'Todas las columnas ya existen' AS Resultado;
END IF;

-- Actualizar registros existentes con valores por defecto
UPDATE usuarios SET 
nombre_completo = nombre,
rut = '00000000-0'
WHERE nombre_completo = '' OR rut = '';

-- Verificar la estructura final de la tabla
DESCRIBE usuarios;

-- Mostrar algunos registros de ejemplo
SELECT id, nombre, nombre_completo, rut, email, telefono, cargo, departamento FROM usuarios LIMIT 5; 