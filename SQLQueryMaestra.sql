-- CREO BASE
CREATE DATABASE DP;
GO

USE DP;
GO

-- CREACION DE TABLAS NECESARIAS 
CREATE TABLE Salas (
    IdSala INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(100) NOT NULL,
    Ubicacion VARCHAR(100) NOT NULL,
    Capacidad INT NOT NULL,
    HoraInicio TIME NOT NULL, -- Hora de inicio de disponibilidad
    HoraFin TIME NOT NULL     -- Hora de fin de disponibilidad
);

CREATE TABLE users (
    IdUsuario INT PRIMARY KEY IDENTITY(1,1),
    Email VARCHAR(255) NOT NULL,
    Contraseña VARCHAR(255) NOT NULL,
    TipoUser int NOT NULL
);


CREATE TABLE Reservas (
    IdReserva INT IDENTITY(1,1) PRIMARY KEY,     
    IdSala INT NOT NULL,                      
    HoraInicio TIME NOT NULL,                
    HoraFin TIME NOT NULL,                       
    NumeroAsistentes INT NOT NULL,              
    Prioridad INT NULL,                          
    IdUsuario INT NOT NULL,                     
    CONSTRAINT FK_Reservas_Salas FOREIGN KEY (IdSala) REFERENCES Salas(IdSala),  -- Clave foránea con la tabla de Salas
    CONSTRAINT FK_Reservas_Usuarios FOREIGN KEY (IdUsuario) REFERENCES users(IdUsuario)  
);


CREATE TABLE Notificaciones (
    IdNotificacion INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT,
    Mensaje VARCHAR(255),
    Leida BIT DEFAULT 0, -- 0: no leída, 1: leída
    FechaCreacion DATETIME DEFAULT GETDATE()
);
GO




   -- A PARTIR DE AQUI ESTAN MIS PROCEDIMIENTO ALMACENADOS
   -- ESTAN PUESTOS EN EL ORDEN QUE LOS TENGO GUARDADOS, NO EN EL ORDEN QUE FUERON CREADOS


   -- BUSCAR SALAS CERCANAS
CREATE PROCEDURE BuscarSalasCercanas
   @Ubicacion VARCHAR(100),
    @HoraInicio TIME,
    @HoraFin TIME,
    @NumeroAsistentes INT,
    @SalaAsignada INT OUTPUT,
    @mensaje VARCHAR(100) OUTPUT
AS
BEGIN
    DECLARE @NumeroPiso INT;
    
    -- Extraer el número de piso de la ubicación solicitada
    SET @NumeroPiso = CAST(SUBSTRING(@Ubicacion, PATINDEX('%[0-9]%', @Ubicacion), LEN(@Ubicacion)) AS INT);

    -- Buscar una sala alternativa en el mismo piso o pisos adyacentes
    SELECT TOP 1 
        @SalaAsignada = s.IdSala
    FROM Salas s
    WHERE CAST(SUBSTRING(s.Ubicacion, PATINDEX('%[0-9]%', s.Ubicacion), LEN(s.Ubicacion)) AS INT) 
          BETWEEN @NumeroPiso - 1 AND @NumeroPiso + 1
    AND s.Capacidad >= @NumeroAsistentes 
    AND NOT EXISTS (
        SELECT 1
        FROM Reservas r
        WHERE r.IdSala = s.IdSala
        AND (@HoraInicio < r.HoraFin AND @HoraFin > r.HoraInicio)
    )
    ORDER BY s.Ubicacion ASC, s.Capacidad ASC;

    IF @SalaAsignada IS NOT NULL
    BEGIN
        SET @mensaje = 'Se ha asignado una sala alternativa.';
    END
    ELSE
    BEGIN
        SET @SalaAsignada = NULL;
        SET @mensaje = 'No hay salas disponibles cercanas con la capacidad adecuada.';
    END
END;
GO
GO
CREATE PROCEDURE VerReservas
    @IdSala INT
AS
BEGIN
    SELECT IdReserva, IdSala, HoraInicio, HoraFin, NumeroAsistentes, Prioridad, IdUsuario, IdReservaGrupo
    FROM Reservas
    WHERE IdSala = @IdSala
    ORDER BY HoraInicio;
END;
GO
CREATE PROCEDURE EliminarReserva
    @IdReserva INT
AS
BEGIN
    BEGIN TRY
        -- Iniciar una transacción
        BEGIN TRANSACTION;

        DECLARE @IdReservaGrupo INT;

        -- OBTENER EL ID DEL GRUPO DE RESERVA
        SELECT @IdReservaGrupo = IdReservaGrupo
        FROM Reservas
        WHERE IdReserva = @IdReserva;

        -- SI LA RESERVA ES PARTE DE UN GRUPO
        IF @IdReservaGrupo IS NOT NULL
        BEGIN
            -- ELIMINAR TODAS LAS RESERVAS ASOCIADAS AL GRUPO
            DELETE FROM Reservas
            WHERE IdReservaGrupo = @IdReservaGrupo;
        END
        ELSE
        BEGIN
            -- ELIMINAR RESERVA INDIVIDUAL
            DELETE FROM Reservas
            WHERE IdReserva = @IdReserva;
        END

        -- Confirmar transacción
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- Manejar errores
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        -- Opcional: retornar el mensaje de error
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT @ErrorMessage;
    END CATCH
END;
GO
-- ASIGNAR RESERVAS ALGORITMO
GO
CREATE PROCEDURE AsignacionReservas
    @IdSala INT,
    @HoraInicio TIME,
    @HoraFin TIME,
    @NumeroAsistentes INT,
    @IdUsuario INT,  -- Nuevo parámetro
    @SalaAsignada INT OUTPUT,
    @mensaje VARCHAR(100) OUTPUT
AS
BEGIN
    DECLARE @Ubicacion VARCHAR(20), @CapacidadSala INT;
    DECLARE @NuevaHoraInicio TIME = @HoraInicio;
    DECLARE @NuevaHoraFin TIME = @HoraFin;
    DECLARE @IntervaloMinutos INT = 30; -- Tiempo en minutos para incrementar en la búsqueda de una franja libre
    DECLARE @MaxIteraciones INT = 10; -- Límite para cuántas veces intentar buscar un horario disponible

    -- Obtener la ubicación y capacidad de la sala solicitada
    SELECT @Ubicacion = s.Ubicacion, @CapacidadSala = s.Capacidad
    FROM Salas s
    WHERE s.IdSala = @IdSala;

    DECLARE @Iteraciones INT = 0;

    WHILE @Iteraciones < @MaxIteraciones
    BEGIN
        -- Verificar si la sala solicitada está ocupada en el horario deseado
        IF EXISTS (
            SELECT 1
            FROM Reservas r
            WHERE r.IdSala = @IdSala
            AND (@NuevaHoraInicio < r.HoraFin AND @NuevaHoraFin > r.HoraInicio)
        )
        BEGIN
            -- Intentar buscar una sala cercana en caso de conflicto de horario
            EXEC BuscarSalasCercanas @Ubicacion, @NumeroAsistentes, @NuevaHoraInicio, @NuevaHoraFin, @SalaAsignada OUTPUT, @mensaje OUTPUT;

            IF @SalaAsignada IS NOT NULL
            BEGIN
                -- Si se encontró una sala cercana y disponible, salir del ciclo
                SET @mensaje = 'Se ha asignado una sala alternativa disponible.';
                RETURN;
            END

            -- Si no hay sala cercana, mover la hora de inicio y fin
            SET @NuevaHoraInicio = DATEADD(MINUTE, @IntervaloMinutos, @NuevaHoraInicio);
            SET @NuevaHoraFin = DATEADD(MINUTE, @IntervaloMinutos, @NuevaHoraFin);

            -- Incrementar el número de iteraciones para evitar un ciclo infinito
            SET @Iteraciones = @Iteraciones + 1;
        END
        ELSE
        BEGIN
            -- Si la sala solicitada está disponible, asignar la misma sala
            SET @SalaAsignada = @IdSala;
            SET @mensaje = 'La sala solicitada está disponible en un nuevo horario: ' 
                            + CONVERT(VARCHAR, @NuevaHoraInicio, 108) + ' - ' 
                            + CONVERT(VARCHAR, @NuevaHoraFin, 108);
							
            INSERT INTO Reservas (IdSala, HoraInicio, HoraFin, NumeroAsistentes, Prioridad, IdUsuario)
            VALUES (@IdSala, @NuevaHoraInicio, @NuevaHoraFin, @NumeroAsistentes, 1, @IdUsuario);
            RETURN;
        END
    END

    -- Si no se encuentra ningún horario disponible dentro del límite de iteraciones
    SET @mensaje = 'No se encontró un horario disponible dentro del rango de búsqueda.';
    SET @SalaAsignada = NULL;
END;
GO
GO
CREATE PROCEDURE CrearReserva
    @Eliminada BIT = 0, -- Parámetro de entrada, se inicializa a 0 por defecto
     @IdSala INT, -- Para la reserva simple, o el ID de la sala principal
    @HoraInicio TIME,
    @HoraFin TIME,
    @NumeroAsistentes INT,
    @Prioridad INT = NULL,
    @IdUsuario INT,
    @MultiSala BIT = 0, -- Indica si es una reserva múltiple
    @ListaSalas VARCHAR(100) = NULL, -- Lista de IDs de salas para reservas múltiples
    @registrado BIT OUTPUT,
    @mensaje VARCHAR(100) OUTPUT
AS
BEGIN
    BEGIN TRY
        -- TRANSACCIÓN
        BEGIN TRANSACTION;

        DECLARE @tipoUser INT, @CapacidadSala INT, @IdReservaExistente INT, @tipoUserExistente INT, @IdUsuarioReservaExistente INT;
        DECLARE @IdReservaGrupo INT = NULL; -- Variable para el IdReservaGrupo

        -- OBTENER EL TIPO DE USUARIO
        SELECT @tipoUser = tipoUser 
        FROM Users 
        WHERE IdUsuario = @IdUsuario;

        -- OBTENER LA CAPACIDAD DE LA SALA
        SELECT @CapacidadSala = Capacidad
        FROM Salas 
        WHERE IdSala = @IdSala;

        IF @NumeroAsistentes > @CapacidadSala
        BEGIN
            SET @registrado = 0;
            SET @mensaje = 'El número de asistentes excede la capacidad de la sala.';
            RETURN;
        END

 -- COMPROBAR CONFLICTOS DE HORARIOS PARA RESERVA SIMPLE
IF @MultiSala = 0
BEGIN

            DECLARE @SalaActual INT, @SalaDisponible BIT;
    IF EXISTS (
        SELECT 1
        FROM Reservas r
        WHERE r.IdSala = @IdSala
        AND (@HoraInicio < r.HoraFin AND @HoraFin > r.HoraInicio)
    )
    BEGIN
        -- VER EL TIPO DE USUARIO
        SELECT @IdReservaExistente = r.IdReserva, 
               @IdUsuarioReservaExistente = r.IdUsuario, 
               @tipoUserExistente = u.tipoUser
        FROM Reservas r
        JOIN Users u ON r.IdUsuario = u.IdUsuario
        WHERE r.IdSala = @IdSala
        AND (@HoraInicio < r.HoraFin AND @HoraFin > r.HoraInicio);

        -- VERIFICAR LA MAYOR PRIORIDAD
        IF @tipoUser = 1 AND @tipoUserExistente = 0
        BEGIN
            -- ELIMINAR RESERVA EXISTENTE
            EXEC EliminarReserva @IdReservaExistente;
			SET @Eliminada = 1;

            -- NOTIFICAR AL USUARIO CUYA RESERVA SE ELIMINA
            DECLARE @MensajeNotificacion VARCHAR(255);
            SET @MensajeNotificacion = 'Tu reserva en la sala ' + CAST(@IdSala AS VARCHAR(10)) + ' fue eliminada debido a una nueva reserva de mayor prioridad.';
            EXEC dbo.InsertarNotificacion @IdUsuarioReservaExistente, @MensajeNotificacion;
			

        END
        ELSE
        BEGIN
            SET @registrado = 0;
            SET @mensaje = 'La sala ya está reservada en el horario solicitado y no se puede eliminar.';
            RETURN;
        END
    END
END


        -- VERIFICAR RESERVA MÚLTIPLE
        IF @MultiSala = 1 AND @ListaSalas IS NOT NULL
        BEGIN
            SET @SalaDisponible = 1;

            -- TODAS LAS SALAS SON VALIDAS?
            DECLARE sala_cursor CURSOR FOR
            SELECT value FROM STRING_SPLIT(@ListaSalas, ',');

            OPEN sala_cursor;
            FETCH NEXT FROM sala_cursor INTO @SalaActual;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM Reservas r
                    WHERE r.IdSala = @SalaActual
                    AND (@HoraInicio < r.HoraFin AND @HoraFin > r.HoraInicio)
                )
                BEGIN
                    -- VERIFICAR PRIORIDAD Y USUARIO
                    SELECT @IdReservaExistente = r.IdReserva, @IdUsuarioReservaExistente = r.IdUsuario, @tipoUserExistente = u.tipoUser
                    FROM Reservas r
                    JOIN Users u ON r.IdUsuario = u.IdUsuario
                    WHERE r.IdSala = @SalaActual
                    AND (@HoraInicio < r.HoraFin AND @HoraFin > r.HoraInicio);

                    IF @tipoUser = 1 AND @tipoUserExistente = 0
                    BEGIN
                        -- ELIMINAR RESERVA EXISTENTE
                        EXEC EliminarReserva @IdReservaExistente;  
						SET @Eliminada = 1;


                        -- NOTIFICAR AL USUARIO CUYA RESERVA SE ELIMINA
                        SET @MensajeNotificacion = 'Tu reserva en la sala ' + CAST(@SalaActual AS VARCHAR(10)) + ' fue eliminada debido a una nueva reserva de mayor prioridad.';
                        EXEC dbo.InsertarNotificacion @IdUsuarioReservaExistente, @MensajeNotificacion;

                    END
                    ELSE
                    BEGIN
                        SET @SalaDisponible = 0;
                        BREAK;
                    END
                END

                FETCH NEXT FROM sala_cursor INTO @SalaActual;
            END
            CLOSE sala_cursor;
            DEALLOCATE sala_cursor;

            IF @SalaDisponible = 0
            BEGIN
                SET @registrado = 0;
                SET @mensaje = 'Una o más salas solicitadas no están disponibles en el horario especificado.';
                ROLLBACK TRANSACTION; -- REVERTIR
                RETURN;
            END
        END

        -- CREAR RESERVAS
        IF @MultiSala = 1 AND @ListaSalas IS NOT NULL
        BEGIN
            -- ASIGNAR UN ID PARA LA RESERVA MULTIPLE
            SELECT @IdReservaGrupo = ISNULL(MAX(IdReservaGrupo), 0) + 1 FROM Reservas;

            DECLARE sala_cursor CURSOR FOR
            SELECT value FROM STRING_SPLIT(@ListaSalas, ',');

            OPEN sala_cursor;
            FETCH NEXT FROM sala_cursor INTO @SalaActual;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                INSERT INTO Reservas (IdSala, HoraInicio, HoraFin, NumeroAsistentes, Prioridad, IdUsuario, IdReservaGrupo)
                VALUES (@SalaActual, @HoraInicio, @HoraFin, @NumeroAsistentes, @Prioridad, @IdUsuario, @IdReservaGrupo);

                FETCH NEXT FROM sala_cursor INTO @SalaActual;
            END
			
                DECLARE @SalaAsignada INT, @MensajeAsignacion VARCHAR(100);


            CLOSE sala_cursor;
            DEALLOCATE sala_cursor;

            SET @registrado = 1;
            SET @mensaje = 'Reservas múltiples registradas con éxito.';
        END
        ELSE
        BEGIN
            -- RESERVA SIMPLE, NO SE NECESITA IdReservaGrupo
            INSERT INTO Reservas (IdSala, HoraInicio, HoraFin, NumeroAsistentes, Prioridad, IdUsuario)
            VALUES (@IdSala, @HoraInicio, @HoraFin, @NumeroAsistentes, @Prioridad, @IdUsuario);

            SET @registrado = 1;
            SET @mensaje = 'Reserva registrada con éxito.';
        END

        -- CONFIRMAR
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- SI FALLA, HACER ROLLBACK
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        -- MANEJO DEL ERROR
        SET @registrado = 0;
        SET @mensaje = 'Ocurrió un error durante el registro de la reserva';
    END CATCH
	END;
GO
GO
CREATE PROCEDURE InsertarNotificacion
    @IdUsuario INT,
    @Mensaje VARCHAR(255)
AS
BEGIN
    INSERT INTO Notificaciones (IdUsuario, Mensaje, Leida)
    VALUES (@IdUsuario, @Mensaje, 0);  -- Se inserta como no leída (Leida = 0)
END;
GO
GO
CREATE PROCEDURE ObtenerNotificacionesNoLeidas
    @IdUsuario INT
AS
BEGIN
    SELECT IdNotificacion, Mensaje, FechaCreacion
    FROM Notificaciones
    WHERE IdUsuario = @IdUsuario
    AND Leida = 0
    ORDER BY FechaCreacion DESC;
END;
GO
GO
CREATE PROCEDURE MarcarNotificacionComoLeida
    @IdNotificacion INT
AS
BEGIN
    UPDATE Notificaciones
    SET Leida = 1
    WHERE IdNotificacion = @IdNotificacion;
END;
GO

-- CREE LAS SALAS DIRECTO DESDE LA BASE DE DATOS
GO
INSERT INTO Salas (Nombre, Ubicacion, Capacidad, HoraInicio, HoraFin) 
VALUES 
('Sala 1', 'Primer Piso', 10, '08:00', '17:00'),
('Sala 2', 'Segundo Piso', 10, '08:00', '17:00'),
('Sala 3', 'Segundo Piso', 20, '08:00', '17:00'),
('Sala 4', 'Primer Piso', 30, '08:00', '17:00'),
('Sala 5', 'Tercer Piso', 10, '08:00', '17:00'),
('Sala 6', 'Tercer Piso', 15, '08:00', '17:00'),
('Sala 7', 'Primer Piso', 25, '08:00', '17:00');
-- Aqui tengo mis salas creadas

GO
CREATE PROCEDURE ObtenerSalas
AS
BEGIN
    SELECT IdSala, Nombre, Ubicacion, Capacidad, HoraInicio, HoraFin
    FROM Salas;
END;
GO
GO
CREATE PROCEDURE RegistrarUsuario
    @tipoUser INT,
    @email VARCHAR(100),
    @contraseña VARCHAR(50),
    @registrado BIT OUTPUT,
    @mensaje VARCHAR(100) OUTPUT
AS
BEGIN
    -- Iniciar manejo de transacciones
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Verificar si el correo ya existe
        IF NOT EXISTS (SELECT 1 FROM users WHERE email = @email)
        BEGIN
            -- Insertar nuevo usuario
            INSERT INTO users (email, contraseña, tipoUser) 
            VALUES (@email, @contraseña, @tipoUser);
            
            -- Establecer valores de salida
            SET @registrado = 1;
            SET @mensaje = 'Usuario registrado con éxito';

            -- Confirmar transacción
            COMMIT TRANSACTION;
        END
        ELSE
        BEGIN
            -- Si el usuario ya existe
            SET @registrado = 0;
            SET @mensaje = 'Ya existe el usuario';
        END
    END TRY
    BEGIN CATCH
        -- Manejo de errores, revertir la transacción en caso de fallo
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        -- Mensaje de error
        SET @registrado = 0;
        SET @mensaje = 'Ocurrió un error durante el registro';
    END CATCH
END;
GO
GO
create proc validarUser(
    @email VARCHAR(100),
    @contraseña VARCHAR(50)
	)
	as 
	begin
	if (exists(select * from users where email = @email and contraseña = @contraseña))
	select IdUsuario from users where email = @email and contraseña = @contraseña
	else
	select '0'
end
GO
GO
CREATE PROCEDURE ObtenerUsuario
    @Email NVARCHAR(100),
    @Contraseña NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    -- Seleccionar el usuario basado en el email y contraseña
    SELECT IdUsuario, email, contraseña
    FROM Users
    WHERE email = @Email AND contraseña = @Contraseña;
END;
