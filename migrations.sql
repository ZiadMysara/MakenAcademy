CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE TABLE "Tenants" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Subdomain" character varying(50) NOT NULL,
        "LogoUrl" character varying(500),
        "PrimaryColor" character varying(7),
        "SecondaryColor" character varying(7),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" uuid,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" uuid,
        CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "TenantId" uuid,
        "Email" character varying(256) NOT NULL,
        "PasswordHash" character varying(256) NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "Role" integer NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "LastLoginAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" uuid,
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" uuid,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Users_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE INDEX "IX_Tenant_IsDeleted" ON "Tenants" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Tenant_Subdomain" ON "Tenants" ("Subdomain");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_User_Email_TenantId" ON "Users" ("Email", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE INDEX "IX_User_IsDeleted" ON "Users" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    CREATE INDEX "IX_User_TenantId" ON "Users" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209033214_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260209033214_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260210212533_AddRefreshTokenEntity') THEN
    CREATE TABLE "RefreshTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Token" character varying(500) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "IsRevoked" boolean NOT NULL DEFAULT FALSE,
        "RevokedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" uuid,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedBy" uuid,
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260210212533_AddRefreshTokenEntity') THEN
    CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260210212533_AddRefreshTokenEntity') THEN
    CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260210212533_AddRefreshTokenEntity') THEN
    CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260210212533_AddRefreshTokenEntity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260210212533_AddRefreshTokenEntity', '8.0.11');
    END IF;
END $EF$;
COMMIT;

