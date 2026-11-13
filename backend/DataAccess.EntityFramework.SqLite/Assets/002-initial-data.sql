-- Dataset-independent roots only. Apply after 001-schema.sql.
-- Setup or a role-specific test seed supplies currency, Rebalancing and configuration.
PRAGMA foreign_keys = ON;
BEGIN IMMEDIATE;
INSERT INTO "AccountGroup" ("Id", "EditRevision", "DeleteRevision", "ModificationType", "Name", "Description", "Order", "IsFavorite", "ParentId") VALUES (X'CCBBD022EC37F44AB5C79CAF34FEC1E9', 0, NULL, 0, 'Account', NULL, 0, 0, X'CCBBD022EC37F44AB5C79CAF34FEC1E9');
INSERT INTO "CategoryGroup" ("Id", "EditRevision", "DeleteRevision", "ModificationType", "Name", "Description", "Order", "IsFavorite", "ParentId") VALUES (X'F633B0CD86862242B33F66B5A3BF2948', 0, NULL, 0, 'Category', NULL, 0, 0, X'F633B0CD86862242B33F66B5A3BF2948');
INSERT INTO "CorrespondentGroup" ("Id", "EditRevision", "DeleteRevision", "ModificationType", "Name", "Description", "Order", "IsFavorite", "ParentId") VALUES (X'FEA1199C579C0347910579075987EE45', 0, NULL, 0, 'Correspondent', NULL, 0, 0, X'FEA1199C579C0347910579075987EE45');
INSERT INTO "ProjectGroup" ("Id", "EditRevision", "DeleteRevision", "ModificationType", "Name", "Description", "Order", "IsFavorite", "ParentId") VALUES (X'F685937CF22847498B44E81DD8D01949', 0, NULL, 0, 'Project', NULL, 0, 0, X'F685937CF22847498B44E81DD8D01949');
INSERT INTO "TemplateGroup" ("Id", "EditRevision", "DeleteRevision", "ModificationType", "Name", "Description", "Order", "IsFavorite", "ParentId") VALUES (X'4FA832B2D8476E42AA54BA8771B8B6DE', 0, NULL, 0, 'Template', NULL, 0, 0, X'4FA832B2D8476E42AA54BA8771B8B6DE');
INSERT INTO "ReportGroup" ("Id", "EditRevision", "DeleteRevision", "ModificationType", "Name", "Description", "Order", "IsFavorite", "ParentId") VALUES (X'C78CD28FB2691C44A27BC85F56DF0D44', 0, NULL, 0, 'Report', NULL, 0, 0, X'C78CD28FB2691C44A27BC85F56DF0D44');
COMMIT;
