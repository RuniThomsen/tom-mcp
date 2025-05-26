using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using System.ComponentModel;
using Microsoft.AnalysisServices.Tabular;
using ModelHelpers;
using System.Linq;

namespace Tools;

[McpServerToolType]
public static class TmdlCreateModelTool
{
    [McpServerTool(Name = "tmdl_create_model")]
    [Description("Create a new TMDL model programmatically using TOM and serialize to TMDL")]
    public static Task<string> CreateModel(
        [Description("Path where the TMDL model should be saved")]
        string outputPath,
        [Description("Model name")]
        string modelName = "Account Lookup Migration - RESTART",
        [Description("Databricks server endpoint")]
        string databricksServer = "adb-3008600774400100.0.azuredatabricks.net",
        [Description("Databricks warehouse path")]
        string warehousePath = "/sql/1.0/warehouses/55eb5a368fc69f9c",
        [Description("Progress reporter for streaming creation results")]
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<string> messages = new();
        
        void ReportProgress(string message)
        {
            messages.Add(message);
            progress?.Report(new ProgressNotificationValue 
            { 
                Progress = messages.Count, 
                Total = null,
                Message = message 
            });
        }        try
        {
            ReportProgress($"Starting creation of model '{modelName}'...");

            Database database;
            Model model;
            
            // Try to load existing model first to preserve existing tables
            if (Directory.Exists(outputPath))
            {
                try
                {
                    ReportProgress("Loading existing model to preserve tables...");
                    database = TmdlSerializer.DeserializeDatabaseFromFolder(outputPath);
                    model = database.Model;
                    ReportProgress($"✅ Loaded existing model with {model.Tables.Count} tables");
                }
                catch (Exception ex)
                {
                    ReportProgress($"⚠️ Could not load existing model ({ex.Message}), creating new one...");
                    // Create new database and model if loading fails
                    database = new Database()
                    {
                        Name = modelName,
                        ID = modelName,
                        CompatibilityLevel = 1600 // Power BI Service compatibility
                    };

                    model = new Model()
                    {
                        Name = modelName,
                        Description = "Account Look Up migration from SSAS to fresh Common Semantic Model - Dev_TEST environment",
                        Culture = "en-US",
                        DefaultPowerBIDataSourceVersion = PowerBIDataSourceVersion.PowerBI_V3
                    };

                    database.Model = model;
                }
            }
            else
            {
                // Create new database and model
                database = new Database()
                {
                    Name = modelName,
                    ID = modelName,
                    CompatibilityLevel = 1600 // Power BI Service compatibility
                };

                model = new Model()
                {
                    Name = modelName,
                    Description = "Account Look Up migration from SSAS to fresh Common Semantic Model - Dev_TEST environment",
                    Culture = "en-US",
                    DefaultPowerBIDataSourceVersion = PowerBIDataSourceVersion.PowerBI_V3
                };

                database.Model = model;
            }
            
            ReportProgress("✅ Model structure ready for enhancement");

            // Ensure Databricks data source exists
            var dataSource = model.DataSources.FirstOrDefault(ds => ds.Name == "Databricks");
            if (dataSource == null)
            {
                dataSource = new StructuredDataSource()
                {
                    Name = "Databricks"
                };
                model.DataSources.Add(dataSource);
                ReportProgress("✅ Added Databricks data source");
            }
            else
            {
                ReportProgress("✅ Using existing Databricks data source");
            }// Create GLEntry fact table
            if (model.Tables.Any(t => t.Name == "GLEntry"))
            {
                ReportProgress("✅ GLEntry table already exists, skipping");
            }
            else
            {
                CreateGLEntryTable(model, dataSource, databricksServer, warehousePath);
                ReportProgress("✅ Created GLEntry fact table");
            }

            // Create GLAccount dimension table
            if (model.Tables.Any(t => t.Name == "GLAccount"))
            {
                ReportProgress("✅ GLAccount table already exists, skipping");
            }
            else
            {
                CreateGLAccountTable(model, dataSource, databricksServer, warehousePath);
                ReportProgress("✅ Created GLAccount dimension table");
            }

            // Create Date calculated table
            if (model.Tables.Any(t => t.Name == "Date"))
            {
                ReportProgress("✅ Date table already exists, skipping");
            }
            else
            {
                CreateDateTable(model);
                ReportProgress("✅ Created Date calculated table");
            }

            // Create Account calculated table
            if (model.Tables.Any(t => t.Name == "Account"))
            {
                ReportProgress("✅ Account table already exists, skipping");
            }
            else
            {
                CreateAccountTable(model);
                ReportProgress("✅ Created Account calculated table");
            }

            // Create relationships
            CreateRelationships(model);
            ReportProgress("✅ Created relationships");

            // Create core measures
            CreateCoreMeasures(model);
            ReportProgress("✅ Created core measures");            // Save to TMDL using separated structure
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            // Use separated TMDL serialization
            TmdlSerializer.SerializeDatabaseToFolder(database, outputPath);
            ReportProgress($"✅ Model saved to {outputPath}");

            return Task.FromResult(string.Join("\n", messages));
        }
        catch (Exception ex)        {
            var errorMsg = $"❌ Error creating model: {ex.Message}";
            ReportProgress(errorMsg);
            return Task.FromResult(string.Join("\n", messages));
        }
    }

    private static void CreateGLEntryTable(Model model, DataSource dataSource, string databricksServer, string warehousePath)
    {
        var table = new Table()
        {
            Name = "GLEntry",
            Description = "Core transaction fact table from Databricks"
        };

        // Add columns
        table.Columns.Add(new DataColumn()
        {
            Name = "g_l_account_no_",
            DataType = DataType.String,
            SourceColumn = "g_l_account_no_"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "noc",
            DataType = DataType.String,
            SourceColumn = "noc"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "amount",
            DataType = DataType.Decimal,
            SourceColumn = "amount"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "posting_date",
            DataType = DataType.DateTime,
            SourceColumn = "posting_date"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "currency_code",
            DataType = DataType.String,
            SourceColumn = "currency_code"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "scd_is_current",
            DataType = DataType.Boolean,
            SourceColumn = "scd_is_current"
        });        // Create partition with proper M expression
        var partition = new Partition()
        {
            Name = "GLEntryData",
            Mode = ModeType.DirectQuery,
            Source = new MPartitionSource()
            {
                Expression = $@"let
    Source = Databricks.Catalogs(""{databricksServer}"", ""{warehousePath}""),
    cdp_gold_prod_systems = Source{{[Name=""cdp_gold_prod_systems""]}}[Data],
    nus3 = cdp_gold_prod_systems{{[Name=""nus3""]}}[Data],
    glentry = nus3{{[Name=""glentry""]}}[Data],
    FilteredRows = Table.SelectRows(glentry, each [scd_is_current] = true)
in
    FilteredRows"
            }
        };

        table.Partitions.Add(partition);
        model.Tables.Add(table);
    }

    private static void CreateGLAccountTable(Model model, DataSource dataSource, string databricksServer, string warehousePath)
    {
        var table = new Table()
        {
            Name = "GLAccount",
            Description = "Chart of accounts dimension from Databricks"
        };

        // Add columns
        table.Columns.Add(new DataColumn()
        {
            Name = "no_",
            DataType = DataType.String,
            SourceColumn = "no_"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "name",
            DataType = DataType.String,
            SourceColumn = "name"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "noc",
            DataType = DataType.String,
            SourceColumn = "noc"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "income_balance",
            DataType = DataType.Int64,
            SourceColumn = "income_balance"
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "scd_is_current",
            DataType = DataType.Boolean,
            SourceColumn = "scd_is_current"
        });        // Create partition
        var partition = new Partition()
        {
            Name = "GLAccountData",
            Mode = ModeType.DirectQuery,
            Source = new MPartitionSource()
            {
                Expression = $@"let
    Source = Databricks.Catalogs(""{databricksServer}"", ""{warehousePath}""),
    cdp_gold_prod_systems = Source{{[Name=""cdp_gold_prod_systems""]}}[Data],
    nus3 = cdp_gold_prod_systems{{[Name=""nus3""]}}[Data],
    glaccount = nus3{{[Name=""glaccount""]}}[Data],
    FilteredRows = Table.SelectRows(glaccount, each [scd_is_current] = true)
in
    FilteredRows"
            }
        };

        table.Partitions.Add(partition);
        model.Tables.Add(table);
    }

    private static void CreateDateTable(Model model)
    {
        var table = new Table()
        {
            Name = "Date",
            Description = "Calculated date dimension with fiscal year logic"
        };

        // Add columns
        var dateColumn = new DataColumn()
        {
            Name = "Date",
            DataType = DataType.DateTime,
            IsKey = true
        };
        table.Columns.Add(dateColumn);

        table.Columns.Add(new DataColumn()
        {
            Name = "DateKey",
            DataType = DataType.String
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "FiscalYear",
            DataType = DataType.Int64
        });

        // Create calculated partition
        var partition = new Partition()
        {
            Name = "DateData",
            Mode = ModeType.Import,
            Source = new CalculatedPartitionSource()
            {
                Expression = @"ADDCOLUMNS(
    CALENDAR(DATE(2020,1,1), DATE(2030,12,31)),
    ""DateKey"", FORMAT([Date], ""YYYYMMDD""),
    ""FiscalYear"", IF(MONTH([Date]) >= 4, YEAR([Date]), YEAR([Date]) - 1)
)"
            }
        };

        table.Partitions.Add(partition);
        model.Tables.Add(table);
    }

    private static void CreateAccountTable(Model model)
    {
        var table = new Table()
        {
            Name = "Account",
            Description = "Enhanced account dimension with hierarchy and business logic"
        };

        // Add columns
        table.Columns.Add(new DataColumn()
        {
            Name = "no_",
            DataType = DataType.String
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "name",
            DataType = DataType.String
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "noc",
            DataType = DataType.String
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "account_hierarchy_level1",
            DataType = DataType.String
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "pl_balance_type",
            DataType = DataType.String
        });

        table.Columns.Add(new DataColumn()
        {
            Name = "sign_convention",
            DataType = DataType.Int64
        });

        // Create calculated partition
        var partition = new Partition()
        {
            Name = "AccountData",
            Mode = ModeType.Import,
            Source = new CalculatedPartitionSource()
            {
                Expression = @"ADDCOLUMNS(
    FILTER(GLAccount, GLAccount[scd_is_current] = TRUE),
    ""account_hierarchy_level1"", 
        SWITCH(
            TRUE(),
            LEFT(GLAccount[no_], 1) = ""1"", ""Assets"",
            LEFT(GLAccount[no_], 1) = ""2"", ""Liabilities"", 
            LEFT(GLAccount[no_], 1) = ""3"", ""COGS"",
            LEFT(GLAccount[no_], 1) = ""5"", ""OPEX"",
            LEFT(GLAccount[no_], 1) = ""6"", ""Other Operating"",
            LEFT(GLAccount[no_], 1) = ""7"", ""Tax"",
            LEFT(GLAccount[no_], 1) = ""8"", ""Revenue"",
            LEFT(GLAccount[no_], 1) = ""9"", ""IFRS/Interco"",
            ""Other""
        ),
    ""pl_balance_type"",
        IF(GLAccount[income_balance] = 0, ""P&L"", ""Balance Sheet""),
    ""sign_convention"",
        IF(LEFT(GLAccount[no_], 1) = ""8"", -1, 1)
)"
            }
        };

        table.Partitions.Add(partition);
        model.Tables.Add(table);
    }

    private static void CreateRelationships(Model model)
    {
        // GLEntry to Account relationship
        model.Relationships.Add(new SingleColumnRelationship()
        {
            Name = "GLEntry_to_Account",
            FromColumn = model.Tables["GLEntry"].Columns["g_l_account_no_"],
            ToColumn = model.Tables["Account"].Columns["no_"],
            CrossFilteringBehavior = CrossFilteringBehavior.OneDirection,
            IsActive = true
        });

        // GLEntry to Date relationship
        model.Relationships.Add(new SingleColumnRelationship()
        {
            Name = "GLEntry_to_Date",
            FromColumn = model.Tables["GLEntry"].Columns["posting_date"],
            ToColumn = model.Tables["Date"].Columns["Date"],
            CrossFilteringBehavior = CrossFilteringBehavior.OneDirection,
            IsActive = true
        });
    }

    private static void CreateCoreMeasures(Model model)
    {
        var glEntryTable = model.Tables["GLEntry"];

        // Core P&L measure
        glEntryTable.Measures.Add(new Measure()
        {
            Name = "Account - Actual P&L DKK",
            Description = "Base P&L measure with sign conventions - equivalent to SSAS measure",
            Expression = @"VAR BaseAmount = 
    SUMX(
        FILTER(
            GLEntry,
            GLEntry[scd_is_current] = TRUE
        ),
        GLEntry[amount] * RELATED(Account[sign_convention])
    )
RETURN
    BaseAmount"
        });

        // YTD measure
        glEntryTable.Measures.Add(new Measure()
        {
            Name = "Account - Actual P&L DKK YTD",
            Description = "YTD measure with April-March fiscal year - equivalent to SSAS YTD dynamic",
            Expression = @"TOTALYTD(
    [Account - Actual P&L DKK],
    Date[Date],
    ""3-31""
)"
        });

        // Validation measure
        glEntryTable.Measures.Add(new Measure()
        {
            Name = "Validation - Total Variance %",
            Description = "Variance calculation for SSAS validation",
            Expression = @"VAR SSASReference = -251750275.98
VAR CurrentValue = [Account - Actual P&L DKK]
RETURN
    IF(
        SSASReference <> 0,
        ABS((CurrentValue - SSASReference) / SSASReference),
        BLANK()
    )",
            FormatString = "0.00%"
        });
    }
}
