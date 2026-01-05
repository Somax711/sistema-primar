-- Script para probar el flujo completo de notificaciones
-- Ejecutar paso a paso para verificar que las notificaciones se crean correctamente

-- PASO 1: Verificar usuarios existentes
PRINT '=== USUARIOS EXISTENTES ===';
SELECT 
    Id,
    Nombre + ' ' + Apellidos AS NombreCompleto,
    Email,
    Rol,
    Activo
FROM Usuarios 
WHERE Activo = 1
ORDER BY Rol, Nombre;

-- PASO 2: Limpiar notificaciones existentes (opcional - solo para pruebas)
-- DELETE FROM Notificaciones;
-- PRINT 'Notificaciones limpiadas';

-- PASO 3: Crear una rendición de prueba
PRINT '=== CREANDO RENDICIÓN DE PRUEBA ===';
DECLARE @UsuarioId INT = (SELECT TOP 1 Id FROM Usuarios WHERE Rol = 'empleado' AND Activo = 1);
DECLARE @NumeroTicket VARCHAR(50) = 'RND-TEST-' + CAST(YEAR(GETDATE()) AS VARCHAR) + '-' + CAST(MONTH(GETDATE()) AS VARCHAR) + '-' + CAST(DAY(GETDATE()) AS VARCHAR) + '-' + CAST(DATEPART(HOUR, GETDATE()) AS VARCHAR) + CAST(DATEPART(MINUTE, GETDATE()) AS VARCHAR);

INSERT INTO Rendiciones (
    NumeroTicket, 
    UsuarioId, 
    Titulo, 
    Descripcion, 
    MontoTotal, 
    Estado, 
    FechaCreacion,
    Nombre,
    Apellidos,
    Rut,
    Telefono,
    Cargo,
    Departamento
)
SELECT 
    @NumeroTicket,
    @UsuarioId,
    'Rendición de Prueba - Notificaciones',
    'Esta es una rendición de prueba para verificar el sistema de notificaciones',
    150000,
    'pendiente',
    GETDATE(),
    u.Nombre,
    u.Apellidos,
    u.Rut,
    u.Telefono,
    u.Cargo,
    u.Departamento
FROM Usuarios u 
WHERE u.Id = @UsuarioId;

DECLARE @RendicionId INT = SCOPE_IDENTITY();
PRINT 'Rendición creada con ID: ' + CAST(@RendicionId AS VARCHAR);

-- PASO 4: Verificar que se creó la rendición
PRINT '=== RENDICIÓN CREADA ===';
SELECT 
    Id,
    NumeroTicket,
    Titulo,
    Estado,
    FechaCreacion,
    UsuarioId
FROM Rendiciones 
WHERE Id = @RendicionId;

-- PASO 5: Simular notificación al supervisor (aprobador1)
PRINT '=== NOTIFICANDO A SUPERVISORES ===';
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
SELECT 
    u.Id,
    @RendicionId,
    'Nueva rendición ' + @NumeroTicket + ' de ' + (SELECT Nombre + ' ' + Apellidos FROM Usuarios WHERE Id = @UsuarioId) + ' requiere tu aprobación.',
    0,
    GETDATE(),
    'supervisor'
FROM Usuarios u 
WHERE u.Rol = 'aprobador1' AND u.Activo = 1;

PRINT 'Notificaciones enviadas a supervisores: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- PASO 6: Verificar notificaciones creadas
PRINT '=== NOTIFICACIONES CREADAS ===';
SELECT 
    n.Id,
    u.Nombre + ' ' + u.Apellidos AS Usuario,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.RendicionId = @RendicionId
ORDER BY n.FechaCreacion DESC;

-- PASO 7: Simular aprobación del supervisor
PRINT '=== SIMULANDO APROBACIÓN DEL SUPERVISOR ===';
UPDATE Rendiciones 
SET 
    Estado = 'aprobado_1',
    Aprobador1Id = (SELECT TOP 1 Id FROM Usuarios WHERE Rol = 'aprobador1' AND Activo = 1),
    FechaAprobacion1 = GETDATE(),
    ComentariosAprobador = 'Aprobado por supervisor - Prueba de notificaciones'
WHERE Id = @RendicionId;

-- PASO 8: Notificar al gerente (aprobador2)
PRINT '=== NOTIFICANDO A GERENTES ===';
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
SELECT 
    u.Id,
    @RendicionId,
    'Rendición ' + @NumeroTicket + ' requiere tu aprobación final.',
    0,
    GETDATE(),
    'gerente'
FROM Usuarios u 
WHERE u.Rol = 'aprobador2' AND u.Activo = 1;

-- PASO 9: Notificar al empleado
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
VALUES (
    @UsuarioId,
    @RendicionId,
    'Tu rendición ' + @NumeroTicket + ' fue aprobada por el supervisor y está pendiente de aprobación del gerente.',
    0,
    GETDATE(),
    'empleado'
);

PRINT 'Notificaciones enviadas a gerentes y empleado';

-- PASO 10: Verificar todas las notificaciones del flujo
PRINT '=== RESUMEN DE NOTIFICACIONES DEL FLUJO ===';
SELECT 
    n.TipoRol,
    u.Nombre + ' ' + u.Apellidos AS Usuario,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.RendicionId = @RendicionId
ORDER BY n.FechaCreacion DESC;

-- PASO 11: Contar notificaciones por tipo
PRINT '=== CONTEO DE NOTIFICACIONES POR TIPO ===';
SELECT 
    n.TipoRol,
    COUNT(*) AS TotalNotificaciones
FROM Notificaciones n
WHERE n.RendicionId = @RendicionId
GROUP BY n.TipoRol
ORDER BY n.TipoRol;

PRINT '=== PRUEBA COMPLETADA ===';
PRINT 'Rendición ID: ' + CAST(@RendicionId AS VARCHAR);
PRINT 'Número Ticket: ' + @NumeroTicket; 