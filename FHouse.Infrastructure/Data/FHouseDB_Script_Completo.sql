USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FHouseDB')
BEGIN
    CREATE DATABASE [FHouseDB];
END
GO

USE [FHouseDB];
GO

-- Limpiar tablas si existen (respetando orden de dependencias)
IF OBJECT_ID(N'[dbo].[AuditLog]', 'U') IS NOT NULL DROP TABLE [dbo].[AuditLog];
IF OBJECT_ID(N'[dbo].[Transaccion]', 'U') IS NOT NULL DROP TABLE [dbo].[Transaccion];
IF OBJECT_ID(N'[dbo].[Presupuesto]', 'U') IS NOT NULL DROP TABLE [dbo].[Presupuesto];
IF OBJECT_ID(N'[dbo].[Cuenta]', 'U') IS NOT NULL DROP TABLE [dbo].[Cuenta];
IF OBJECT_ID(N'[dbo].[FuenteIngreso]', 'U') IS NOT NULL DROP TABLE [dbo].[FuenteIngreso];
IF OBJECT_ID(N'[dbo].[Categoria]', 'U') IS NOT NULL DROP TABLE [dbo].[Categoria];
IF OBJECT_ID(N'[dbo].[TasaCambio]', 'U') IS NOT NULL DROP TABLE [dbo].[TasaCambio];
IF OBJECT_ID(N'[dbo].[UsuarioFamilia]', 'U') IS NOT NULL DROP TABLE [dbo].[UsuarioFamilia];
IF OBJECT_ID(N'[dbo].[Familia]', 'U') IS NOT NULL DROP TABLE [dbo].[Familia];
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', 'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserRoles];
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', 'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserClaims];
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', 'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserLogins];
IF OBJECT_ID(N'[dbo].[AspNetUsers]', 'U') IS NOT NULL DROP TABLE [dbo].[AspNetUsers];
IF OBJECT_ID(N'[dbo].[AspNetRoles]', 'U') IS NOT NULL DROP TABLE [dbo].[AspNetRoles];
GO

-- 1. AspNetRoles
CREATE TABLE [dbo].[AspNetRoles](
    [Id] [nvarchar](128) NOT NULL,
    [Name] [nvarchar](256) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 2. AspNetUsers
CREATE TABLE [dbo].[AspNetUsers](
    [Id] [nvarchar](128) NOT NULL,
    [NombreCompleto] [nvarchar](150) NULL,
    [AvatarUrl] [nvarchar](500) NULL,
    [FechaRegistro] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [UltimoAcceso] [datetime] NULL,
    [FamiliaActualId] [int] NULL,
    [Email] [nvarchar](256) NULL,
    [EmailConfirmed] [bit] NOT NULL DEFAULT(0),
    [PasswordHash] [nvarchar](max) NULL,
    [SecurityStamp] [nvarchar](max) NULL,
    [PhoneNumber] [nvarchar](max) NULL,
    [PhoneNumberConfirmed] [bit] NOT NULL DEFAULT(0),
    [TwoFactorEnabled] [bit] NOT NULL DEFAULT(0),
    [LockoutEndDateUtc] [datetime] NULL,
    [LockoutEnabled] [bit] NOT NULL DEFAULT(0),
    [AccessFailedCount] [int] NOT NULL DEFAULT(0),
    [UserName] [nvarchar](256) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 3. AspNetUserClaims
CREATE TABLE [dbo].[AspNetUserClaims](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [UserId] [nvarchar](128) NOT NULL,
    [ClaimType] [nvarchar](max) NULL,
    [ClaimValue] [nvarchar](max) NULL,
    CONSTRAINT [PK_dbo.AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

-- 4. AspNetUserLogins
CREATE TABLE [dbo].[AspNetUserLogins](
    [LoginProvider] [nvarchar](128) NOT NULL,
    [ProviderKey] [nvarchar](128) NOT NULL,
    [UserId] [nvarchar](128) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC, [UserId] ASC),
    CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

-- 5. AspNetUserRoles
CREATE TABLE [dbo].[AspNetUserRoles](
    [UserId] [nvarchar](128) NOT NULL,
    [RoleId] [nvarchar](128) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId] FOREIGN KEY([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

-- 6. Familia
CREATE TABLE [dbo].[Familia](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Nombre] [nvarchar](100) NOT NULL,
    [CodigoInvitacion] [nvarchar](20) NOT NULL,
    [Descripcion] [nvarchar](250) NULL,
    [TasaCambioActual] [decimal](18, 4) NOT NULL DEFAULT(59.5000),
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.Familia] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 7. UsuarioFamilia
CREATE TABLE [dbo].[UsuarioFamilia](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioId] [nvarchar](128) NOT NULL,
    [FamiliaId] [int] NOT NULL,
    [Rol] [int] NOT NULL DEFAULT(0),
    [AliasFamiliar] [nvarchar](100) NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.UsuarioFamilia] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.UsuarioFamilia_dbo.AspNetUsers_UsuarioId] FOREIGN KEY([UsuarioId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [FK_dbo.UsuarioFamilia_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id])
);
GO

-- 8. FuenteIngreso
CREATE TABLE [dbo].[FuenteIngreso](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Nombre] [nvarchar](100) NOT NULL,
    [Descripcion] [nvarchar](250) NULL,
    [Tipo] [int] NOT NULL DEFAULT(0),
    [FamiliaId] [int] NOT NULL,
    [UsuarioCreadorId] [nvarchar](128) NULL,
    [ColorIdentificador] [nvarchar](20) NULL DEFAULT('#0071E3'),
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.FuenteIngreso] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.FuenteIngreso_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id])
);
GO

-- 9. Cuenta
CREATE TABLE [dbo].[Cuenta](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Nombre] [nvarchar](100) NOT NULL,
    [InstitucionFinanciera] [nvarchar](100) NULL,
    [NumeroCuenta] [nvarchar](50) NULL,
    [Tipo] [int] NOT NULL DEFAULT(0),
    [Moneda] [int] NOT NULL DEFAULT(0),
    [SaldoActual] [decimal](18, 2) NOT NULL DEFAULT(0.00),
    [FamiliaId] [int] NOT NULL,
    [FuenteIngresoId] [int] NULL,
    [UsuarioResponsableId] [nvarchar](128) NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.Cuenta] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.Cuenta_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id]),
    CONSTRAINT [FK_dbo.Cuenta_dbo.FuenteIngreso_FuenteIngresoId] FOREIGN KEY([FuenteIngresoId]) REFERENCES [dbo].[FuenteIngreso] ([Id])
);
GO

-- 10. Categoria
CREATE TABLE [dbo].[Categoria](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Nombre] [nvarchar](100) NOT NULL,
    [Icono] [nvarchar](50) NULL DEFAULT('tag'),
    [Color] [nvarchar](20) NULL DEFAULT('#86868B'),
    [Tipo] [int] NOT NULL DEFAULT(1),
    [FamiliaId] [int] NOT NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.Categoria] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.Categoria_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id])
);
GO

-- 11. Presupuesto
CREATE TABLE [dbo].[Presupuesto](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Nombre] [nvarchar](100) NOT NULL,
    [MontoLimite] [decimal](18, 2) NOT NULL DEFAULT(0.00),
    [Moneda] [int] NOT NULL DEFAULT(0),
    [Mes] [int] NOT NULL,
    [Anio] [int] NOT NULL,
    [FamiliaId] [int] NOT NULL,
    [CategoriaId] [int] NULL,
    [FuenteIngresoId] [int] NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.Presupuesto] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.Presupuesto_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id]),
    CONSTRAINT [FK_dbo.Presupuesto_dbo.Categoria_CategoriaId] FOREIGN KEY([CategoriaId]) REFERENCES [dbo].[Categoria] ([Id]),
    CONSTRAINT [FK_dbo.Presupuesto_dbo.FuenteIngreso_FuenteIngresoId] FOREIGN KEY([FuenteIngresoId]) REFERENCES [dbo].[FuenteIngreso] ([Id])
);
GO

-- 12. Transaccion
CREATE TABLE [dbo].[Transaccion](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Concepto] [nvarchar](300) NOT NULL,
    [Monto] [decimal](18, 2) NOT NULL,
    [Moneda] [int] NOT NULL DEFAULT(0),
    [TasaCambioAplicada] [decimal](18, 4) NOT NULL DEFAULT(1.0000),
    [MontoEnDOP] [decimal](18, 2) NOT NULL DEFAULT(0.00),
    [Tipo] [int] NOT NULL DEFAULT(1),
    [FechaTransaccion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FamiliaId] [int] NOT NULL,
    [FuenteIngresoId] [int] NULL,
    [CuentaOrigenId] [int] NULL,
    [CuentaDestinoId] [int] NULL,
    [CategoriaId] [int] NULL,
    [UsuarioRegistradorId] [nvarchar](128) NULL,
    [NombreUsuarioRegistrador] [nvarchar](150) NULL,
    [Comentario] [nvarchar](500) NULL,
    [ComprobanteUrl] [nvarchar](500) NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.Transaccion] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.Transaccion_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id]),
    CONSTRAINT [FK_dbo.Transaccion_dbo.Cuenta_CuentaOrigenId] FOREIGN KEY([CuentaOrigenId]) REFERENCES [dbo].[Cuenta] ([Id]),
    CONSTRAINT [FK_dbo.Transaccion_dbo.Cuenta_CuentaDestinoId] FOREIGN KEY([CuentaDestinoId]) REFERENCES [dbo].[Cuenta] ([Id]),
    CONSTRAINT [FK_dbo.Transaccion_dbo.Categoria_CategoriaId] FOREIGN KEY([CategoriaId]) REFERENCES [dbo].[Categoria] ([Id]),
    CONSTRAINT [FK_dbo.Transaccion_dbo.FuenteIngreso_FuenteIngresoId] FOREIGN KEY([FuenteIngresoId]) REFERENCES [dbo].[FuenteIngreso] ([Id])
);
GO

-- 13. TasaCambio
CREATE TABLE [dbo].[TasaCambio](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [FamiliaId] [int] NOT NULL,
    [TasaDOPporUSD] [decimal](18, 4) NOT NULL,
    [FechaVigencia] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [UsuarioRegistradorId] [nvarchar](128) NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.TasaCambio] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.TasaCambio_dbo.Familia_FamiliaId] FOREIGN KEY([FamiliaId]) REFERENCES [dbo].[Familia] ([Id])
);
GO

-- 14. AuditLog
CREATE TABLE [dbo].[AuditLog](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Entidad] [nvarchar](50) NOT NULL,
    [Accion] [nvarchar](50) NOT NULL,
    [RegistroId] [nvarchar](100) NULL,
    [ValoresAnterioresJson] [nvarchar](max) NULL,
    [ValoresNuevosJson] [nvarchar](max) NULL,
    [UsuarioId] [nvarchar](128) NULL,
    [NombreUsuario] [nvarchar](150) NULL,
    [DireccionIP] [nvarchar](50) NULL,
    [FamiliaId] [int] NULL,
    [Detalles] [nvarchar](max) NULL,
    [FechaCreacion] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
    [FechaModificacion] [datetime] NULL,
    [Activo] [bit] NOT NULL DEFAULT(1),
    CONSTRAINT [PK_dbo.AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- ==========================================================
-- SEED DATA EXACTO PARA ENTIDADES
-- ==========================================================

-- 1. Insertar Familia inicial
SET IDENTITY_INSERT [dbo].[Familia] ON;
INSERT INTO [dbo].[Familia] ([Id], [Nombre], [CodigoInvitacion], [Descripcion], [TasaCambioActual], [Activo], [FechaCreacion])
VALUES (1, N'Familia Gómez', N'FH-GOMEZ1', N'Hogar Principal', 60.5000, 1, GETUTCDATE());
SET IDENTITY_INSERT [dbo].[Familia] OFF;
GO

-- 2. Insertar Usuario Admin por defecto
INSERT INTO [dbo].[AspNetUsers] (
    [Id], [NombreCompleto], [FechaRegistro], [FamiliaActualId], [Email], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [UserName], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount]
)
VALUES (
    N'usr-admin-001', N'Esteban Gómez', GETUTCDATE(), 1, N'admin@fhouse.com',
    1, N'AJb+V4q4YqBfHk3v5P2uY9L7eD4rZ8sQ2mN6oP0wX1y=', N'7a1b8c2d-9e3f-4a5b-8c7d-6e5f4a3b2c1d', N'admin@fhouse.com', 0, 0, 0
);
GO

-- 3. Vincular UsuarioFamilia
INSERT INTO [dbo].[UsuarioFamilia] ([UsuarioId], [FamiliaId], [Rol], [AliasFamiliar], [Activo], [FechaCreacion])
VALUES (N'usr-admin-001', 1, 0, N'Administrador Principal', 1, GETUTCDATE());
GO

-- 4. Insertar Categorías base universales
INSERT INTO [dbo].[Categoria] ([FamiliaId], [Nombre], [Tipo], [Color], [Icono], [Activo], [FechaCreacion]) VALUES
(1, N'Supermercado y Alimentación', 1, N'#34C759', N'shopping-cart', 1, GETUTCDATE()),
(1, N'Vivienda y Servicios', 1, N'#0071E3', N'home', 1, GETUTCDATE()),
(1, N'Combustible y Transporte', 1, N'#FF9500', N'car', 1, GETUTCDATE()),
(1, N'Salud y Medicamentos', 1, N'#FF3B30', N'heart', 1, GETUTCDATE()),
(1, N'Educación y Cursos', 1, N'#AF52DE', N'book-open', 1, GETUTCDATE()),
(1, N'Salario y Honorarios', 0, N'#30D158', N'briefcase', 1, GETUTCDATE()),
(1, N'Ventas y Negocios', 0, N'#2997FF', N'trending-up', 1, GETUTCDATE()),
(1, N'Rentas e Inversiones', 0, N'#BF5AF2', N'dollar-sign', 1, GETUTCDATE());
GO

-- 5. Insertar Cuentas de ejemplo iniciales
INSERT INTO [dbo].[Cuenta] ([FamiliaId], [Nombre], [Tipo], [Moneda], [SaldoActual], [InstitucionFinanciera], [NumeroCuenta], [Activo], [FechaCreacion]) VALUES
(1, N'Efectivo DOP', 0, 0, 15000.00, N'Billetera', N'N/A', 1, GETUTCDATE()),
(1, N'Cuenta Corriente Banco', 1, 0, 85000.00, N'Banco BHD', N'****1234', 1, GETUTCDATE()),
(1, N'Ahorro USD', 0, 1, 2500.00, N'Banco Popular', N'****5678', 1, GETUTCDATE());
GO

PRINT 'Esquema y datos iniciales de FHouseDB actualizados exitosamente en SQL Server.';
GO
