using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Wasl.Models;

namespace Wasl.Data;

public partial class WaslDbContext : DbContext
{
    public WaslDbContext()
    {
    }

    public WaslDbContext(DbContextOptions<WaslDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<RevenueReport> RevenueReports { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipmentRequest> ShipmentRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admin__AD0500A66343DEC1");

            entity.ToTable("Admin");

            entity.Property(e => e.AdminId).HasColumnName("adminId");
            entity.Property(e => e.AdminEmail)
                .HasMaxLength(150)
                .HasColumnName("adminEmail");
            entity.Property(e => e.AdminFirstName)
                .HasMaxLength(100)
                .HasColumnName("adminFirstName");
            entity.Property(e => e.AdminLastName)
                .HasMaxLength(100)
                .HasColumnName("adminLastName");
            entity.Property(e => e.AdminPhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("adminPhoneNumber");
            entity.Property(e => e.AdminRole)
                .HasMaxLength(50)
                .HasColumnName("adminRole");
            entity.Property(e => e.AdminStatus)
                .HasMaxLength(50)
                .HasColumnName("adminStatus");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Admins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admin_User");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.BidId).HasName("PK__Bid__48E98F5849ED964D");

            entity.ToTable("Bid");

            entity.Property(e => e.BidId).HasColumnName("bidId");
            entity.Property(e => e.BidNotes)
                .HasMaxLength(255)
                .HasColumnName("bidNotes");
            entity.Property(e => e.BidPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("bidPrice");
            entity.Property(e => e.BidStatus)
                .HasMaxLength(50)
                .HasColumnName("bidStatus");
            entity.Property(e => e.EstimatedDeliveryDays).HasColumnName("estimatedDeliveryDays");
            entity.Property(e => e.ProviderId).HasColumnName("providerId");
            entity.Property(e => e.ShipmentRequestId).HasColumnName("shipmentRequestId");
            entity.Property(e => e.SubmitDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("submitDate");

            entity.HasOne(d => d.Provider).WithMany(p => p.Bids)
                .HasForeignKey(d => d.ProviderId)
                .HasConstraintName("FK_Bid_Provider");

            entity.HasOne(d => d.ShipmentRequest).WithMany(p => p.Bids)
                .HasForeignKey(d => d.ShipmentRequestId)
                .HasConstraintName("FK_Bid_ShipmentRequest");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId).HasName("PK__Company__AD5459907BDE6BEB");

            entity.ToTable("Company");

            entity.Property(e => e.CompanyId).HasColumnName("companyId");
            entity.Property(e => e.AdminId).HasColumnName("adminId");
            entity.Property(e => e.BusinessRegistrationNumber)
                .HasMaxLength(100)
                .HasColumnName("businessRegistrationNumber");
            entity.Property(e => e.CompanyAddress)
                .HasMaxLength(255)
                .HasColumnName("companyAddress");
            entity.Property(e => e.CompanyCity)
                .HasMaxLength(100)
                .HasColumnName("companyCity");
            entity.Property(e => e.CompanyEmail)
                .HasMaxLength(150)
                .HasColumnName("companyEmail");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(150)
                .HasColumnName("companyName");
            entity.Property(e => e.CompanyRegion)
                .HasMaxLength(100)
                .HasColumnName("companyRegion");
            entity.Property(e => e.CompanyStatus)
                .HasMaxLength(50)
                .HasColumnName("companyStatus");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(false)
                .HasColumnName("isApproved");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Admin).WithMany(p => p.Companies)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_Company_Admin");

            entity.HasOne(d => d.User).WithMany(p => p.Companies)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Company_User");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__13820941D420FFE9");

            entity.ToTable("Contract");

            entity.Property(e => e.ContractId).HasColumnName("contractId");
            entity.Property(e => e.BidId).HasColumnName("bidId");
            entity.Property(e => e.CompanyId).HasColumnName("companyId");
            entity.Property(e => e.ContractDocument)
                .HasMaxLength(255)
                .HasColumnName("contractDocument");
            entity.Property(e => e.ProviderId).HasColumnName("providerId");
            entity.Property(e => e.ShipmentId).HasColumnName("shipmentId");
            entity.Property(e => e.SignDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("signDate");

            entity.HasOne(d => d.Bid).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.BidId)
                .HasConstraintName("FK_Contract_Bid");

            entity.HasOne(d => d.Company).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_Contract_Company");

            entity.HasOne(d => d.Provider).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ProviderId)
                .HasConstraintName("FK_Contract_Provider");

            entity.HasOne(d => d.Shipment).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ShipmentId)
                .HasConstraintName("FK_Contract_Shipment");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__2613FD243DBA5040");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("feedbackId");
            entity.Property(e => e.Comments)
                .HasMaxLength(255)
                .HasColumnName("comments");
            entity.Property(e => e.CompanyId).HasColumnName("companyId");
            entity.Property(e => e.FeedbackDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("feedbackDate");
            entity.Property(e => e.ProviderId).HasColumnName("providerId");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ShipmentId).HasColumnName("shipmentId");

            entity.HasOne(d => d.Company).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_Feedback_Company");

            entity.HasOne(d => d.Provider).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ProviderId)
                .HasConstraintName("FK_Feedback_Provider");

            entity.HasOne(d => d.Shipment).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ShipmentId)
                .HasConstraintName("FK_Feedback_Shipment");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__1252416C7D7F4882");

            entity.Property(e => e.InvoiceId).HasColumnName("invoiceId");
            entity.Property(e => e.ContractId).HasColumnName("contractId");
            entity.Property(e => e.InvoiceDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("invoiceDate");
            entity.Property(e => e.InvoiceDueDate)
                .HasColumnType("datetime")
                .HasColumnName("invoiceDueDate");
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(100)
                .HasColumnName("invoiceNumber");
            entity.Property(e => e.PaymentId).HasColumnName("paymentId");

            entity.HasOne(d => d.Contract).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("FK_Invoice_Contract");

            entity.HasOne(d => d.Payment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.PaymentId)
                .HasConstraintName("FK_Invoice_Payment");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__A0D9EFC6F858EDA8");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).HasColumnName("paymentId");
            entity.Property(e => e.InvoiceId).HasColumnName("invoiceId");
            entity.Property(e => e.PaymentAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("paymentAmount");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("paymentDate");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("paymentMethod");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasColumnName("paymentStatus");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transactionId");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Payment_Invoice");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.ProviderId).HasName("PK__Provider__107017F3E846348D");

            entity.ToTable("Provider");

            entity.Property(e => e.ProviderId).HasColumnName("providerId");
            entity.Property(e => e.AdminId).HasColumnName("adminId");
            entity.Property(e => e.BusinessRegistrationNumber)
                .HasMaxLength(100)
                .HasColumnName("businessRegistrationNumber");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(false)
                .HasColumnName("isApproved");
            entity.Property(e => e.ProviderAddress)
                .HasMaxLength(255)
                .HasColumnName("providerAddress");
            entity.Property(e => e.ProviderCity)
                .HasMaxLength(100)
                .HasColumnName("providerCity");
            entity.Property(e => e.ProviderEmail)
                .HasMaxLength(150)
                .HasColumnName("providerEmail");
            entity.Property(e => e.ProviderName)
                .HasMaxLength(150)
                .HasColumnName("providerName");
            entity.Property(e => e.ProviderPhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("providerPhoneNumber");
            entity.Property(e => e.ProviderRegion)
                .HasMaxLength(100)
                .HasColumnName("providerRegion");
            entity.Property(e => e.ServiceDescription)
                .HasMaxLength(255)
                .HasColumnName("serviceDescription");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Admin).WithMany(p => p.Providers)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_Provider_Admin");

            entity.HasOne(d => d.User).WithMany(p => p.Providers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Provider_User");
        });

        modelBuilder.Entity<RevenueReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__RevenueR__1C9B4E2D269DEFB1");

            entity.ToTable("RevenueReport");

            entity.Property(e => e.ReportId).HasColumnName("reportId");
            entity.Property(e => e.AdminId).HasColumnName("adminId");
            entity.Property(e => e.GenerateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("generateDate");
            entity.Property(e => e.PeriodCovered)
                .HasMaxLength(100)
                .HasColumnName("periodCovered");
            entity.Property(e => e.TotalCommission)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("totalCommission");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("totalRevenue");

            entity.HasOne(d => d.Admin).WithMany(p => p.RevenueReports)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_RevenueReport_Admin");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.ShipmentId).HasName("PK__Shipment__47217801B2680510");

            entity.ToTable("Shipment");

            entity.Property(e => e.ShipmentId).HasColumnName("shipmentId");
            entity.Property(e => e.ActualDeliveryTime)
                .HasColumnType("datetime")
                .HasColumnName("actualDeliveryTime");
            entity.Property(e => e.ActualStartDate)
                .HasColumnType("datetime")
                .HasColumnName("actualStartDate");
            entity.Property(e => e.ContractId).HasColumnName("contractId");
            entity.Property(e => e.CurrentStatus)
                .HasMaxLength(50)
                .HasColumnName("currentStatus");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("trackingNumber");

            entity.HasOne(d => d.Contract).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("FK_Shipment_Contract");
        });

        modelBuilder.Entity<ShipmentRequest>(entity =>
        {
            entity.HasKey(e => e.ShipmentRequestId).HasName("PK__Shipment__902A6491ED31BF60");

            entity.ToTable("ShipmentRequest");

            entity.Property(e => e.ShipmentRequestId).HasColumnName("shipmentRequestId");
            entity.Property(e => e.CompanyId).HasColumnName("companyId");
            entity.Property(e => e.DeliveryCity)
                .HasMaxLength(100)
                .HasColumnName("deliveryCity");
            entity.Property(e => e.DeliveryDeadline)
                .HasColumnType("datetime")
                .HasColumnName("deliveryDeadline");
            entity.Property(e => e.DeliveryLocation)
                .HasMaxLength(255)
                .HasColumnName("deliveryLocation");
            entity.Property(e => e.DeliveryRegion)
                .HasMaxLength(100)
                .HasColumnName("deliveryRegion");
            entity.Property(e => e.GoodsType)
                .HasMaxLength(100)
                .HasColumnName("goodsType");
            entity.Property(e => e.PickupCity)
                .HasMaxLength(100)
                .HasColumnName("pickupCity");
            entity.Property(e => e.PickupLocation)
                .HasMaxLength(255)
                .HasColumnName("pickupLocation");
            entity.Property(e => e.PickupRegion)
                .HasMaxLength(100)
                .HasColumnName("pickupRegion");
            entity.Property(e => e.ProviderId).HasColumnName("providerId");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("requestDate");
            entity.Property(e => e.SpecialInstructions)
                .HasMaxLength(255)
                .HasColumnName("specialInstructions");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdateAt)
                .HasColumnType("datetime")
                .HasColumnName("updateAt");
            entity.Property(e => e.WeightKg)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("weightKg");

            entity.HasOne(d => d.Company).WithMany(p => p.ShipmentRequests)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_ShipmentRequest_Company");

            entity.HasOne(d => d.Provider).WithMany(p => p.ShipmentRequests)
                .HasForeignKey(d => d.ProviderId)
                .HasConstraintName("FK_ShipmentRequest_Provider");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CFF9A9D6A0B");

            entity.ToTable("User");

            entity.HasIndex(e => e.UserEmail, "UQ__User__D54ADF55040E3FF2").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(150)
                .HasColumnName("userEmail");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("userName");
            entity.Property(e => e.UserPassword)
                .HasMaxLength(255)
                .HasColumnName("userPassword");
            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .HasColumnName("userRole");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
