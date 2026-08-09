namespace RetailCommerce.Domain.Common;

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
}

public enum CustomerType
{
    Retail = 0,
    Wholesale = 1,
    Corporate = 2,
    WalkIn = 3,
}

public enum PartyStatus
{
    Active = 0,
    Inactive = 1,
    OnHold = 2,
}

public enum StockMovementKind
{
    OpeningStock = 0,
    Sale = 1,
    SaleReturn = 2,
    TransferOut = 3,
    TransferIn = 4,
    PurchaseReceipt = 5,
    Adjustment = 6,
}

public enum PurchaseOrderStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Received = 3,
    Completed = 4,
    Cancelled = 5,
}

public enum TransferStatus
{
    Draft = 0,
    InTransit = 1,
    Completed = 2,
    Cancelled = 3,
}

public enum OrderChannel
{
    Pos = 0,
    Online = 1,
    Wholesale = 2,
}

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Refunded = 3,
    Cancelled = 4,
}

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    Wallet = 2,
    StoreCredit = 3,
    BankTransfer = 4,
    Credit = 5,
}

public enum DiscountValueType
{
    Percentage = 0,
    FixedAmount = 1,
}

public enum ShiftStatus
{
    Open = 0,
    Closed = 1,
}
