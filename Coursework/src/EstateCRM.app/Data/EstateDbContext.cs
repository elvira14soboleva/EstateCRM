using System;
using System.Collections.Generic;
using EstateCRM.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Data;

public partial class EstateDbContext : DbContext
{
    public EstateDbContext(DbContextOptions<EstateDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientRequest> ClientRequests { get; set; }

    public virtual DbSet<Deal> Deals { get; set; }

    public virtual DbSet<DealType> DealTypes { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<PropertyStatus> PropertyStatuses { get; set; }

    public virtual DbSet<PropertyType> PropertyTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Viewing> Viewings { get; set; }

    public virtual DbSet<ViewingStatus> ViewingStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("pk_clients");

            entity.ToTable("clients");

            entity.HasIndex(e => new { e.PassportSeries, e.PassportNumber }, "uq_clients_passport").IsUnique();

            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(50)
                .HasColumnName("middle_name");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("passport_number");
            entity.Property(e => e.PassportSeries)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("passport_series");
            entity.Property(e => e.Phone)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("(CONVERT([date],sysdatetime()))", "df_clients_registration_date")
                .HasColumnName("registration_date");
        });

        modelBuilder.Entity<ClientRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("pk_client_requests");

            entity.ToTable("client_requests");

            entity.HasIndex(e => e.ClientId, "ix_requests_client");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.AreaMin)
                .HasColumnType("decimal(7, 2)")
                .HasColumnName("area_min");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "df_client_requests_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DealTypeId).HasColumnName("deal_type_id");
            entity.Property(e => e.DistrictId).HasColumnName("district_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "df_client_requests_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.PriceMax)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("price_max");
            entity.Property(e => e.PriceMin)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("price_min");
            entity.Property(e => e.PropertyTypeId).HasColumnName("property_type_id");
            entity.Property(e => e.Rooms).HasColumnName("rooms");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientRequests)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_client_requests_client_id");

            entity.HasOne(d => d.DealType).WithMany(p => p.ClientRequests)
                .HasForeignKey(d => d.DealTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_client_requests_deal_type_id");

            entity.HasOne(d => d.District).WithMany(p => p.ClientRequests)
                .HasForeignKey(d => d.DistrictId)
                .HasConstraintName("fk_client_requests_district_id");

            entity.HasOne(d => d.PropertyType).WithMany(p => p.ClientRequests)
                .HasForeignKey(d => d.PropertyTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_client_requests_property_type_id");
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.DealId).HasName("pk_deals");

            entity.ToTable("deals");

            entity.HasIndex(e => new { e.DealDate, e.EmployeeId }, "ix_deals_period");

            entity.HasIndex(e => e.ContractNumber, "uq_deals_contract_number").IsUnique();

            entity.HasIndex(e => e.ViewingId, "uq_deals_viewing_id").IsUnique();

            entity.Property(e => e.DealId).HasColumnName("deal_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CommissionAmount)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("commission_amount");
            entity.Property(e => e.CommissionPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("commission_percent");
            entity.Property(e => e.ContractFile)
                .HasMaxLength(260)
                .HasColumnName("contract_file");
            entity.Property(e => e.ContractNumber)
                .HasMaxLength(30)
                .HasColumnName("contract_number");
            entity.Property(e => e.DealDate)
                .HasDefaultValueSql("(CONVERT([date],sysdatetime()))", "df_deals_deal_date")
                .HasColumnName("deal_date");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.ViewingId).HasColumnName("viewing_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.Deals)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_deals_employee_id");

            entity.HasOne(d => d.Viewing).WithOne(p => p.Deal)
                .HasForeignKey<Deal>(d => d.ViewingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_deals_viewing_id");
        });

        modelBuilder.Entity<DealType>(entity =>
        {
            entity.HasKey(e => e.DealTypeId).HasName("pk_deal_types");

            entity.ToTable("deal_types");

            entity.HasIndex(e => e.DealTypeName, "uq_deal_types_deal_type_name").IsUnique();

            entity.Property(e => e.DealTypeId).HasColumnName("deal_type_id");
            entity.Property(e => e.CommissionPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("commission_percent");
            entity.Property(e => e.DealTypeName)
                .HasMaxLength(30)
                .HasColumnName("deal_type_name");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.DistrictId).HasName("pk_districts");

            entity.ToTable("districts");

            entity.HasIndex(e => e.DistrictName, "uq_districts_district_name").IsUnique();

            entity.Property(e => e.DistrictId).HasColumnName("district_id");
            entity.Property(e => e.DistrictName)
                .HasMaxLength(60)
                .HasColumnName("district_name");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("pk_employees");

            entity.ToTable("employees");

            entity.HasIndex(e => e.Login, "uq_employees_login").IsUnique();

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "df_employees_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.Login)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("login");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(50)
                .HasColumnName("middle_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_employees_role_id");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("pk_properties");

            entity.ToTable("properties");

            entity.HasIndex(e => new { e.StatusId, e.DealTypeId, e.PropertyTypeId, e.Price }, "ix_properties_search");

            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.Area)
                .HasColumnType("decimal(7, 2)")
                .HasColumnName("area");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "df_properties_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DealTypeId).HasColumnName("deal_type_id");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.DistrictId).HasColumnName("district_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Floor).HasColumnName("floor");
            entity.Property(e => e.OwnerClientId).HasColumnName("owner_client_id");
            entity.Property(e => e.PhotoPath)
                .HasMaxLength(260)
                .HasColumnName("photo_path");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("price");
            entity.Property(e => e.PropertyTypeId).HasColumnName("property_type_id");
            entity.Property(e => e.Rooms).HasColumnName("rooms");
            entity.Property(e => e.StatusId)
                .HasDefaultValue(1, "df_properties_status_id")
                .HasColumnName("status_id");

            entity.HasOne(d => d.DealType).WithMany(p => p.Properties)
                .HasForeignKey(d => d.DealTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_properties_deal_type_id");

            entity.HasOne(d => d.District).WithMany(p => p.Properties)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_properties_district_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.Properties)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_properties_employee_id");

            entity.HasOne(d => d.OwnerClient).WithMany(p => p.Properties)
                .HasForeignKey(d => d.OwnerClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_properties_owner_client_id");

            entity.HasOne(d => d.PropertyType).WithMany(p => p.Properties)
                .HasForeignKey(d => d.PropertyTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_properties_property_type_id");

            entity.HasOne(d => d.Status).WithMany(p => p.Properties)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_properties_status_id");
        });

        modelBuilder.Entity<PropertyStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("pk_property_statuses");

            entity.ToTable("property_statuses");

            entity.HasIndex(e => e.StatusName, "uq_property_statuses_status_name").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(30)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<PropertyType>(entity =>
        {
            entity.HasKey(e => e.PropertyTypeId).HasName("pk_property_types");

            entity.ToTable("property_types");

            entity.HasIndex(e => e.TypeName, "uq_property_types_type_name").IsUnique();

            entity.Property(e => e.PropertyTypeId).HasColumnName("property_type_id");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("pk_roles");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "uq_roles_role_name").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(30)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Viewing>(entity =>
        {
            entity.HasKey(e => e.ViewingId).HasName("pk_viewings");

            entity.ToTable("viewings");

            entity.HasIndex(e => new { e.PropertyId, e.ViewingDatetime }, "ix_viewings_property_time");

            entity.HasIndex(e => e.RequestId, "ix_viewings_request");

            entity.Property(e => e.ViewingId).HasColumnName("viewing_id");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.ResultComment)
                .HasMaxLength(500)
                .HasColumnName("result_comment");
            entity.Property(e => e.ViewingDatetime)
                .HasPrecision(0)
                .HasColumnName("viewing_datetime");
            entity.Property(e => e.ViewingStatusId)
                .HasDefaultValue(1, "df_viewings_viewing_status_id")
                .HasColumnName("viewing_status_id");

            entity.HasOne(d => d.Property).WithMany(p => p.Viewings)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_viewings_property_id");

            entity.HasOne(d => d.Request).WithMany(p => p.Viewings)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_viewings_request_id");

            entity.HasOne(d => d.ViewingStatus).WithMany(p => p.Viewings)
                .HasForeignKey(d => d.ViewingStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_viewings_viewing_status_id");
        });

        modelBuilder.Entity<ViewingStatus>(entity =>
        {
            entity.HasKey(e => e.ViewingStatusId).HasName("pk_viewing_statuses");

            entity.ToTable("viewing_statuses");

            entity.HasIndex(e => e.StatusName, "uq_viewing_statuses_status_name").IsUnique();

            entity.Property(e => e.ViewingStatusId).HasColumnName("viewing_status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(30)
                .HasColumnName("status_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
