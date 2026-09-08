using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public byte Method { get; set; }

    public DateTime PaidAt { get; set; }

    public string? Note { get; set; }

    public string? ReferenceCode { get; set; }

    public int? ReceivedBy { get; set; }

    public bool IsRefund { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual User? ReceivedByNavigation { get; set; }
}
