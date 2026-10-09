-- Generated from SqLiteAppDbContext. Apply to an empty SQLite database.
PRAGMA foreign_keys = ON;

CREATE TABLE "AccountGroup" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AccountGroup" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "ParentId" TEXT NOT NULL,
    CONSTRAINT "FK_AccountGroup_AccountGroup_ParentId" FOREIGN KEY ("ParentId") REFERENCES "AccountGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "CategoryGroup" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CategoryGroup" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "ParentId" TEXT NOT NULL,
    CONSTRAINT "FK_CategoryGroup_CategoryGroup_ParentId" FOREIGN KEY ("ParentId") REFERENCES "CategoryGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "CorrespondentGroup" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CorrespondentGroup" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "ParentId" TEXT NOT NULL,
    CONSTRAINT "FK_CorrespondentGroup_CorrespondentGroup_ParentId" FOREIGN KEY ("ParentId") REFERENCES "CorrespondentGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Currency" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Currency" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Code" TEXT NOT NULL,
    "EnglishName" TEXT NOT NULL,
    "Symbol" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "Order" INTEGER NOT NULL
);


CREATE TABLE "LocalConfig" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_LocalConfig" PRIMARY KEY,
    "LocalDatasetKey" TEXT NOT NULL,
    "AccountNameOrder" INTEGER NOT NULL,
    "ConflictPriority" INTEGER NOT NULL,
    "SyncTrigger" INTEGER NOT NULL,
    "SnapshotRevision" INTEGER NOT NULL,
    "AccountNameSeparator" TEXT NOT NULL,
    "AccountNameAddCurrency" INTEGER NOT NULL
);


CREATE TABLE "ProjectGroup" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ProjectGroup" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "ParentId" TEXT NOT NULL,
    CONSTRAINT "FK_ProjectGroup_ProjectGroup_ParentId" FOREIGN KEY ("ParentId") REFERENCES "ProjectGroup" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ReportGroup" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ReportGroup" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "ParentId" TEXT NOT NULL,
    CONSTRAINT "FK_ReportGroup_ReportGroup_ParentId" FOREIGN KEY ("ParentId") REFERENCES "ReportGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "TemplateGroup" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_TemplateGroup" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "ParentId" TEXT NOT NULL,
    CONSTRAINT "FK_TemplateGroup_TemplateGroup_ParentId" FOREIGN KEY ("ParentId") REFERENCES "TemplateGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Transaction" (
    "Id" BLOB NOT NULL CONSTRAINT "PK_Transaction" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "DateTime" TEXT NOT NULL,
    "State" INTEGER NOT NULL,
    "Description" TEXT NULL
);


CREATE TABLE "Category" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Category" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "GroupId" TEXT NOT NULL,
    CONSTRAINT "FK_Category_CategoryGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "CategoryGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Correspondent" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Correspondent" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "GroupId" TEXT NOT NULL,
    CONSTRAINT "FK_Correspondent_CorrespondentGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "CorrespondentGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "CurrencyRate" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CurrencyRate" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "CurrencyId" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "Rate" INTEGER NOT NULL,
    "Description" TEXT NULL,
    CONSTRAINT "FK_CurrencyRate_Currency_CurrencyId" FOREIGN KEY ("CurrencyId") REFERENCES "Currency" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Project" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Project" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "GroupId" TEXT NOT NULL,
    CONSTRAINT "FK_Project_ProjectGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "ProjectGroup" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Report" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Report" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Json" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "GroupId" TEXT NOT NULL,
    CONSTRAINT "FK_Report_ReportGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "ReportGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Template" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Template" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "GroupId" TEXT NOT NULL,
    CONSTRAINT "FK_Template_TemplateGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "TemplateGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Account" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Account" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "IsFavorite" INTEGER NOT NULL,
    "GroupId" TEXT NOT NULL,
    "CurrencyId" TEXT NOT NULL,
    "CategoryId" TEXT NULL,
    "CorrespondentId" TEXT NULL,
    "ProjectId" TEXT NULL,
    CONSTRAINT "FK_Account_AccountGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "AccountGroup" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Account_Category_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Category" ("Id"),
    CONSTRAINT "FK_Account_Correspondent_CorrespondentId" FOREIGN KEY ("CorrespondentId") REFERENCES "Correspondent" ("Id"),
    CONSTRAINT "FK_Account_Currency_CurrencyId" FOREIGN KEY ("CurrencyId") REFERENCES "Currency" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Account_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id")
);


CREATE TABLE "SystemConfig" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SystemConfig" PRIMARY KEY,
    "EditRevision" INTEGER NULL,
    "DeleteRevision" INTEGER NULL,
    "ModificationType" INTEGER NOT NULL,
    "BaseCurrencyId" TEXT NOT NULL,
    "MasterDatasetKey" TEXT NOT NULL,
    "BalancingAccountId" TEXT NULL,
    "AmountPrecision" INTEGER NOT NULL,
    "RatePrecision" INTEGER NOT NULL,
    CONSTRAINT "FK_SystemConfig_Account_BalancingAccountId" FOREIGN KEY ("BalancingAccountId") REFERENCES "Account" ("Id"),
    CONSTRAINT "FK_SystemConfig_Currency_BaseCurrencyId" FOREIGN KEY ("BaseCurrencyId") REFERENCES "Currency" ("Id") ON DELETE CASCADE
);


CREATE TABLE "TemplateEntry" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_TemplateEntry" PRIMARY KEY,
    "TemplateId" TEXT NOT NULL,
    "AccountId" TEXT NOT NULL,
    "Position" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    CONSTRAINT "FK_TemplateEntry_Account_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Account" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TemplateEntry_Template_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES "Template" ("Id") ON DELETE CASCADE
);


CREATE TABLE "TransactionEntry" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_TransactionEntry" PRIMARY KEY,
    "TransactionId" BLOB NOT NULL,
    "AccountId" TEXT NOT NULL,
    "Position" INTEGER NOT NULL,
    "CumulativeAmount" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    "Rate" INTEGER NOT NULL,
    CONSTRAINT "FK_TransactionEntry_Account_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Account" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TransactionEntry_Transaction_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES "Transaction" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_Account_CategoryId" ON "Account" ("CategoryId");


CREATE INDEX "IX_Account_CorrespondentId" ON "Account" ("CorrespondentId");


CREATE INDEX "IX_Account_CurrencyId" ON "Account" ("CurrencyId");


CREATE INDEX "IX_Account_GroupId" ON "Account" ("GroupId");


CREATE INDEX "IX_Account_ProjectId" ON "Account" ("ProjectId");


CREATE INDEX "IX_AccountGroup_ParentId" ON "AccountGroup" ("ParentId");


CREATE INDEX "IX_Category_GroupId" ON "Category" ("GroupId");


CREATE INDEX "IX_CategoryGroup_ParentId" ON "CategoryGroup" ("ParentId");


CREATE INDEX "IX_Correspondent_GroupId" ON "Correspondent" ("GroupId");


CREATE INDEX "IX_CorrespondentGroup_ParentId" ON "CorrespondentGroup" ("ParentId");


CREATE INDEX "IX_CurrencyRate_CurrencyId" ON "CurrencyRate" ("CurrencyId");


CREATE INDEX "IX_Project_GroupId" ON "Project" ("GroupId");


CREATE INDEX "IX_ProjectGroup_ParentId" ON "ProjectGroup" ("ParentId");


CREATE INDEX "IX_SystemConfig_BalancingAccountId" ON "SystemConfig" ("BalancingAccountId");


CREATE INDEX "IX_SystemConfig_BaseCurrencyId" ON "SystemConfig" ("BaseCurrencyId");


CREATE INDEX "IX_Template_GroupId" ON "Template" ("GroupId");


CREATE INDEX "IX_TemplateEntry_AccountId" ON "TemplateEntry" ("AccountId");


CREATE INDEX "IX_TemplateEntry_TemplateId" ON "TemplateEntry" ("TemplateId");


CREATE INDEX "IX_TemplateGroup_ParentId" ON "TemplateGroup" ("ParentId");


CREATE INDEX "IX_Transaction_DateTime_Id" ON "Transaction" ("DateTime", "Id");


CREATE INDEX "IX_TransactionEntry_AccountId_TransactionId_Position" ON "TransactionEntry" ("AccountId", "TransactionId", "Position");


CREATE INDEX "IX_TransactionEntry_TransactionId" ON "TransactionEntry" ("TransactionId");


CREATE INDEX "IX_ReportGroup_ParentId" ON "ReportGroup" ("ParentId");
CREATE INDEX "IX_Report_GroupId" ON "Report" ("GroupId");

PRAGMA user_version = 1;
