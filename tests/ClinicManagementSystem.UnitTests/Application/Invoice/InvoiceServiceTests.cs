using System.Linq.Expressions;

using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.Payment;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.Services;

using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using MockQueryable;

using Moq;


namespace ClinicManagementSystem.UnitTests.Application.Invoice;


// =====================================================
// ALIAS
// =====================================================

using InvoiceEntity =
    ClinicManagementSystem.Infrastructure.Models.Invoice;


public class InvoiceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    private readonly InvoiceService _service;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public InvoiceServiceTests()
    {
        _unitOfWorkMock =
            new Mock<IUnitOfWork>();

        _service =
            new InvoiceService(
                _unitOfWorkMock.Object,
                NullLogger<InvoiceService>.Instance
            );
    }


    // =====================================================
    // HELPERS
    // =====================================================

    private void SetupInvoiceRepository(
        params InvoiceEntity[] invoices)
    {
        var repositoryMock =
            new Mock<IInvoiceRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    InvoiceEntity,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            InvoiceEntity,
                            bool
                        >
                    > predicate
                ) =>
                    invoices
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.InvoiceRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupInvoiceItemRepository(
        params InvoiceItem[] items)
    {
        var repositoryMock =
            new Mock<IInvoiceItemRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    InvoiceItem,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            InvoiceItem,
                            bool
                        >
                    > predicate
                ) =>
                    items
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.InvoiceItemRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupPaymentRepository(
        params Payment[] payments)
    {
        var repositoryMock =
            new Mock<IPaymentRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Payment,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            Payment,
                            bool
                        >
                    > predicate
                ) =>
                    payments
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PaymentRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void VerifyNoTransaction()
    {
        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.CommitTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.RollbackTransactionAsync(),
            Times.Never
        );
    }


    private static PaymentRequestDTO
        CreateValidPaymentRequest()
    {
        return new PaymentRequestDTO
        {
            Amount = 100_000,

            Method = 0,

            IsRefund = false
        };
    }


    // =====================================================
    // GET INVOICE
    // =====================================================


    // =====================================================
    // TEST 1
    //
    // Invalid invoice ID
    //
    // invoiceId <= 0
    //
    // Expected:
    // 400
    // InvoiceNotFound
    // =====================================================

    [Fact]
    public async Task GetInvoiceAsync_ShouldReturn400_WhenInvoiceIdIsInvalid()
    {
        // Arrange
        // Không cần setup repository.


        // Act

        var result =
            await _service
                .GetInvoiceAsync(
                    invoiceId: 0
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .InvoiceNotFound,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 2
    //
    // Invoice không tồn tại
    //
    // Expected:
    // 404
    // InvoiceNotFound
    // =====================================================

    [Fact]
    public async Task GetInvoiceAsync_ShouldReturn404_WhenInvoiceDoesNotExist()
    {
        // Arrange

        SetupInvoiceRepository();


        // Act

        var result =
            await _service
                .GetInvoiceAsync(
                    invoiceId: 1
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .InvoiceNotFound,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 3
    //
    // Invoice tồn tại.
    //
    // Kiểm tra:
    // - Mapping invoice
    // - Mapping items
    // - Mapping payments
    // - RemainingAmount
    //
    // Expected:
    // 200
    // =====================================================

    [Fact]
    public async Task GetInvoiceAsync_ShouldReturn200_WhenInvoiceExists()
    {
        // Arrange

        var invoice =
            new InvoiceEntity
            {
                Id = 1,

                InvoiceNo =
                    "INV-001",

                PatientId =
                    10,

                PatientName =
                    "Nguyen Van A",

                AppointmentId =
                    20,

                MedicalRecordId =
                    30,

                TotalAmount =
                    1_000_000,

                DiscountAmount =
                    100_000,

                TaxAmount =
                    50_000,

                InsuranceAmount =
                    50_000,

                PaidAmount =
                    500_000,

                Status =
                    1,

                CreatedAt =
                    DateTime.Now
            };


        var item =
            new InvoiceItem
            {
                Id = 1,

                InvoiceId =
                    1,

                MedicineId =
                    5,

                MedicalRecordServiceId =
                    null,

                Description =
                    "Thuốc test",

                Quantity =
                    2,

                UnitPrice =
                    300_000,

                DiscountAmount =
                    50_000,

                Amount =
                    550_000
            };


        var payment =
            new Payment
            {
                Id = 1,

                InvoiceId =
                    1,

                Amount =
                    500_000,

                Method =
                    0,

                PaidAt =
                    DateTime.Now,

                Note =
                    "Thanh toán test",

                ReferenceCode =
                    "PAY-001",

                ReceivedBy =
                    99,

                IsRefund =
                    false
            };


        SetupInvoiceRepository(
            invoice
        );

        SetupInvoiceItemRepository(
            item
        );

        SetupPaymentRepository(
            payment
        );


        // Act

        var result =
            await _service
                .GetInvoiceAsync(
                    invoiceId: 1
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .GetSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );


        // Invoice

        Assert.Equal(
            1,
            result.Content!.Id
        );

        Assert.Equal(
            "INV-001",
            result.Content.InvoiceNo
        );

        Assert.Equal(
            10,
            result.Content.PatientId
        );

        Assert.Equal(
            "Nguyen Van A",
            result.Content.PatientName
        );

        Assert.Equal(
            20,
            result.Content.AppointmentId
        );

        Assert.Equal(
            30,
            result.Content.MedicalRecordId
        );


        // Amount

        Assert.Equal(
            1_000_000,
            result.Content.TotalAmount
        );

        Assert.Equal(
            100_000,
            result.Content.DiscountAmount
        );

        Assert.Equal(
            50_000,
            result.Content.TaxAmount
        );

        Assert.Equal(
            50_000,
            result.Content.InsuranceAmount
        );

        Assert.Equal(
            500_000,
            result.Content.PaidAmount
        );


        // Payable:
        //
        // 1,000,000
        // + 50,000
        // - 100,000
        // - 50,000
        // = 900,000
        //
        // Remaining:
        //
        // 900,000
        // - 500,000
        // = 400,000

        Assert.Equal(
            400_000,
            result.Content.RemainingAmount
        );


        // Items

        Assert.NotNull(
            result.Content.Items
        );

        Assert.Single(
            result.Content.Items
        );

        Assert.Equal(
            "Thuốc test",
            result.Content.Items[0].Description
        );

        Assert.Equal(
            2,
            result.Content.Items[0].Quantity
        );

        Assert.Equal(
            300_000,
            result.Content.Items[0].UnitPrice
        );

        Assert.Equal(
            50_000,
            result.Content.Items[0].DiscountAmount
        );

        Assert.Equal(
            550_000,
            result.Content.Items[0].Amount
        );


        // Payments

        Assert.NotNull(
            result.Content.Payments
        );

        Assert.Single(
            result.Content.Payments
        );

        Assert.Equal(
            500_000,
            result.Content.Payments[0].Amount
        );

        Assert.Equal(
            "PAY-001",
            result.Content.Payments[0].ReferenceCode
        );

        Assert.False(
            result.Content.Payments[0].IsRefund
        );
    }


    // =====================================================
    // TEST 4
    //
    // PaidAmount > payableAmount
    //
    // Expected:
    // RemainingAmount = 0
    //
    // Không được trả số âm.
    // =====================================================

    [Fact]
    public async Task GetInvoiceAsync_ShouldSetRemainingAmountToZero_WhenPaidAmountExceedsPayableAmount()
    {
        // Arrange

        var invoice =
            new InvoiceEntity
            {
                Id = 1,

                InvoiceNo =
                    "INV-002",

                PatientId =
                    10,

                PatientName =
                    "Nguyen Van A",

                TotalAmount =
                    500_000,

                DiscountAmount =
                    0,

                TaxAmount =
                    0,

                InsuranceAmount =
                    0,

                PaidAmount =
                    600_000,

                Status =
                    2,

                CreatedAt =
                    DateTime.Now
            };


        SetupInvoiceRepository(
            invoice
        );

        SetupInvoiceItemRepository();

        SetupPaymentRepository();


        // Act

        var result =
            await _service
                .GetInvoiceAsync(
                    invoiceId: 1
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Equal(
            0,
            result.Content!.RemainingAmount
        );
    }


// =====================================================
// TEST 5
//
// InvoiceRepository throw exception.
//
// Expected:
// 500
// GetFailed
// =====================================================

[Fact]
public async Task GetInvoiceAsync_ShouldReturn500_WhenExceptionOccurs()
{
    // Arrange

    _unitOfWorkMock
        .Setup(
            x =>
                x.InvoiceRepository
        )
        .Throws(
            new InvalidOperationException(
                "Test exception"
            )
        );


    // Act

    var result =
        await _service
            .GetInvoiceAsync(
                invoiceId: 1
            );


    // Assert

    Assert.Equal(
        500,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .GetFailed,
        result.Message
    );

    Assert.Null(
        result.Content
    );
}

    // =====================================================
    // CREATE PAYMENT
    // =====================================================


    // =====================================================
    // TEST 6
    //
    // Invalid invoice ID
    //
    // invoiceId <= 0
    //
    // Expected:
    // 400
    // InvoiceNotFound
    // =====================================================

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturn400_WhenInvoiceIdIsInvalid()
    {
        // Arrange

        var request =
            CreateValidPaymentRequest();


        // Act

        var result =
            await _service
                .CreatePaymentAsync(
                    invoiceId: 0,
                    request: request,
                    currentUserId: 1
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .InvoiceNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 7
    //
    // Invalid currentUserId
    //
    // currentUserId <= 0
    //
    // Expected:
    // 401
    // PaymentUserNotFound
    // =====================================================

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturn401_WhenCurrentUserIdIsInvalid()
    {
        // Arrange

        var request =
            CreateValidPaymentRequest();


        // Act

        var result =
            await _service
                .CreatePaymentAsync(
                    invoiceId: 1,
                    request: request,
                    currentUserId: 0
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .PaymentUserNotFound,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 8
    //
    // Null request
    //
    // Expected:
    // 400
    // PaymentRequestRequired
    // =====================================================

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturn400_WhenRequestIsNull()
    {
        // Act

        var result =
            await _service
                .CreatePaymentAsync(
                    invoiceId: 1,
                    request: null!,
                    currentUserId: 1
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .PaymentRequestRequired,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 9
    //
    // IsRefund = true
    //
    // CreatePaymentAsync chỉ dùng để thu tiền.
    //
    // Expected:
    // 400
    // RefundMustUseRefundFunction
    // =====================================================

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturn400_WhenRequestIsRefund()
    {
        // Arrange

        var request =
            new PaymentRequestDTO
            {
                Amount =
                    100_000,

                Method =
                    0,

                IsRefund =
                    true
            };


        // Act

        var result =
            await _service
                .CreatePaymentAsync(
                    invoiceId: 1,
                    request: request,
                    currentUserId: 1
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .RefundMustUseRefundFunction,
            result.Message
        );

        VerifyNoTransaction();
    }


    // =====================================================
    // TEST 10
    //
    // Amount <= 0
    //
    // Expected:
    // 400
    // RefundAmountInvalid
    // =====================================================

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturn400_WhenAmountIsInvalid()
    {
        // Arrange

        var request =
            new PaymentRequestDTO
            {
                Amount =
                    0,

                Method =
                    0,

                IsRefund =
                    false
            };


        // Act

        var result =
            await _service
                .CreatePaymentAsync(
                    invoiceId: 1,
                    request: request,
                    currentUserId: 1
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            InvoiceResponseMessageDTO
                .RefundAmountInvalid,
            result.Message
        );

        VerifyNoTransaction();
    }

    // =====================================================
// ADDITIONAL HELPER
// =====================================================

private void SetupAuditLogRepositoryForInvoiceTests()
{
    var repositoryMock =
        new Mock<IAuditLogRepository>();

    repositoryMock
        .Setup(
            x =>
                x.AddAsync(
                    It.IsAny<AuditLog>()
                )
        )
        .Returns(
            Task.CompletedTask
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.AuditLogRepository
        )
        .Returns(
            repositoryMock.Object
        );
}


// =====================================================
// TEST 11
//
// Invoice tồn tại nhưng lấy InvoiceItem bị exception.
//
// Expected:
// 500
// GetFailed
// =====================================================

[Fact]
public async Task GetInvoiceAsync_ShouldReturn500_WhenInvoiceItemRepositoryThrows()
{
    // Arrange

    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-011",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                0,

            CreatedAt =
                DateTime.Now
        };


    SetupInvoiceRepository(
        invoice
    );


    var itemRepositoryMock =
        new Mock<IInvoiceItemRepository>();

    itemRepositoryMock
        .Setup(
            x =>
                x.WhereSql(
                    It.IsAny<
                        Expression<
                            Func<
                                InvoiceItem,
                                bool
                            >
                        >
                    >()
                )
        )
        .Throws(
            new InvalidOperationException(
                "Test exception"
            )
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.InvoiceItemRepository
        )
        .Returns(
            itemRepositoryMock.Object
        );


    // Act

    var result =
        await _service
            .GetInvoiceAsync(
                invoiceId: 1
            );


    // Assert

    Assert.Equal(
        500,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .GetFailed,
        result.Message
    );

    Assert.Null(
        result.Content
    );
}


// =====================================================
// CREATE PAYMENT
// =====================================================


// =====================================================
// TEST 12
//
// Payment method invalid.
//
// Expected:
// 400
// PaymentMethodInvalid
// =====================================================

[Fact]
public async Task CreatePaymentAsync_ShouldReturn400_WhenPaymentMethodIsInvalid()
{
    // Arrange

    var request =
        CreateValidPaymentRequest();

    request.Method =
        byte.MaxValue;


    // Act

    var result =
        await _service
            .CreatePaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    // Assert

    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .PaymentMethodInvalid,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 13
//
// Invoice không tồn tại.
//
// Expected:
// 404
// InvoiceNotFound
// =====================================================

[Fact]
public async Task CreatePaymentAsync_ShouldReturn404_WhenInvoiceDoesNotExist()
{
    // Arrange

    SetupInvoiceRepository();

    var request =
        CreateValidPaymentRequest();


    // Act

    var result =
        await _service
            .CreatePaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    // Assert

    Assert.Equal(
        404,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .InvoiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 14
//
// Invoice đã cancelled.
//
// Expected:
// 400
// PaymentInvoiceCancelled
// =====================================================

[Fact]
public async Task CreatePaymentAsync_ShouldReturn400_WhenInvoiceIsCancelled()
{
    // Arrange

    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-014",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                (byte)InvoiceStatus.Cancelled,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        CreateValidPaymentRequest();


    // Act

    var result =
        await _service
            .CreatePaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    // Assert

    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .PaymentInvoiceCancelled,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 15
//
// Payment amount > remaining amount.
//
// Expected:
// 400
// Không được vượt quá remaining.
// =====================================================

[Fact]
public async Task CreatePaymentAsync_ShouldReturn400_WhenPaymentAmountExceedsRemainingAmount()
{
    // Arrange

    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-015",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                200_000,

            Status =
                (byte)InvoiceStatus.PartiallyPaid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        new PaymentRequestDTO
        {
            Amount =
                800_001,

            Method =
                0,

            IsRefund =
                false
        };


    // Act

    var result =
        await _service
            .CreatePaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    // Assert

    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        "Số tiền thanh toán không được vượt quá 800,000.",
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 16
//
// Thanh toán đủ toàn bộ invoice.
//
// Expected:
// 200
// PaymentSuccess
//
// Kiểm tra:
// - PaidAmount
// - Status -> Paid
// - Payment được tạo
// - BeginTransaction
// - SaveChanges
// - Commit
// =====================================================

[Fact]
public async Task CreatePaymentAsync_ShouldReturn200_WhenPaymentFullyPaysInvoice()
{
    // Arrange

    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-016",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                (byte)InvoiceStatus.Unpaid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    SetupInvoiceItemRepository();


    Payment? addedPayment = null;

    var paymentRepositoryMock =
        new Mock<IPaymentRepository>();

    paymentRepositoryMock
        .Setup(
            x =>
                x.WhereSql(
                    It.IsAny<
                        Expression<
                            Func<
                                Payment,
                                bool
                            >
                        >
                    >()
                )
        )
        .Returns(
            Array.Empty<Payment>()
                .BuildMock()
        );

    paymentRepositoryMock
        .Setup(
            x =>
                x.AddAsync(
                    It.IsAny<Payment>()
                )
        )
        .Callback(
            (Payment payment) =>
            {
                addedPayment =
                    payment;
            }
        )
        .Returns(
            Task.CompletedTask
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.PaymentRepository
        )
        .Returns(
            paymentRepositoryMock.Object
        );


    SetupAuditLogRepositoryForInvoiceTests();


    var request =
        new PaymentRequestDTO
        {
            Amount =
                1_000_000,

            Method =
                0,

            Note =
                "Thanh toán toàn bộ",

            ReferenceCode =
                "PAY-016",

            IsRefund =
                false
        };


    // Act

    var result =
        await _service
            .CreatePaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 5
            );


    // Assert

    Assert.Equal(
        200,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .PaymentSuccess,
        result.Message
    );

    Assert.NotNull(
        result.Content
    );

    Assert.Equal(
        1_000_000,
        invoice.PaidAmount
    );

    Assert.Equal(
        (byte)InvoiceStatus.Paid,
        invoice.Status
    );


    Assert.NotNull(
        addedPayment
    );

    Assert.Equal(
        1,
        addedPayment!.InvoiceId
    );

    Assert.Equal(
        1_000_000,
        addedPayment.Amount
    );

    Assert.Equal(
        5,
        addedPayment.ReceivedBy
    );

    Assert.False(
        addedPayment.IsRefund
    );

    Assert.Equal(
        "PAY-016",
        addedPayment.ReferenceCode
    );


    _unitOfWorkMock.Verify(
        x =>
            x.BeginTransactionAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.CommitTransactionAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.RollbackTransactionAsync(),
        Times.Never
    );

    paymentRepositoryMock.Verify(
        x =>
            x.AddAsync(
                It.IsAny<Payment>()
            ),
        Times.Once
    );
}


// =====================================================
// REFUND PAYMENT
// =====================================================


// =====================================================
// TEST 17
//
// Invalid invoice ID.
//
// Expected:
// 400
// InvoiceNotFound
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenInvoiceIdIsInvalid()
{
    var request =
        new PaymentRequestDTO
        {
            Amount =
                100_000,

            Method =
                0,

            IsRefund =
                true
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 0,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .InvoiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 18
//
// Null request.
//
// Expected:
// 400
// PaymentRequestRequired
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenRequestIsNull()
{
    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: null!,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .PaymentRequestRequired,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 19
//
// IsRefund = false.
//
// Expected:
// 400
// RefundFlagInvalid
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenRefundFlagIsFalse()
{
    var request =
        new PaymentRequestDTO
        {
            Amount =
                100_000,

            Method =
                0,

            IsRefund =
                false
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .RefundFlagInvalid,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 20
//
// Amount <= 0.
//
// Expected:
// 400
// PaymentAmountInvalid
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenAmountIsInvalid()
{
    var request =
        new PaymentRequestDTO
        {
            Amount =
                0,

            Method =
                0,

            IsRefund =
                true
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .PaymentAmountInvalid,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 21
//
// Invoice không tồn tại.
//
// Expected:
// 404
// InvoiceNotFound
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn404_WhenInvoiceDoesNotExist()
{
    SetupInvoiceRepository();

    var request =
        new PaymentRequestDTO
        {
            Amount =
                100_000,

            Method =
                0,

            IsRefund =
                true
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        404,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .InvoiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 22
//
// Invoice đã cancelled.
//
// Expected:
// 400
// PaymentInvoiceCancelled
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenInvoiceIsCancelled()
{
    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-022",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                1_000_000,

            Status =
                (byte)InvoiceStatus.Cancelled,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        new PaymentRequestDTO
        {
            Amount =
                100_000,

            Method =
                0,

            IsRefund =
                true
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .PaymentInvoiceCancelled,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 23
//
// PaidAmount <= 0.
//
// Expected:
// 400
// RefundNotAvailable
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenThereIsNothingToRefund()
{
    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-023",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                (byte)InvoiceStatus.Unpaid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        new PaymentRequestDTO
        {
            Amount =
                100_000,

            Method =
                0,

            IsRefund =
                true
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .RefundNotAvailable,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 24
//
// Refund amount > PaidAmount.
//
// Expected:
// 400
// Dynamic error message.
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn400_WhenRefundAmountExceedsPaidAmount()
{
    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-024",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                300_000,

            Status =
                (byte)InvoiceStatus.PartiallyPaid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        new PaymentRequestDTO
        {
            Amount =
                300_001,

            Method =
                0,

            IsRefund =
                true
        };


    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        "Số tiền hoàn không được vượt quá 300,000.",
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 25
//
// Refund toàn bộ số tiền đã thanh toán.
//
// Expected:
// 200
// RefundSuccess
//
// PaidAmount:
// 1,000,000 -> 0
//
// Status:
// Paid -> Unpaid
//
// Refund Payment:
// Amount = -1,000,000
// IsRefund = true
// =====================================================

[Fact]
public async Task RefundPaymentAsync_ShouldReturn200_WhenFullRefundIsSuccessful()
{
    // Arrange

    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-025",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                1_000_000,

            Status =
                (byte)InvoiceStatus.Paid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    SetupInvoiceItemRepository();


    Payment? addedRefund =
        null;

    var paymentRepositoryMock =
        new Mock<IPaymentRepository>();

    paymentRepositoryMock
        .Setup(
            x =>
                x.WhereSql(
                    It.IsAny<
                        Expression<
                            Func<
                                Payment,
                                bool
                            >
                        >
                    >()
                )
        )
        .Returns(
            Array.Empty<Payment>()
                .BuildMock()
        );

    paymentRepositoryMock
        .Setup(
            x =>
                x.AddAsync(
                    It.IsAny<Payment>()
                )
        )
        .Callback(
            (Payment payment) =>
            {
                addedRefund =
                    payment;
            }
        )
        .Returns(
            Task.CompletedTask
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.PaymentRepository
        )
        .Returns(
            paymentRepositoryMock.Object
        );


    SetupAuditLogRepositoryForInvoiceTests();


    var request =
        new PaymentRequestDTO
        {
            Amount =
                1_000_000,

            Method =
                0,

            Note =
                "Hoàn tiền toàn bộ",

            ReferenceCode =
                "REF-025",

            IsRefund =
                true
        };


    // Act

    var result =
        await _service
            .RefundPaymentAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 5
            );


    // Assert

    Assert.Equal(
        200,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .RefundSuccess,
        result.Message
    );

    Assert.NotNull(
        result.Content
    );


    Assert.Equal(
        0,
        invoice.PaidAmount
    );

    Assert.Equal(
        (byte)InvoiceStatus.Unpaid,
        invoice.Status
    );


    Assert.NotNull(
        addedRefund
    );

    Assert.Equal(
        1,
        addedRefund!.InvoiceId
    );

    Assert.Equal(
        -1_000_000,
        addedRefund.Amount
    );

    Assert.True(
        addedRefund.IsRefund
    );

    Assert.Equal(
        5,
        addedRefund.ReceivedBy
    );

    Assert.Equal(
        "REF-025",
        addedRefund.ReferenceCode
    );


    _unitOfWorkMock.Verify(
        x =>
            x.BeginTransactionAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.CommitTransactionAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.RollbackTransactionAsync(),
        Times.Never
    );

    paymentRepositoryMock.Verify(
        x =>
            x.AddAsync(
                It.IsAny<Payment>()
            ),
        Times.Once
    );
}


// =====================================================
// CANCEL INVOICE
// =====================================================


// =====================================================
// TEST 26
//
// Invalid invoice ID.
//
// Expected:
// 400
// InvoiceNotFound
// =====================================================

[Fact]
public async Task CancelInvoiceAsync_ShouldReturn400_WhenInvoiceIdIsInvalid()
{
    var request =
        new CancelInvoiceRequestDTO
        {
            CancelReason =
                "Test cancel"
        };


    var result =
        await _service
            .CancelInvoiceAsync(
                invoiceId: 0,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .InvoiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 27
//
// Null request.
//
// Expected:
// 400
// CancelRequestRequired
// =====================================================

[Fact]
public async Task CancelInvoiceAsync_ShouldReturn400_WhenRequestIsNull()
{
    var result =
        await _service
            .CancelInvoiceAsync(
                invoiceId: 1,
                request: null!,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .CancelRequestRequired,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 28
//
// Invoice không tồn tại.
//
// Expected:
// 404
// InvoiceNotFound
// =====================================================

[Fact]
public async Task CancelInvoiceAsync_ShouldReturn404_WhenInvoiceDoesNotExist()
{
    SetupInvoiceRepository();

    var request =
        new CancelInvoiceRequestDTO
        {
            CancelReason =
                "Test cancel"
        };


    var result =
        await _service
            .CancelInvoiceAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        404,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .InvoiceNotFound,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 29
//
// Invoice đã cancelled.
//
// Expected:
// 400
// InvoiceAlreadyCancelled
// =====================================================

[Fact]
public async Task CancelInvoiceAsync_ShouldReturn400_WhenInvoiceIsAlreadyCancelled()
{
    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-029",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                500_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                (byte)InvoiceStatus.Cancelled,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        new CancelInvoiceRequestDTO
        {
            CancelReason =
                "Cancel again"
        };


    var result =
        await _service
            .CancelInvoiceAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .InvoiceAlreadyCancelled,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 30
//
// Invoice đã có payment.
//
// PaidAmount != 0
//
// Expected:
// 400
// CannotCancelPaidInvoice
// =====================================================

[Fact]
public async Task CancelInvoiceAsync_ShouldReturn400_WhenInvoiceHasPaidAmount()
{
    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-030",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                1_000_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                100_000,

            Status =
                (byte)InvoiceStatus.PartiallyPaid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    var request =
        new CancelInvoiceRequestDTO
        {
            CancelReason =
                "Không muốn tiếp tục"
        };


    var result =
        await _service
            .CancelInvoiceAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 1
            );


    Assert.Equal(
        400,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .CannotCancelPaidInvoice,
        result.Message
    );

    VerifyNoTransaction();
}


// =====================================================
// TEST 31
//
// Cancel thành công.
//
// Expected:
// 200
// CancelSuccess
//
// Kiểm tra:
// - Status -> Cancelled
// - CancelReason Trim
// - CancelledAt
// - BeginTransaction
// - SaveChanges
// - Commit
// =====================================================

[Fact]
public async Task CancelInvoiceAsync_ShouldReturn200_WhenCancellationIsSuccessful()
{
    // Arrange

    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-031",

            PatientId =
                1,

            PatientName =
                "Test Patient",

            TotalAmount =
                500_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                (byte)InvoiceStatus.Unpaid,

            CreatedAt =
                DateTime.Now
        };

    SetupInvoiceRepository(
        invoice
    );

    SetupInvoiceItemRepository();

    SetupPaymentRepository();

    SetupAuditLogRepositoryForInvoiceTests();


    var request =
        new CancelInvoiceRequestDTO
        {
            CancelReason =
                "   Khách hàng không tiếp tục khám   "
        };


    // Act

    var result =
        await _service
            .CancelInvoiceAsync(
                invoiceId: 1,
                request: request,
                currentUserId: 7
            );


    // Assert

    Assert.Equal(
        200,
        result.StatusCode
    );

    Assert.Equal(
        InvoiceResponseMessageDTO
            .CancelSuccess,
        result.Message
    );

    Assert.NotNull(
        result.Content
    );


    Assert.Equal(
        (byte)InvoiceStatus.Cancelled,
        invoice.Status
    );

    Assert.Equal(
        "Khách hàng không tiếp tục khám",
        invoice.CancelReason
    );

    Assert.NotNull(
        invoice.CancelledAt
    );


    _unitOfWorkMock.Verify(
        x =>
            x.BeginTransactionAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.SaveChangesAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.CommitTransactionAsync(),
        Times.Once
    );

    _unitOfWorkMock.Verify(
        x =>
            x.RollbackTransactionAsync(),
        Times.Never
    );
}


// =====================================================
// GET INVOICE PDF
// =====================================================


// =====================================================
// TEST 32
//
// Invalid invoice ID.
//
// Expected:
// null
// =====================================================

[Fact]
public async Task GetInvoicePdfAsync_ShouldReturnNull_WhenInvoiceIdIsInvalid()
{
    var result =
        await _service
            .GetInvoicePdfAsync(
                invoiceId: 0
            );


    Assert.Null(
        result
    );
}


// =====================================================
// TEST 33
//
// Invoice không tồn tại.
//
// Expected:
// null
// =====================================================

[Fact]
public async Task GetInvoicePdfAsync_ShouldReturnNull_WhenInvoiceDoesNotExist()
{
    SetupInvoiceRepository();


    var result =
        await _service
            .GetInvoicePdfAsync(
                invoiceId: 1
            );


    Assert.Null(
        result
    );
}


// =====================================================
// TEST 34
//
// Invoice tồn tại.
//
// Expected:
// PDF byte[] được tạo.
// =====================================================

[Fact]
public async Task GetInvoicePdfAsync_ShouldReturnPdf_WhenInvoiceExists()
{
    // Arrange

    // QuestPDF hỗ trợ Evaluation license
    // cho learning/evaluation/testing.
    QuestPDF.Settings.License =
        QuestPDF.Infrastructure.LicenseType.Evaluation;


    var invoice =
        new InvoiceEntity
        {
            Id = 1,

            InvoiceNo =
                "INV-034",

            PatientId =
                1,

            PatientName =
                "Nguyen Van A",

            AppointmentId =
                10,

            MedicalRecordId =
                20,

            TotalAmount =
                500_000,

            DiscountAmount =
                0,

            TaxAmount =
                0,

            InsuranceAmount =
                0,

            PaidAmount =
                0,

            Status =
                (byte)InvoiceStatus.Unpaid,

            CreatedAt =
                DateTime.Now
        };


    SetupInvoiceRepository(
        invoice
    );

    SetupInvoiceItemRepository();

    SetupPaymentRepository();


    // Act

    var result =
        await _service
            .GetInvoicePdfAsync(
                invoiceId: 1
            );


    // Assert

    Assert.NotNull(
        result
    );

    Assert.NotEmpty(
        result!
    );
}


// =====================================================
// TEST 35
//
// Exception khi lấy invoice.
//
// GetInvoicePdfAsync phải catch exception
// và trả null.
//
// Expected:
// null
// =====================================================

[Fact]
public async Task GetInvoicePdfAsync_ShouldReturnNull_WhenExceptionOccurs()
{
    // Arrange

    var repositoryMock =
        new Mock<IInvoiceRepository>();

    repositoryMock
        .Setup(
            x =>
                x.WhereSql(
                    It.IsAny<
                        Expression<
                            Func<
                                InvoiceEntity,
                                bool
                            >
                        >
                    >()
                )
        )
        .Throws(
            new InvalidOperationException(
                "Test exception"
            )
        );

    _unitOfWorkMock
        .Setup(
            x =>
                x.InvoiceRepository
        )
        .Returns(
            repositoryMock.Object
        );


    // Act

    var result =
        await _service
            .GetInvoicePdfAsync(
                invoiceId: 1
            );


    // Assert

    Assert.Null(
        result
    );
}
}