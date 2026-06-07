using FluentAssertions;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Models.Role;
using IdentityService.Api.Services;
using Moq;

namespace IdentityService.UnitTests.Services
{
    public class BaseServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IBaseRepository<Role>> _repoMock;
        private readonly BaseService<Role> _service;

        public BaseServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IBaseRepository<Role>>();

            _uowMock.Setup(x => x.Repository<Role>()).Returns(_repoMock.Object);

            _service = new BaseService<Role>(_uowMock.Object);
        }

        [Fact]
        public async Task Add_ShouldCallRepositoryAddAndSaveChanges()
        {
            // 1. ARRANGE
            var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };

            // 2. ACT
            var resultId = await _service.Add(role);

            // 3. ASSERT
            resultId.Should().Be(role.Id);

            _repoMock.Verify(x => x.AddAsync(role), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetById_ShouldReturnEntity_WhenExists()
        {
            // 1. ARRANGE
            var id = Guid.NewGuid();
            var role = new Role { Id = id, Name = "User" };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(role);

            // 2. ACT
            var result = await _service.GetById(id);

            // 3. ASSERT
            result.Should().BeEquivalentTo(role);
            _repoMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldCallRepositoryUpdateAndSaveChanges()
        {
            // 1. ARRANGE
            var role = new Role { Id = Guid.NewGuid(), Name = "SuperAdmin" };

            // 2. ACT
            var resultId = await _service.Update(role);

            // 3. ASSERT
            resultId.Should().Be(role.Id);

            _repoMock.Verify(x => x.Update(role), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ShouldCallRepositoryDeleteAndSaveChanges()
        {
            // 1. ARRANGE
            var id = Guid.NewGuid();

            // 2. ACT
            await _service.Delete(id);

            // 3. ASSERT
            _repoMock.Verify(x => x.DeleteAsync(id), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByName_ShouldReturnEntity_WhenFound()
        {
            // 1. ARRANGE
            var name = "Manager";
            var role = new Role { Id = Guid.NewGuid(), Name = name };

            _repoMock.Setup(x => x.GetByNameAsync(name)).ReturnsAsync(role);

            // 2. ACT
            var result = await _service.GetByName(name);

            // 3. ASSERT
            result.Should().BeEquivalentTo(role);
        }

        [Fact]
        public void IsNameUnique_ShouldReturnTrue_WhenRepoReturnsTrue()
        {
            // 1. ARRANGE
            var name = "UniqueRole";
            _repoMock.Setup(x => x.IsNameUnique(name)).Returns(true);

            // 2. ACT
            var result = _service.IsNameUnique(name);

            // 3. ASSERT
            result.Should().BeTrue();
        }
    }
}