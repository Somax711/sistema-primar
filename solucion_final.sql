-- Solución final para el problema de eliminación de rendiciones
-- Ejecutar en MySQL

USE rendiciones_primar;

-- Agregar restricción CASCADE para notificaciones
ALTER TABLE notificaciones 
ADD CONSTRAINT fk_notificaciones_rendicion_cascade 
FOREIGN KEY (rendicion_id) REFERENCES rendiciones(id) 
ON DELETE CASCADE;

-- Agregar restricción CASCADE para archivos_adjuntos
ALTER TABLE archivos_adjuntos 
ADD CONSTRAINT fk_archivos_adjuntos_rendicion_cascade 
FOREIGN KEY (rendicion_id) REFERENCES rendiciones(id) 
ON DELETE CASCADE;

-- Verificar que se agregaron
SHOW CREATE TABLE notificaciones;
SHOW CREATE TABLE archivos_adjuntos; 