<#
.SYNOPSIS
    Measures the main API queries on the seeded data, with and without the secondary indexes.

.DESCRIPTION
    Runs the exact SQL that EF Core generates (captured from the API log) through sp_executesql,
    with SET STATISTICS IO, TIME ON. Every query runs against:
      - ServiceRequests            : the real table, with all indexes from the EF configuration
      - ServiceRequests_NoIndexes  : a temporary copy of the same rows with only the clustered primary key
    The copy is created at the start and dropped at the end; the real table is never modified.
    Prints logical reads and the median elapsed time, and writes the actual execution plans
    (SET STATISTICS PROFILE ON: operators with actual row counts) to plans.txt.

.EXAMPLE
    ./Measure-Queries.ps1 -Server '.\SQLEXPRESS' -Database 'ServiceRequests' -Runs 7
#>
param(
    [string]$Server = '.\SQLEXPRESS',
    [string]$Database = 'ServiceRequests',
    [int]$Runs = 7
)

$ErrorActionPreference = 'Stop'
$PlansFile = Join-Path $PSScriptRoot 'plans.txt'
$IndexedTable = 'ServiceRequests'
$NoIndexTable = 'ServiceRequests_NoIndexes'

# {T} is replaced by the table name. Parameters match what EF sends.
$Cases = @(
    @{ Name = 'List: status=InProgress & priority=High, newest first (COUNT)'
       Sql = 'SELECT COUNT(*) FROM [{T}] AS [s] WHERE [s].[Status] = @status AND [s].[Priority] = @priority'
       Params = "N'@status int, @priority int', @status = 2, @priority = 3" },
    @{ Name = 'List: status=InProgress & priority=High, newest first (page 1)'
       Sql = 'SELECT [s].[Id], [s].[Title], [s].[OrganizationName], [s].[Status], [s].[Priority], [s].[AssignedTo], [s].[CreatedAt], [s].[UpdatedAt], [s].[RowVersion] FROM [{T}] AS [s] WHERE [s].[Status] = @status AND [s].[Priority] = @priority ORDER BY [s].[CreatedAt] DESC, [s].[Id] DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY'
       Params = "N'@status int, @priority int, @skip int, @take int', @status = 2, @priority = 3, @skip = 0, @take = 20" },
    @{ Name = 'Search: prefix on Title or OrganizationName (COUNT)'
       Sql = "SELECT COUNT(*) FROM [{T}] AS [s] WHERE [s].[Title] LIKE @search ESCAPE N'\' OR [s].[OrganizationName] LIKE @search ESCAPE N'\'"
       Params = "N'@search nvarchar(200)', @search = N'גליל%'" },
    @{ Name = 'Search: prefix on Title or OrganizationName (page 1)'
       Sql = "SELECT [s].[Id], [s].[Title], [s].[OrganizationName], [s].[Status], [s].[Priority], [s].[AssignedTo], [s].[CreatedAt], [s].[UpdatedAt], [s].[RowVersion] FROM [{T}] AS [s] WHERE [s].[Title] LIKE @search ESCAPE N'\' OR [s].[OrganizationName] LIKE @search ESCAPE N'\' ORDER BY [s].[CreatedAt] DESC, [s].[Id] DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY"
       Params = "N'@search nvarchar(200), @skip int, @take int', @search = N'גליל%', @skip = 0, @take = 20" },
    @{ Name = 'No filter, deep page (page 5000 of 5000)'
       Sql = 'SELECT [s].[Id], [s].[Title], [s].[OrganizationName], [s].[Status], [s].[Priority], [s].[AssignedTo], [s].[CreatedAt], [s].[UpdatedAt], [s].[RowVersion] FROM [{T}] AS [s] ORDER BY [s].[CreatedAt] DESC, [s].[Id] DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY'
       Params = "N'@skip int, @take int', @skip = 99980, @take = 20" },
    @{ Name = 'Summary: GROUP BY Status'
       Sql = 'SELECT [s].[Status], COUNT(*) AS [Count] FROM [{T}] AS [s] GROUP BY [s].[Status]'
       Params = $null },
    @{ Name = 'Summary: GROUP BY Priority'
       Sql = 'SELECT [s].[Priority], COUNT(*) AS [Count] FROM [{T}] AS [s] GROUP BY [s].[Priority]'
       Params = $null }
)

function Invoke-Batch([string]$Batch, [string]$ColumnSeparator = ' ') {
    # Written to a UTF-8 file so Hebrew parameters reach SQL Server intact.
    $file = New-TemporaryFile
    try {
        [IO.File]::WriteAllText($file, $Batch, [Text.UTF8Encoding]::new($false))
        $output = sqlcmd -S $Server -E -C -d $Database -f 'i:65001,o:65001' -W -h -1 -s $ColumnSeparator -i $file 2>&1
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $output" }
        return $output
    } finally {
        Remove-Item $file
    }
}

function Get-Statement([hashtable]$Case, [string]$Table) {
    $sql = $Case.Sql.Replace('{T}', $Table).Replace("'", "''")
    if ($Case.Params) { return "EXEC sp_executesql N'$sql', $($Case.Params);" }
    return "EXEC sp_executesql N'$sql';"
}

function Measure-Case([hashtable]$Case, [string]$Table) {
    Measure-Statement $Case.Name $Table (Get-Statement $Case $Table)
}

function Measure-Statement([string]$Name, [string]$Label, [string]$Statement) {
    $null = Invoke-Batch "SET NOCOUNT ON; $Statement"   # warm-up: plan compiled, pages in memory

    $elapsed = @()
    $reads = 0
    foreach ($run in 1..$Runs) {
        $output = Invoke-Batch "SET NOCOUNT ON; SET STATISTICS IO, TIME ON; $Statement"
        $times = $output | Select-String 'CPU time = \d+ ms,\s+elapsed time = (\d+) ms' -AllMatches |
            ForEach-Object { [int]$_.Matches[0].Groups[1].Value }
        $elapsed += ($times | Measure-Object -Maximum).Maximum
        $reads = ($output | Select-String 'logical reads (\d+)' -AllMatches |
            ForEach-Object { [int]$_.Matches[0].Groups[1].Value } | Measure-Object -Sum).Sum
    }

    $sorted = $elapsed | Sort-Object
    [pscustomobject]@{
        Query        = $Name
        Table        = $Label
        LogicalReads = $reads
        MedianMs     = $sorted[[int][math]::Floor($sorted.Count / 2)]
        MinMs        = $sorted[0]
        MaxMs        = $sorted[-1]
    }
}

function Get-Plan([string]$Statement) {
    # STATISTICS PROFILE returns one row per plan operator: Rows, Executes, StmtText, ... (tab-separated here).
    $output = Invoke-Batch "SET NOCOUNT ON; SET STATISTICS PROFILE ON; $Statement" "`t"
    foreach ($line in $output) {
        $columns = "$line" -split "`t"
        if ($columns.Count -ge 10 -and $columns[0] -match '^\d+$' -and $columns[2] -notmatch '^EXEC sp_executesql') {
            '{0,8} rows {1,6} executions  {2}' -f $columns[0], $columns[1], $columns[2]
        }
    }
}

function New-TableCopy([string]$Name, [string]$ExtraIndexSql) {
    $null = Invoke-Batch @"
SET NOCOUNT ON;
DROP TABLE IF EXISTS dbo.[$Name];
SELECT Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion = CAST(RowVersion AS binary(8))
INTO dbo.[$Name] FROM dbo.[$IndexedTable];
ALTER TABLE dbo.[$Name] ADD CONSTRAINT [PK_$Name] PRIMARY KEY CLUSTERED (Id);
$ExtraIndexSql
"@
}

# Possible improvements and risks found while reading the plans, measured on the same data.
function Get-Experiments {
    $keyset = (Invoke-Batch "SET NOCOUNT ON; SELECT CONCAT(CONVERT(varchar(27), CreatedAt, 126), '|', Id) FROM dbo.[$IndexedTable] ORDER BY CreatedAt DESC, Id DESC OFFSET 99979 ROWS FETCH NEXT 1 ROWS ONLY;" |
        Where-Object { $_ -match '\|' } | Select-Object -First 1).Trim() -split '\|'
    $searchPage = $Cases[3]

    @(
        @{ Name = 'Composite index (Status, Priority, CreatedAt): COUNT'; Label = $CompositeTable
           Statement = Get-Statement $Cases[0] $CompositeTable },
        @{ Name = 'Composite index (Status, Priority, CreatedAt): page 1'; Label = $CompositeTable
           Statement = Get-Statement $Cases[1] $CompositeTable },
        # Same SQL text as the common-prefix search above, so SQL Server reuses the plan it cached for N'גליל%'.
        @{ Name = 'Search page 1, prefix with no matches, reused plan'; Label = $IndexedTable
           Statement = (Get-Statement $searchPage $IndexedTable).Replace("N'גליל%'", "N'zzz%'") },
        @{ Name = 'Search page 1, prefix with no matches, OPTION (RECOMPILE)'; Label = $IndexedTable
           Statement = (Get-Statement @{ Sql = "$($searchPage.Sql) OPTION (RECOMPILE)"; Params = $searchPage.Params } $IndexedTable).Replace("N'גליל%'", "N'zzz%'") },
        @{ Name = 'Keyset pagination: same last page as OFFSET 99980'; Label = $IndexedTable
           Statement = "EXEC sp_executesql N'SELECT TOP (@take) [s].[Id], [s].[Title], [s].[OrganizationName], [s].[Status], [s].[Priority], [s].[AssignedTo], [s].[CreatedAt], [s].[UpdatedAt], [s].[RowVersion] FROM [$IndexedTable] AS [s] WHERE [s].[CreatedAt] < @lastCreatedAt OR ([s].[CreatedAt] = @lastCreatedAt AND [s].[Id] < @lastId) ORDER BY [s].[CreatedAt] DESC, [s].[Id] DESC', N'@take int, @lastCreatedAt datetime2, @lastId int', @take = 20, @lastCreatedAt = '$($keyset[0])', @lastId = $($keyset[1]);" }
    )
}

$CompositeTable = 'ServiceRequests_Composite'
Write-Host "Creating $NoIndexTable (clustered primary key only) and $CompositeTable (plus one composite index)..."
New-TableCopy $NoIndexTable ''
New-TableCopy $CompositeTable "CREATE INDEX IX_Status_Priority_CreatedAt ON dbo.[$CompositeTable] (Status, Priority, CreatedAt);"

try {
    $results = foreach ($case in $Cases) {
        Measure-Case $case $NoIndexTable
        Measure-Case $case $IndexedTable
    }
    $results | Format-Table -AutoSize | Out-String -Width 250

    $experiments = Get-Experiments
    $experimentResults = foreach ($experiment in $experiments) {
        Measure-Statement $experiment.Name $experiment.Label $experiment.Statement
    }
    Write-Host 'Possible improvements and risks:'
    $experimentResults | Format-Table -AutoSize | Out-String -Width 250

    $plans = @(
        foreach ($case in $Cases) {
            foreach ($table in $NoIndexTable, $IndexedTable) {
                "=== $($case.Name) | $table"
                Get-Plan (Get-Statement $case $table)
                ''
            }
        }
        foreach ($experiment in $experiments) {
            "=== $($experiment.Name) | $($experiment.Label)"
            Get-Plan $experiment.Statement
            ''
        }
    )
    [IO.File]::WriteAllLines($PlansFile, [string[]]$plans, [Text.UTF8Encoding]::new($false))
    Write-Host "Plans written to $PlansFile"
} finally {
    $null = Invoke-Batch "DROP TABLE IF EXISTS dbo.[$NoIndexTable]; DROP TABLE IF EXISTS dbo.[$CompositeTable];"
    Write-Host "Dropped $NoIndexTable and $CompositeTable."
}
