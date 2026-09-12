using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FHouse.Core.Entities;
using FHouse.Core.Enums;
using FHouse.Core.Interfaces.Repositories;
using FHouse.Services.DTOs;
using FHouse.Services.Implementations;
using Moq;
using Xunit;

namespace FHouse.Tests.Services
{
    public class TransaccionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ITransaccionRepository> _mockTransaccionRepo;
        private readonly Mock<ICuentaRepository> _mockCuentaRepo;
        private readonly Mock<IRepository<Familia>> _mockFamiliaRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CrearTransaccionDto>> _mockValidator;
        private readonly TransaccionService _service;

        public TransaccionServiceTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockTransaccionRepo = new Mock<ITransaccionRepository>();
            _mockCuentaRepo = new Mock<ICuentaRepository>();
            _mockFamiliaRepo = new Mock<IRepository<Familia>>();

            _mockUow.Setup(u => u.Transacciones).Returns(_mockTransaccionRepo.Object);
            _mockUow.Setup(u => u.Cuentas).Returns(_mockCuentaRepo.Object);
            _mockUow.Setup(u => u.Familias).Returns(_mockFamiliaRepo.Object);

            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<CrearTransaccionDto>>();

            _service = new TransaccionService(_mockUow.Object, _mockMapper.Object, _mockValidator.Object);
        }

        [Fact]
        public async Task RegistrarEgreso_ConSaldoInsuficiente_RetornaError()
        {
            // Arrange
            var dto = new CrearTransaccionDto
            {
                Concepto = "Pago de Servicio",
                Monto = 15000m,
                Moneda = Moneda.DOP,
                Tipo = TipoTransaccion.Egreso,
                FamiliaId = 1,
                CuentaOrigenId = 10
            };

            var cuenta = new Cuenta
            {
                Id = 10,
                Nombre = "Cuenta Nómina",
                SaldoActual = 5000m,
                Moneda = Moneda.DOP
            };

            var familia = new Familia
            {
                Id = 1,
                Nombre = "Familia Test",
                TasaCambioActual = 60.0m
            };

            _mockValidator.Setup(v => v.ValidateAsync(dto, default))
                          .ReturnsAsync(new ValidationResult());

            _mockFamiliaRepo.Setup(u => u.ObtenerPorIdAsync(1))
                            .ReturnsAsync(familia);

            _mockCuentaRepo.Setup(u => u.ObtenerPorIdAsync(10))
                           .ReturnsAsync(cuenta);

            // Act
            var resultado = await _service.RegistrarTransaccionAsync(dto, "user-123", "Esteban");

            // Assert
            resultado.Exitoso.Should().BeFalse();
            resultado.Mensaje.Should().Contain("Saldo insuficiente");
            _mockTransaccionRepo.Verify(u => u.AgregarAsync(It.IsAny<Transaccion>()), Times.Never);
            _mockUow.Verify(u => u.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarEgreso_ConSaldoSuficiente_DescuentaSaldoYGuardaTransaccion()
        {
            // Arrange
            var dto = new CrearTransaccionDto
            {
                Concepto = "Compra de Suministros",
                Monto = 3000m,
                Moneda = Moneda.DOP,
                Tipo = TipoTransaccion.Egreso,
                FamiliaId = 1,
                CuentaOrigenId = 10
            };

            var cuenta = new Cuenta
            {
                Id = 10,
                Nombre = "Cuenta Operativa",
                SaldoActual = 10000m,
                Moneda = Moneda.DOP
            };

            var familia = new Familia
            {
                Id = 1,
                Nombre = "Familia Test",
                TasaCambioActual = 60.0m
            };

            var transaccion = new Transaccion
            {
                Id = 1,
                Concepto = dto.Concepto,
                Monto = dto.Monto,
                FamiliaId = 1
            };

            var detalleDto = new TransaccionDetalleDto
            {
                Id = 1,
                Concepto = dto.Concepto,
                Monto = dto.Monto
            };

            _mockValidator.Setup(v => v.ValidateAsync(dto, default))
                          .ReturnsAsync(new ValidationResult());

            _mockFamiliaRepo.Setup(u => u.ObtenerPorIdAsync(1))
                            .ReturnsAsync(familia);

            _mockCuentaRepo.Setup(u => u.ObtenerPorIdAsync(10))
                           .ReturnsAsync(cuenta);

            _mockMapper.Setup(m => m.Map<Transaccion>(dto))
                       .Returns(transaccion);

            _mockMapper.Setup(m => m.Map<TransaccionDetalleDto>(transaccion))
                       .Returns(detalleDto);

            // Act
            var resultado = await _service.RegistrarTransaccionAsync(dto, "user-123", "Esteban");

            // Assert
            resultado.Exitoso.Should().BeTrue();
            cuenta.SaldoActual.Should().Be(7000m);
            _mockCuentaRepo.Verify(u => u.Actualizar(cuenta), Times.Once);
            _mockTransaccionRepo.Verify(u => u.AgregarAsync(transaccion), Times.Once);
            _mockUow.Verify(u => u.GuardarCambiosAsync(), Times.Once);
        }
    }
}
