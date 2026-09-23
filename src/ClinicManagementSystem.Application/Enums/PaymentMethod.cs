namespace ClinicManagementSystem.Application.Enums;


// =====================================================
// PAYMENT METHOD
// =====================================================

public enum PaymentMethod : byte
{
    Cash = 0,

    Card = 1,

    BankTransfer = 2,

    EWallet = 3
}