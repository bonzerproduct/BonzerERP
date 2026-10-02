namespace Bonzer.Domain.Enums;

public enum FeeFrequency { OneTime, Annual, Term, Quarterly, Monthly }

public enum LateFeeType { None, FixedOnce, PerDay, Percentage }

public enum DiscountType { Percentage, FixedAmount }

public enum InvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled, Waived }

public enum PaymentMethod { Cash, Cheque, DemandDraft, BankTransfer, Upi, Card, NetBanking, OnlineGateway, Wallet }

public enum PaymentStatus { Pending, Completed, Failed, Bounced, Refunded, Cancelled }
