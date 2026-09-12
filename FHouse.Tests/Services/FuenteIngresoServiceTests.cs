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
    public class FuenteIngresoServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IFuenteIngresoRepository> _mockFuenteRepo;
        private readonly Mock<ITransaccionRepository> _mockTransaccionRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CrearFuenteIngresoDto>> _mockValidator;
        private readonly FuenteIngresoService _service;

        public FuenteIngresoServiceTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockFuenteRepo = new Mock<IFuenteIngresoRepository>();
            _mockTransaccionRepo = new Mock<ITransaccionRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<CrearFuenteIngresoDto>>();

            _mockUow.Setup(u => u.FuentesIngreso).Returns(_mockFuenteRepo.Object);
            _mockUow.Setup(u => u.Transacciones).Returns(_mockTransaccionRepo.Object);

            _service = new FuenteIngresoService(_mockUow.Object, _mockMapper.Object, _mockValidator.Object);
        }

        [Fact]
        public async Task ObtenerPorFamiliaAsync_CalculaTotalesEnMemoriaSinNMasUno()
        {
            // Arrange
            var familiaId = 1;
            var fuentes = new List<FuenteIngreso>
            {
                new FuenteIngreso { Id = 1, FamiliaId = familiaId, Nombre = "Negocio A", Activo = true },
                new FuenteIngreso { Id = 2, FamiliaId = familiaId, Nombre = "Negocio B", Activo = true }
            };

            var transacciones = new List<Transaccion>
            {
                new Transaccion { Id = 101, FuenteIngresoId = 1, MontoEnDOP = 5000m, Tipo = TipoTransaccion.Ingreso, Activo = true, FamiliaId = familiaId },
                new Transaccion { Id = 102, FuenteIngresoId = 1, MontoEnDOP = 1200m, Tipo = TipoTransaccion.Egreso, Activo = true, FamiliaId = familiaId },
                new Transaccion { Id = 103, FuenteIngresoId = 2, MontoEnDOP = 3000m, Tipo = TipoTransaccion.Ingreso, Activo = true, FamiliaId = familiaId }
            };

            _mockFuenteRepo.Setup(r => r.ObtenerPorFamiliaAsync(familiaId))
                           .ReturnsAsync(fuentes);

            _mockTransaccionRepo.Setup(r => r.ObtenerPorFamiliaAsync(familiaId, null))
                                .ReturnsAsync(transacciones);

            _mockMapper.Setup(m => m.Map<FuenteIngresoDetalleDto>(It.Is<FuenteIngreso>(f => f.Id == 1)))
                       .Returns(new FuenteIngresoDetalleDto { Id = 1, Nombre = "Negocio A" });

            _mockMapper.Setup(m => m.Map<FuenteIngresoDetalleDto>(It.Is<FuenteIngreso>(f => f.Id == 2)))
                       .Returns(new FuenteIngresoDetalleDto { Id = 2, Nombre = "Negocio B" });

            // Act
            var res = await _service.ObtenerPorFamiliaAsync(familiaId);

            // Assert
            res.Exitoso.Should().BeTrue();
            var resultado = res.Datos.ToList();
            resultado.Should().HaveCount(2);

            // Negocio A: 5000 ingreso, 1200 gasto, 3800 neto
            var fuenteA = resultado.First(f => f.Id == 1);
            fuenteA.TotalIngresosDOP.Should().Be(5000m);
            fuenteA.TotalEgresosDOP.Should().Be(1200m);
            fuenteA.BalanceNetoDOP.Should().Be(3800m);

            // Negocio B: 3000 ingreso, 0 gasto, 3000 neto
            var fuenteB = resultado.First(f => f.Id == 2);
            fuenteB.TotalIngresosDOP.Should().Be(3000m);
            fuenteB.TotalEgresosDOP.Should().Be(0m);
            fuenteB.BalanceNetoDOP.Should().Be(3000m);

            // Verification: ObtenerPorFamiliaAsync was called exactly ONCE on TransaccionRepository (no N+1 loops)
            _mockTransaccionRepo.Verify(r => r.ObtenerPorFamiliaAsync(familiaId, null), Times.Once);
        }
    }
}
