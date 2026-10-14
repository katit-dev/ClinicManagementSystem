using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Audit;

public class MedicalAuditWriterTests
{
    [Fact]
    public async Task EmptyBatch_DoesNotTouchPersistence()
    {
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        await new MedicalAuditWriter(unit.Object).WriteAsync([]);
        unit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Batch_IsAddedThenSavedOnce_WithoutUpdateOrDelete()
    {
        var repository = new Mock<IAuditLogRepository>(MockBehavior.Strict);
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var row = new AuditLog { Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 1204 };
        var sequence = new MockSequence();
        unit.SetupGet(x => x.AuditLogRepository).Returns(repository.Object);
        repository.InSequence(sequence).Setup(x => x.AddListItemsAsync(It.Is<List<AuditLog>>(rows => rows.Count == 1 && rows[0] == row))).Returns(Task.CompletedTask);
        unit.InSequence(sequence).Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        await new MedicalAuditWriter(unit.Object).WriteAsync([row]);
        repository.Verify(x => x.AddListItemsAsync(It.IsAny<List<AuditLog>>()), Times.Once);
        unit.Verify(x => x.SaveChangesAsync(), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PersistenceError_IsPropagated_WithoutAutomaticRetry()
    {
        var repository = new Mock<IAuditLogRepository>(); var unit = new Mock<IUnitOfWork>();
        unit.SetupGet(x => x.AuditLogRepository).Returns(repository.Object);
        repository.Setup(x => x.AddListItemsAsync(It.IsAny<List<AuditLog>>())).Returns(Task.CompletedTask);
        unit.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("test error"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new MedicalAuditWriter(unit.Object).WriteAsync([new AuditLog()]));
        unit.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
