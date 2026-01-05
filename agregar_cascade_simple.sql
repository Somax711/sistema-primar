-- Script simple para agregar restricciones CASCADE
-- Ejecutar en MySQL

USE rendiciones_primar;

-- 1. Ver restricciones actuales
SELECT 
    TABLE_NAME,
    CONSTRAINT_NAME,
    REFERENCED_TABLE_NAME,
    DELETE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS 
WHERE CONSTRAINT_SCHEMA = 'rendiciones_primar' 
AND TABLE_NAME IN ('notificaciones', 'archivos_adjuntos');

-- 2. Agregar restricciones CASCADE (ignorar errores si ya existen)
-- Para notificaciones
ALTER TABLE notificaciones 
ADD CONSTRAINT fk_notificaciones_rendicion_cascade 
FOREIGN KEY (rendicion_id) REFERENCES rendiciones(id) 
ON DELETE CASCADE;

-- Para archivos_adjuntos
ALTER TABLE archivos_adjuntos 
ADD CONSTRAINT fk_archivos_adjuntos_rendicion_cascade 
FOREIGN KEY (rendicion_id) REFERENCES rendiciones(id) 
ON DELETE CASCADE;

-- 3. Verificar que se agregaron
SELECT 
    TABLE_NAME,
    CONSTRAINT_NAME,
    REFERENCED_TABLE_NAME,
    DELETE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS 
WHERE CONSTRAINT_SCHEMA = 'rendiciones_primar' 
AND TABLE_NAME IN ('notificaciones', 'archivos_adjuntos'); 