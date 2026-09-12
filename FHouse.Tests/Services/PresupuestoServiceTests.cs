using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FHouse.Core.Entities;
using FHouse.Core.Enums;
using FHouse.Core.Interfaces.Repositories;
using FHouse.Services.DTOs;
using FHouse.Services.Implementations;
using Moq;
using Xunit;

namespace FHouse.Tests.Services
{
    public class PresupuestoServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPresupuestoRepository> _mockPresupuestoRepo;
        private readonly Mock<ITransaccionRepository> _mockTransaccionRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CrearPresupuestoDto>> _mockValidator;
        private readonly PresupuestoService _service;

        public PresupuestoServiceTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockPresupuestoRepo = new Mock<IPresupuestoRepository>();
            _mockTransaccionRepo = new Mock<ITransaccionRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<CrearPresupuestoDto>>();

            _mockUow.Setup(u => u.Presupuestos).Returns(_mockPresupuestoRepo.Object);
            _mockUow.Setup(u => u.Transacciones).Returns(_mockTransaccionRepo.Object);

            _service = new PresupuestoService(_mockUow.Object, _mockMapper.Object, _mockValidator.Object);
        }

        [Fact]
        public async Task ObtenerPorPeriodoAsync_CalculaGastosAcumuladosSinNMasUno()
        {
            // Arrange
            var familiaId = 1;
            var mes = 9;
            var anio = 2026;

            var presupuestos = new List<Presupuesto>
            {
                new Presupuesto 
                { 
                    Id = 1, 
                    FamiliaId = familiaId, 
                    Nombre = "Supermercado", 
                    MontoLimite = 10000m, 
                    CategoriaId = 10,
                    Mes = mes,
                    Anio = anio,
                    Activo = true
                },
                new Presupuesto 
                { 
                    Id = 2, 
                    FamiliaId = familiaId, 
                    Nombre = "Combustible", 
                    MontoLimite = 5000m, 
                    CategoriaId = 20,
                    Mes = mes,
                    Anio = anio,
                    Activo = true
                }
            };

            var transaccionesMes = new List<Transaccion>
            {
                new Transaccion { Id = 1, CategoriaId = 10, MontoEnDOP = 3000m, Tipo = TipoTransaccion.Egreso, FechaTransaccion = new DateTime(2026, 9, 5), Activo = true, FamiliaId = familiaId },
                new Transaccion { Id = 2, CategoriaId = 10, MontoEnDOP = 2500m, Tipo = TipoTransaccion.Egreso, FechaTransaccion = new DateTime(2026, 9, 10), Activo = true, FamiliaId = familiaId },
                new Transaccion { Id = 3, CategoriaId = 20, MontoEnDOP = 1800m, Tipo = TipoTransaccion.Egreso, FechaTransaccion = new DateTime(2026, 9, 8), Activo = true, FamiliaId = familiaId }
            };

            var dtoList = new List<PresupuestoDetalleDto>
            {
                new PresupuestoDetalleDto { Id = 1, Nombre = "Supermercado", MontoLimite = 10000m, CategoriaId = 10 },
                new PresupuestoDetalleDto { Id = 2, Nombre = "Combustible", MontoLimite = 5000m, CategoriaId = 20 }
            };

            _mockPresupuestoRepo.Setup(r => r.ObtenerPorFamiliaPeriodoAsync(familiaId, mes, anio))
                                .ReturnsAsync(presupuestos);

            _mockTransaccionRepo.Setup(r => r.ObtenerFiltradasAsync(
                                    familiaId, 
                                    It.IsAny<DateTime?>(), 
                                    It.IsAny<DateTime?>(), 
                                    null, 
                                    null, 
                                    null, 
                                    TipoTransaccion.Egreso, 
                                    null))
                                .ReturnsAsync(transaccionesMes);

            _mockMapper.Setup(m => m.Map<List<PresupuestoDetalleDto>>(presupuestos))
                       .Returns(dtoList);

            // Act
            var res = await _service.ObtenerPorPeriodoAsync(familiaId, mes, anio);

            // Assert
            res.Exitoso.Should().BeTrue();
            var resultado = res.Datos.ToList();
            resultado.Should().HaveCount(2);

            // Supermercado: Limite 10,000, Gastado 5,500, % = 55%
            var p1 = resultado.First(p => p.Id == 1);
            p1.MontoEjecutadoDOP.Should().Be(5500m);
            p1.PorcentajeEjecutado.Should().Be(55m);

            // Combustible: Limite 5,000, Gastado 1,800, % = 36%
            var p2 = resultado.First(p => p.Id == 2);
            p2.MontoEjecutadoDOP.Should().Be(1800m);
            p2.PorcentajeEjecutado.Should().Be(36m);

            // Verification: ObtenerFiltradasAsync was called exactly ONCE on TransaccionRepository
            _mockTransaccionRepo.Verify(r => r.ObtenerFiltradasAsync(
                familiaId, 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>(), 
                null, 
                null, 
                null, 
                TipoTransaccion.Egreso, 
                null), Times.Once);
        }
    }
}
